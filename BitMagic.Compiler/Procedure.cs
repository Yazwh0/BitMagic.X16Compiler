﻿using BitMagic.Common;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace BitMagic.Compiler;

public class Procedure : IScope
{
    [JsonProperty]
    public string Name { get; }
    [JsonProperty]
    public Variables Variables { get; }

    IVariables IScope.Variables => Variables;

    [JsonProperty]
    public List<IOutputData> Data { get; set; } = new List<IOutputData>();

    [JsonIgnore]
    public Procedure? Parent { get; }

    [JsonIgnore]
    private Dictionary<string, Procedure> _procedures = new Dictionary<string, Procedure>();

    [JsonProperty]
    public bool Anonymous { get; }

    [JsonProperty]
    public int? StartAddress => Data.FirstOrDefault()?.Address ?? null;
    public int? EndAddress {
        get {
            var line = Data.LastOrDefault();

            if (line == null)
                return null;

            return line.Address + line.Data.Length - 1;
        }
    }

    [JsonIgnore]
    public Scope Scope { get; }

    public Procedure(Scope scope, string name, bool anonymous, Procedure? parent)
    {
        Name = name;
        Variables = anonymous ? scope.Variables : new Variables(scope.Variables, name);
        Parent = parent;
        Anonymous = anonymous;
        Scope = scope;
    }
    public Procedure(string name, Procedure parent)
    {
        Name = name;
        Variables =  new Variables(parent.Variables, name);
        Parent = parent;
        Anonymous = false;
        Scope = parent.Scope;
    }

    public void AddData(IOutputData line)
    {
        Data.Add(line);
    }

    public Procedure GetProcedure(string name, int address)
    {
        if (!_procedures.ContainsKey(name))
        {
            var proc = new Procedure(name, this);
            _procedures.Add(name, proc);
            Variables.SetValue(name, address, VariableDataType.ProcStart, false);
        }

        return _procedures[name];
    }

    public Procedure GetProcedure(string name, int address, Scope scope)
    {
        if (!_procedures.ContainsKey(name))
        {
            var proc = new Procedure(scope, name, true, this);
            _procedures.Add(name, proc);
            Variables.SetValue(name, address, VariableDataType.ProcStart, false);
        }

        return _procedures[name];
    }

    internal void Write(IWriter writer)
    {
        foreach (var d in Data)
        {
            writer.Add(d.Data, d.Address, d.DebugData);
        }

        foreach(var p in _procedures.Values)
        {
            p.Write(writer);
        }
    }

    public void Relocate(int delta)
    {
        foreach (var data in Data)
        {
            data.Address += delta;
        }

        foreach (var p in _procedures.Values)
        {
            p.Relocate(delta);
        }
    }

    public IEnumerable<Procedure> Procedures => _procedures.Values;

    IScope IScope.Parent => Parent;
    bool IScope.Anonymous => Anonymous;
}
