using BitMagic.Common;
using BitMagic.Compiler.Exceptions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text.RegularExpressions;

namespace BitMagic.Compiler;

public class VariableException : CompilerException
{
    public string VariableName { get; }

    public VariableException(string variableName, string message) : base(message)
    {
        VariableName = variableName;
    }

    public override string ErrorDetail => VariableName;
}

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
    public bool TryGetValue(string name, int lineNumber, out int result)
    {
        if (_variables.ContainsKey(name))
        {
            result = _variables[name].Value;
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
            return _parent.TryGetValue(name, lineNumber, out result);
        }

        var matches = new List<(string Name, int Value)>(1);

        var prev = name;
        var regexname = name;
        while (true)
        {
            regexname = prev.Replace("::", ":.*:");
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
                result = 0;
                return false;
            case 1:
                result = matches[0].Value;
                return true;
            default:
                throw new VariableException(name, $"Cannot find unique match. Possibilities: {string.Join(", ", matches.Select(i => i.Name))}");
        }
    }

    public IEnumerable<(string Name, int Value)> GetChildVariables(string prepend)
    {
        foreach (var kv in _variables)
        {
            yield return ($"{prepend}:{kv.Key}", kv.Value.Value);
        }

        foreach (var child in _children.Where(i => i != null))
        {
            foreach (var v in child.GetChildVariables(child.Namespace))
            {
                yield return ($"{prepend}:{v.Name}", v.Value);
            }
        }
    }

    public void SetValue(string name, int value, VariableType variableType, int length = 0, bool array = false)
    {
        var toAdd = new AsmVariable { Name = name, Value = value, VariableType = variableType, Length = length, Array = array };
        if (variableType == VariableType.LabelPointer) // consider all labels to be ambiguous when creating
        {
            _ambiguousVariables.Add(toAdd);
            return;
        }

        _variables[name] = toAdd;
    }

    public void MakeExplicit()
    {
        var variables = _ambiguousVariables.GroupBy(i => i.Name).Where(i => i.Count() == 1).Select(i => i.First()).ToArray();

        foreach(var i in variables)
        {
            if (_variables.ContainsKey(i.Name))
                throw new Exception($"Variable already defined {i.Name}");

            _ambiguousVariables.Remove(i);
            _variables.Add(i.Name, i);
        }
    }

    public bool HasValue(string name) => _variables.ContainsKey(name);
}
