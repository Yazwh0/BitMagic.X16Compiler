using BitMagic.Common;
using BitMagic.Compiler.Warnings;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace BitMagic.Compiler;

public class CompileState
{
    [JsonProperty]
    public Dictionary<string, Segment> Segments { get; set; } = new Dictionary<string, Segment>();

    [JsonProperty]
    internal Segment Segment { get; set; }
    [JsonProperty]
    internal Scope Scope { get; set; }
    [JsonProperty]
    public Procedure Procedure { get; internal set; }
    [JsonProperty]
    public Variables Globals { get; internal set; }
    [JsonProperty]
    internal int AnonCounter { get; set; }
    [JsonProperty]
    public List<string> Files { get; set; } = new List<string>();

    [JsonProperty]
    public ScopeFactory ScopeFactory { get; set; }

    public IExpressionEvaluator Evaluator { get; internal set; }

    public List<CompilerWarning> Warnings { get; } = new List<CompilerWarning>();

    public bool ZpParse { get; set; }

    public bool NoStop { get; set; }
    public bool StopNext { get; set; }
    public bool Breakpoint { get; set; }
    public bool Exception { get; set; }

    public uint GetDebugData()
    {
        var toReturn = (Breakpoint ? DebugConstants.Breakpoint : 0u) +
                       (StopNext ? 0u : (NoStop ? DebugConstants.NoStop : 0u)) +
                       (Exception ? DebugConstants.Exception : 0u);

        Breakpoint = false;
        Exception = false;
        StopNext = false;

        return toReturn;
    }

    public CompileState(Variables globals, string defaultFileName)
    {
        Globals = globals;
        ScopeFactory = new ScopeFactory(Globals);
        Evaluator = new ExpressionEvaluator(this);

        Segment = new Segment(Globals, true, 0x801, "Main", defaultFileName);
        Segments.Add(Segment.Name, Segment);
        Scope = ScopeFactory.GetScope($"Main");
        Procedure = Segment.GetDefaultProcedure(this);
    }
}
