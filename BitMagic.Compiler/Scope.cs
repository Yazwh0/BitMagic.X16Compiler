using BitMagic.Common;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace BitMagic.Compiler;

public class ScopeFactory
{
    private readonly Dictionary<string, Scope> _scopes = new Dictionary<string, Scope>();
    private readonly Variables _global;
    public ScopeFactory(Variables global)
    {
        _global = global;
    }

    public Scope GetScope(string name)
    {
        if (!_scopes.ContainsKey(name))
            _scopes.Add(name, new Scope(name, _global));

        return _scopes[name];
    }

    public IEnumerable<Scope> AllScopes => _scopes.Values;
    public Variables GlobalVariables => _global;
}

public class Scope : IScope
{
    [JsonProperty]
    public string Name { get; }
    [JsonProperty]
    public Variables Variables { get; }
    IVariables IScope.Variables => Variables;

    internal Scope(string name, Variables globals)
    {
        Name = name;
        Variables = new Variables(globals, name);
    }
}
