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

    /// <summary>
    /// A separate evaluator over this state's names, for evaluating outside the compile (eg the hover) without
    /// disturbing <see cref="Evaluator"/>, which remembers what it last evaluated and isn't thread safe.
    /// </summary>
    public IExpressionEvaluator CreateEvaluator() => new ExpressionEvaluator(this);

    public List<CompilerWarning> Warnings { get; } = new List<CompilerWarning>();

    // the procedure in force after each line of the code, by line number; index 0 is before the first line
    [JsonIgnore]
    internal List<Procedure> LineProcedures { get; } = new List<Procedure>();

    /// <summary>
    /// The procedure a line of the compiled code is in, for every line: directives, labels, blank lines and
    /// comments as well as code. A line that opens a procedure or scope is in the new one, a line that closes
    /// one is in the outer one. Null if the line number is past the end of the code.
    /// </summary>
    public Procedure ProcedureAt(int lineNumber) =>
        lineNumber >= 0 && lineNumber < LineProcedures.Count ? LineProcedures[lineNumber] : null;

    private uint Id { get; set; }

    public bool ZpParse { get; set; }

    public bool NoStop { get; set; }
    public bool StopNext { get; set; }
    public bool Breakpoint { get; set; }
    public uint? DebugActionId { get; set; }

    // returns collected state that is emitted on a line of code or data
    public uint GetDebugData()
    {
        var toReturn = (Breakpoint ? DebugConstants.Breakpoint : 0u) +
                       (StopNext ? 0u : (NoStop ? DebugConstants.NoStop : 0u)) +
                       (DebugActionId != null ? DebugConstants.DebugAction : 0u);

        Breakpoint = false;
        StopNext = false;

        if (DebugActionId != null)
        {
            var id = Id++;
            toReturn |= (id << 8);
            DebugActionId = null;
        }

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
        Id = 1;
    }
}
