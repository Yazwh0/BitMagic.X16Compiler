using BitMagic.Common;
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

    private readonly Variables? _parent;
    private readonly List<Variables> _children = new List<Variables>();

    public string Namespace { get; }

    [JsonIgnore]
    public IReadOnlyDictionary<string, IAsmVariable> Values => _variables;

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
    public bool TryGetValue(string name, SourceFilePosition source, out IAsmVariable? result)
    {
        if (_variables.ContainsKey(name))
        {
            result = _variables[name];
            return true;
        }

        // check child variables, with no namespace, eg, so can cross from proc to proc.
        foreach (var child in _children.Where(i => i != null))
        {
            foreach (var v in child.GetChildVariables(child.Namespace))
            {
                if (v.Name == name)
                {
                    result = v.Value;
                    return true;
                }
            }
        }

        if (_parent != null)
        {
            return _parent.TryGetValue(name, source, out result);
        }

        var matches = new List<(string Name, IAsmVariable Value)>(1);

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
        foreach (var kv in GetChildVariables(Namespace))
        {
            if (kv.Name == name)
            {
                matches.Add(kv);
                continue;
            }

            if (name.StartsWith(':') && kv.Name.EndsWith(name))
            {
                matches.Add(kv);
                continue;
            }

            if (regex.Match(kv.Name).Success)
            {
                matches.Add(kv);
                continue;
            }
        }

        switch (matches.Count)
        {
            case 0:
                result = default;
                return false;
            case 1:
                result = matches[0].Value;
                return true;
            default:
                throw new VariableException(source, name, $"Cannot find unique match for {name}. Possibilities: {string.Join(", ", matches.Select(i => i.Name))}");
        }
    }

    public bool TryGetValue(int value, SourceFilePosition source, out IAsmVariable? result)
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

    public void SetValue(string name, int value, VariableDataType variableType, bool requiresReval, int length = 0, bool array = false,
        Func<bool, (int Value, bool RequiresReval)>? evaluate = null, SourceFilePosition? position = null)
    {
        name = name.Trim();
        var toAdd = new AsmVariable
        {
            Name = name,
            Value = value,
            VariableDataType = variableType,
            Length = length,
            Array = array,
            RequiresReval = requiresReval,
            SourceFilePosition = position
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

    public void MakeExplicit()
    {
        var variables = _ambiguousVariables.GroupBy(i => i.Name).Where(i => i.Count() == 1).Select(i => i.First()).ToArray();

        foreach (var i in variables)
        {
            if (_variables.ContainsKey(i.Name))
                throw new Exception($"Variable already defined {i.Name}");

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
