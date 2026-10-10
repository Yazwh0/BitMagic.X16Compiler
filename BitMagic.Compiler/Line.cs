using BitMagic.Common;
using BitMagic.Compiler.CodingSeb;
using BitMagic.Compiler.Cpu;
using BitMagic.Compiler.Exceptions;
using CodingSeb.ExpressionEvaluator;
using System.Collections.Generic;
using System.Linq;

namespace BitMagic.Compiler;

public class Line : IOutputData
{
    public readonly static Asm6502ExpressionEvaluator _evaluator = new();

    public byte[] Data { get; internal set; } = new byte[] { };
    public uint[] DebugData { get; internal set; } = new uint[] { };
    private readonly uint _debugData;
    private readonly ICpuOpCode _opCode;
    public ICpuOpCode OpCode => _opCode;
    public bool RequiresReval { get; internal set; }
    public List<string> RequiresRevalNames { get; } = new List<string>();
    public Procedure Procedure { get; }
    private string _toParse { get; set; }
    private string _original { get; set; }
    public SourceFilePosition Source { get; }
    public string Params { get; }
    public int Address { get; set; }
    public bool CanStep => true;
    public IScope Scope => Procedure;

    private readonly ICpu _cpu;
    private readonly IExpressionEvaluator _expressionEvaluator;
    private readonly CompileState _state;

    internal Line(ICpuOpCode opCode, SourceFilePosition source, Procedure proc, ICpu cpu, IExpressionEvaluator expressionEvaluator, int address, string[] parts, CompileState state)
    {
        _cpu = cpu;
        _expressionEvaluator = expressionEvaluator;
        Procedure = proc;
        _opCode = opCode;
        _toParse = string.Join(' ', parts);
        _original = _toParse;
        Params = _toParse;
        Address = address;
        Source = source;
        _debugData = state.GetDebugData();
        _state = state;
    }

    private IEnumerable<byte> IntToByteArray(uint i)
    {
        byte toReturn;

        if (_cpu.OpCodeBytes == 4)
        {
            toReturn = (byte)((i & 0xff000000) >> 24);

            yield return toReturn;
        }

        if (_cpu.OpCodeBytes >= 3)
        {
            toReturn = (byte)((i & 0xff0000) >> 16);

            yield return toReturn;
        }

        if (_cpu.OpCodeBytes >= 2)
        {
            toReturn = (byte)((i & 0xff00) >> 8);

            yield return toReturn;
        }

        yield return (byte)(i & 0xff);
    }

    /// <summary>
    /// Removes spaces from the parameters so the addressing mode templates match, but keeps them inside a
    /// character or string literal, eg `cmp #' '`.
    /// </summary>
    internal static string RemoveSpaces(string parameters)
    {
        if (!parameters.Contains(' '))
            return parameters;

        var sb = new System.Text.StringBuilder(parameters.Length);
        char? quote = null;

        for (var i = 0; i < parameters.Length; i++)
        {
            var c = parameters[i];

            if (quote != null)
            {
                sb.Append(c);

                if (c == '\\' && i + 1 < parameters.Length)
                    sb.Append(parameters[++i]); // escaped character, eg '\''
                else if (c == quote)
                    quote = null;

                continue;
            }

            if (c is '\'' or '"')
                quote = c;

            if (c != ' ')
                sb.Append(c);
        }

        return sb.ToString();
    }

    public void ProcessParts(bool finalParse)
    {
        //var allPossible = _cpu.ParameterDefinitions.Where(i => i.Value.Valid(Params) && i.Value.HasTemplate).OrderBy(i => i.Value.Order).ToList();

        // check if the params have a label
        var thisParams = Params;
        List<string> labels = null;
        var idx = thisParams.IndexOf(": ");

        if (idx != -1)
            labels = new List<string>();

        while (idx != -1)
        {
            labels.Add(thisParams[..idx]); // removes :
            thisParams = thisParams[(idx + 1)..];
            idx = thisParams.IndexOf(": ");
        }

        thisParams = RemoveSpaces(thisParams);

        // a mode whose syntax matched, but whose value didn't fit only because a name isn't known yet
        ParametersDefinitionSingle unresolvedMode = null;

        foreach (var i in _opCode.Modes.Where(i => _cpu.ParameterDefinitions.ContainsKey(i)).Select(i => _cpu.ParameterDefinitions[i]).OrderBy(i => i.Order))
        {
            try
            {
                var compileResult = i.Compile(thisParams, this, _opCode, _expressionEvaluator, Procedure.Variables, finalParse);
                if (compileResult.Data != null)
                {
                    RequiresReval = compileResult.RequiresRecalc;

                    if (finalParse && RequiresReval)
                    {
                        // a name that was found but is private says so, rather than "unknown"
                        if (_expressionEvaluator is ExpressionEvaluator { HiddenReasons.Count: > 0 } evaluator)
                            throw new CannotCompileException(this, $"{string.Join("; ", evaluator.HiddenReasons)}.");

                        throw new CannotCompileException(this, $"Unknown label within '{_toParse}'");
                    }

                    SetData(i.AccessMode, compileResult.Data, labels);
                    return;
                }

                // Compile only evaluates when the syntax matches, so the flag is for this mode's value
                if (!finalParse && unresolvedMode == null && i is ParametersDefinitionSingle single && single.Valid(thisParams) &&
                    _expressionEvaluator is ExpressionEvaluator { LastRequiresReval: true })
                    unresolvedMode = single;
            }
            catch (BranchOutOfRangeException ex)
            {
                throw new CompilerBranchToFarException(this, ex.Message);
            }
            catch (ExpressionEvaluatorSyntaxErrorException ex)
            {
                throw new UnknownSymbolException(this, ex.Message);
            }
        }

        // On an early pass a name that isn't known yet is a two byte placeholder, so a mode that only takes a byte
        // (#x, (x),y, ...) rejects it, and with no wider mode for that syntax nothing compiled. The syntax fixes the
        // size, so use that mode with a placeholder and let the final pass work out the value, or say why it can't.
        if (unresolvedMode != null)
        {
            RequiresReval = true;
            SetData(unresolvedMode.AccessMode, new byte[ParameterBytes(unresolvedMode.ParameterSize)], labels);
            return;
        }

        // a private name gives an unknown value, which may not fit the operand, so say why
        if (_expressionEvaluator is ExpressionEvaluator { HiddenReasons.Count: > 0 } hidden)
            throw new CannotCompileException(this, $"Cannot compile line '{_original}': {string.Join("; ", hidden.HiddenReasons)}.");

        throw new CannotCompileException(this, $"Cannot compile line '{_original}'");
    }

    private static int ParameterBytes(ParameterSize size) => size switch
    {
        ParameterSize.Bit8 => 1,
        ParameterSize.Bit16 => 2,
        ParameterSize.Bit32 => 4,
        _ => 0
    };

    // The opcode plus its operand, and any inline labels on the operand.
    private void SetData(AccessMode accessMode, byte[] operand, List<string> labels)
    {
        var currentLength = Data.Length;

        Data = IntToByteArray(_opCode.GetOpCode(accessMode)).Concat(operand).ToArray();
        DebugData = new uint[Data.Length];
        DebugData[0] = _debugData;
        for (var j = 1; j < Data.Length; j++)
            DebugData[j] = _debugData & 0xfffffffe;

        if (currentLength != 0 && currentLength != Data.Length)
            throw new CannotCompileException(this, $"Fatal error. While parsing '{_toParse}' the opcode data length has changed.");

        if (labels == null)
            return;

        if (Data.Length <= 1)
            throw new LabelOutOfBoundsException(this, "Cannot apply inline labels to a opcode that doesn't have parameters.");

        foreach (var l in labels.Select(l => l.Trim()))
        {
            // operand labels are private outside their scope, .export opens them up
            var keyword = l.TrimStart('<', '>').Split(' ', '\t')[0];
            if (l.Contains(' ') && Variables.ReservedNames.Contains(keyword))
                throw new LabelOutOfBoundsException(this, $"Operand labels can't be {keyword}, they're private to their procedure. Use .export to make one visible.");

            if (l.StartsWith('<'))
            {
                _state.Procedure.Variables.SetValue(l[1..], Address + 1, VariableDataType.Byte, false, position: Source, operandLabel: true);
            }
            else if (l.StartsWith('>'))
            {
                // Data includes the opcode, so a two byte operand is a length of 3
                if (Data.Length < 3)
                    throw new LabelOutOfBoundsException(this, "Cannot use '>' to define a inline label for a opcode that takes a byte.");

                _state.Procedure.Variables.SetValue(l[1..], Address + 2, VariableDataType.Byte, false, position: Source, operandLabel: true);
            }
            else
            {
                var destType = Data.Length <= 2 ? VariableDataType.Byte : VariableDataType.Ushort;
                _state.Procedure.Variables.SetValue(l, Address + 1, destType, false, position: Source, operandLabel: true);
            }
        }
    }

    public void WriteToConsole(IEmulatorLogger logger)
    {
        logger.Log($"${Address:X4}:{(RequiresReval ? "* " : "  ")}{string.Join(", ", Data.Select(a => $"${a:X2}")),-22}");
        logger.LogLine($"{_opCode.Code}\t{Params}");
    }
}

public class LabelOutOfBoundsException(IOutputData line, string message) : CompilerLineException(line, message);