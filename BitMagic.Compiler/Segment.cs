using Newtonsoft.Json;
using System.Collections.Generic;

namespace BitMagic.Compiler;

public class Segment
{
    [JsonProperty]
    public string Name { get; set; } = "";

    [JsonProperty]
    public int StartAddress { get; set; }

    [JsonProperty]
    public Variables Variables { get; }

    [JsonProperty]
    public string? Filename { get; set; }

    [JsonProperty]
    public int Address { get; set; }

    [JsonProperty]
    public int MaxSize { get; set; }

    [JsonProperty]
    public bool ExplicitAddress { get; set; }

    [JsonProperty]
    public Dictionary<string, Procedure> DefaultProcedure { get; } = new Dictionary<string, Procedure>();

    public Segment(Variables globals, string name)
    {
        Name = name;
        Variables = new Variables(globals, name);
        globals.RegisterChild(Variables);
    }

    public Segment(Variables globals, bool anonymous, int startAddress, string name, string? filename = null)
    {
        Variables = new Variables(globals, name);

        globals.RegisterChild(Variables);

        StartAddress = startAddress;
        Address = startAddress;
        Name = name;
        Filename = filename;
    }

    public Procedure GetDefaultProcedure(CompileState state)
    {
        if (!DefaultProcedure.ContainsKey(state.Scope.Name))
            DefaultProcedure.Add(state.Scope.Name, new Procedure(state.Scope, $"Segment_{Name}_{state.Scope.Name}_Default", true, state.Procedure));

        return DefaultProcedure[state.Scope.Name];
    }
}
