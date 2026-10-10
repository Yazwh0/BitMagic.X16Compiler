using BitMagic.Common;
using BitMagic.Compiler.Exceptions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;

namespace BitMagic.Compiler;

public class Variables : IVariables
{
    [JsonProperty]
    private readonly Dictionary<string, IAsmVariable> _variables = new();

    [JsonProperty]
    private readonly List<IAsmVariable> _ambiguousVariables = new();

    private readonly Variables _parent;
    private readonly List<Variables> _children = new List<Variables>();

    public string Namespace { get; }

    /// <summary>
    /// A private procedure hides everything below it from code outside it.
    /// </summary>
    public Visibility Visibility { get; set; } = Visibility.Public;

    [JsonIgnore]
    public Variables Parent => _parent;

    [JsonIgnore]
    public string FullName => _parent == null ? Namespace : $"{_parent.FullName}:{Namespace}";

    /// <summary>
    /// Called (on the root) when a label is reached from outside its scope. Labels are
    /// private to their scope, but for now that is a warning rather than an error.
    /// </summary>
    [JsonIgnore]
    public Action<SourceFilePosition, string> LabelAccessWarning { get; set; }

    // name -> why it couldn't be used, for names that were found but are private
    private readonly Dictionary<string, string> _hidden = new();

    [JsonIgnore]
    public IReadOnlyDictionary<string, IAsmVariable> Values => _variables;

    // Segment/Scope/Procedure nesting is already this same tree (see the two-arg ctor
    // below) - this just exposes it for walking, e.g. to build a debugger-side "all
    // variables" view without any new compiler-side bookkeeping.
    [JsonIgnore]
    public IReadOnlyList<Variables> Children => _children;

    [JsonIgnore]
    public IList<IAsmVariable> AmbiguousVariables => _ambiguousVariables;

    public Variables(IVariables defaultValues, string @namespace)
    {
        foreach (var kv in defaultValues.Values)
        {
            _variables.Add(kv.Key, kv.Value);
        }
        Namespace = @namespace;
    }

    public Variables(Variables parent, string @namespace)
    {
        _parent = parent;
        Namespace = @namespace;
        _parent.RegisterChild(this);
    }

    public Variables(string @namespace)
    {
        _parent = null;
        Namespace = @namespace;
    }

    internal void RegisterChild(Variables child)
    {
        _children.Add(child);
    }

    // Goes up the variable tree looking for a perfect match.
    public bool TryGetValue(string name, SourceFilePosition source, out IAsmVariable result)
    {
        Root._hidden.Remove(name);
        return TryGetValue(name, source, out result, out _, this, null);
    }

    /// <summary>
    /// For the debugger: the same lookup, but everything is visible and nothing is warned about.
    /// </summary>
    public bool TryGetValueIgnoringVisibility(string name, SourceFilePosition source, out IAsmVariable result) =>
        TryGetValue(name, source, out result, out _, this, Root);

    /// <summary>
    /// Looks up a name as seen from <paramref name="requester"/>: names it can't see (private
    /// ones, or ones inside a private procedure) are skipped. Anything inside
    /// <paramref name="bypass"/> is visible whatever its visibility (used by `.export`).
    /// </summary>
    internal bool TryGetValue(string name, SourceFilePosition source, out IAsmVariable result, out Variables owner, Variables requester, Variables bypass)
    {
        if (_variables.TryGetValue(name, out var own) && Accept(name, source, own, this, requester, bypass))
        {
            result = own;
            owner = this;
            return true;
        }

        // check child variables, with no namespace, eg, so can cross from proc to proc.
        foreach (var child in _children.Where(i => i != null))
        {
            foreach (var v in child.GetChildVariablesWithOwner(child.Namespace))
            {
                if (v.Name == name && Accept(name, source, v.Value, v.Owner, requester, bypass))
                {
                    result = v.Value;
                    owner = v.Owner;
                    return true;
                }
            }
        }

        if (_parent != null)
        {
            return _parent.TryGetValue(name, source, out result, out owner, requester, bypass);
        }

        var matches = new List<(string Name, IAsmVariable Value, Variables Owner)>(1);

        var prev = name;
        var regexname = name;
        while (true)
        {
            regexname = prev.Replace("::", ":[^:]*:");
            if (regexname == prev)
                break;

            prev = regexname;
        }

        var regex = new Regex($"^{(name.StartsWith(':') ? ".*" : "")}{regexname}$", RegexOptions.Compiled | RegexOptions.Singleline);

        // use pattern matching
        foreach (var kv in GetChildVariablesWithOwner(Namespace))
        {
            var isMatch = kv.Name == name ||
                (name.StartsWith(':') && kv.Name.EndsWith(name)) ||
                regex.Match(kv.Name).Success;

            // names that can't be seen from here don't count, so can't make a match ambiguous
            if (isMatch && Accept(name, source, kv.Value, kv.Owner, requester, bypass))
                matches.Add(kv);
        }

        switch (matches.Count)
        {
            case 0:
                result = default;
                owner = null;
                return false;
            case 1:
                result = matches[0].Value;
                owner = matches[0].Owner;
                return true;
            default:
                throw new VariableException(source, name, $"Cannot find unique match for {name}. Possibilities: {string.Join(", ", matches.Select(i => i.Name))}");
        }
    }

    private enum Access
    {
        Visible,
        Hidden,
        Label       // a label reached from outside its scope: allowed for now, with a warning
    }

    // Whether a match can be used from the requester. Hidden matches are remembered so a failed
    // lookup can say why.
    private bool Accept(string name, SourceFilePosition source, IAsmVariable value, Variables owner, Variables requester, Variables bypass)
    {
        switch (GetAccess(value, owner, requester, bypass))
        {
            case Access.Visible:
                return true;
            case Access.Label:
                Root.LabelAccessWarning?.Invoke(source, $"'{name}' is a label in '{owner.FullName}', so is private to '{(ScopeOf(owner) ?? owner).FullName}'. Use .export to make it visible.");
                return true;
            default:
                Root._hidden[name] = $"'{name}' is private to '{(ScopeOf(owner) ?? owner).FullName}'";
                return false;
        }
    }

    private static Access GetAccess(IAsmVariable value, Variables owner, Variables requester, Variables bypass)
    {
        if (bypass != null && IsWithin(owner, bypass))
            return Access.Visible;

        // a name can always be seen from its own procedure and everything below it
        if (IsWithin(requester, owner))
            return Access.Visible;

        // Visibility is about what a library shows other code: everything in a scope can see
        // everything else in it, whatever its visibility, labels included.
        if (ScopeOf(owner) is Variables scope && scope == ScopeOf(requester))
            return Access.Visible;

        // every procedure between the owner and the requester's branch has to be public
        for (var node = owner; node != null && !IsWithin(requester, node); node = node._parent)
        {
            if (node.Visibility == Visibility.Private)
                return Access.Hidden;
        }

        if (value is not AsmVariable variable)
            return Access.Visible;

        if (variable.Visibility == Visibility.Private)
            return Access.Hidden;

        if (variable.IsLabel)
            return Access.Label;

        return Access.Visible;
    }

    // The scope a node is in: its ancestor directly under the root (App). Null for the root itself.
    private static Variables ScopeOf(Variables node)
    {
        for (var n = node; n?._parent != null; n = n._parent)
        {
            if (n._parent._parent == null)
                return n;
        }

        return null;
    }

    private static bool IsWithin(Variables node, Variables ancestor)
    {
        for (var n = node; n != null; n = n._parent)
        {
            if (n == ancestor)
                return true;
        }

        return false;
    }

    private Variables Root => _parent == null ? this : _parent.Root;

    /// <summary>
    /// Why the last lookup of <paramref name="name"/> failed, if it found the name but couldn't
    /// see it. Null if the name wasn't found at all.
    /// </summary>
    public string HiddenReason(string name) => Root._hidden.TryGetValue(name, out var reason) ? reason : null;

    public bool TryGetValue(int value, SourceFilePosition source, out IAsmVariable result)
    {
        foreach (var i in _variables.Where(i => i.Value.Value == value && i.Value.VariableType == VariableType.CompileConstant))
        {
            result = i.Value;
            return true;
        }

        foreach (var child in _children.Where(i => i != null))
        {
            foreach (var v in child.GetChildVariables(child.Namespace).Where(v => v.Value.Value == value))
            {
                result = v.Value;
                return true;
            }
        }

        if (_parent != null)
        {
            return _parent.TryGetValue(value, source, out result);
        }

        result = null;
        return false;
    }

    public IEnumerable<(string Name, IAsmVariable Value)> GetChildVariables(string prepend)
    {
        foreach (var kv in _variables.Where(i => i.Value.VariableType == VariableType.CompileConstant))
        {
            yield return ($"{prepend}:{kv.Key}", kv.Value);
        }

        foreach (var child in _children.Where(i => i != null))
        {
            foreach (var v in child.GetChildVariables(child.Namespace))
            {
                yield return ($"{prepend}:{v.Name}", v.Value);
            }
        }
    }

    // As GetChildVariables, with the node each variable belongs to.
    internal IEnumerable<(string Name, IAsmVariable Value, Variables Owner)> GetChildVariablesWithOwner(string prepend)
    {
        foreach (var kv in _variables.Where(i => i.Value.VariableType == VariableType.CompileConstant))
        {
            yield return ($"{prepend}:{kv.Key}", kv.Value, this);
        }

        foreach (var child in _children.Where(i => i != null))
        {
            foreach (var v in child.GetChildVariablesWithOwner(child.Namespace))
            {
                yield return ($"{prepend}:{v.Name}", v.Value, v.Owner);
            }
        }
    }

    public IEnumerable<IAsmVariable> GetChildVariables()
    {
        foreach (var kv in _variables.Values)
        {
            yield return kv;
        }
        foreach (var child in _children.Where(i => i != null))
        {
            foreach (var v in child.GetChildVariables())
            {
                yield return v;
            }
        }
    }

    public void SetDebuggerValue(string name, string expression, VariableDataType variableType, int length = 0, bool array = false)
    {
        var toAdd = new DebuggerVariable
        {
            Name = name,
            Expression = expression,
            VariableDataType = variableType,
            Length = length,
            Array = array
        };

        _variables[name] = toAdd;
    }

    public static readonly IReadOnlySet<string> ReservedNames = new HashSet<string> { "private", "public" };

    public void SetValue(string name, int value, VariableDataType variableType, bool requiresReval, int length = 0, bool array = false,
        Func<bool, (int Value, bool RequiresReval)> evaluate = null, SourceFilePosition position = null,
        Visibility visibility = Visibility.Public, bool operandLabel = false)
    {
        name = name.Trim();
        CheckName(name, position);

        var toAdd = new AsmVariable
        {
            Name = name,
            Value = value,
            VariableDataType = variableType,
            Length = length,
            Array = array,
            RequiresReval = requiresReval,
            SourceFilePosition = position,
            Visibility = visibility,
            OperandLabel = operandLabel
        };

        if (evaluate != null)
            toAdd.Evaluate = evaluate;

        if (variableType == VariableDataType.LabelPointer) // consider all labels to be ambiguous when creating
        {
            _ambiguousVariables.Add(toAdd);
            return;
        }

        _variables[name] = toAdd;
    }

    // An `.export` alias, built by the compiler.
    internal void Add(AsmVariable variable)
    {
        CheckName(variable.Name, variable.SourceFilePosition);
        _variables[variable.Name] = variable;
    }

    private static void CheckName(string name, SourceFilePosition position)
    {
        if (!ReservedNames.Contains(name))
            return;

        var message = $"'{name}' is reserved, it can't be used as a name.";
        if (position != null)
            throw new VariableException(position, name, message);

        throw new CompilerGeneralException(message);
    }

    public void MakeExplicit()
    {
        var variables = _ambiguousVariables.GroupBy(i => i.Name).Where(i => i.Count() == 1).Select(i => i.First()).ToArray();

        foreach (var i in variables)
        {
            if (_variables.ContainsKey(i.Name))
            {
                var message = $"Variable '{i.Name}' is already defined.";
                if (i is AsmVariable { SourceFilePosition: not null } asmVariable)
                    throw new VariableException(asmVariable.SourceFilePosition, i.Name, message);

                throw new CompilerGeneralException(message);
            }

            _ambiguousVariables.Remove(i);
            _variables.Add(i.Name, i);
        }

        foreach (var child in _children)
        {
            child.MakeExplicit();
        }
    }

    public bool HasValue(string name) => _variables.ContainsKey(name);
}
