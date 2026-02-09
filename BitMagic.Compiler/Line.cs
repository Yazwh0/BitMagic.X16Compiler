using BitMagic.Common;
using BitMagic.Compiler.CodingSeb;
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

    public void ProcessParts(bool finalParse)
    {
        //var allPossible = _cpu.ParameterDefinitions.Where(i => i.Value.Valid(Params) && i.Value.HasTemplate).OrderBy(i => i.Value.Order).ToList();

        // check if the params have a label
        var thisParams = Params;
        List<string>? labels = null;
        var idx = thisParams.IndexOf(": ");

        if (idx != -1)
            labels = new List<string>();

        while (idx != -1)
        {
            labels.Add(thisParams[..idx]); // removes :
            thisParams = thisParams[(idx + 1)..];
            idx = thisParams.IndexOf(':');
        }

        thisParams = thisParams.Replace(" ", "");

        foreach (var i in _opCode.Modes.Where(i => _cpu.ParameterDefinitions.ContainsKey(i)).Select(i => _cpu.ParameterDefinitions[i]).OrderBy(i => i.Order))
        {
            try
            {
                var compileResult = i.Compile(thisParams, this, _opCode, _expressionEvaluator, Procedure.Variables, finalParse);
                if (compileResult.Data != null)
                {
                    RequiresReval = compileResult.RequiresRecalc;

                    if (finalParse && RequiresReval)
                        throw new CannotCompileException(this, $"Unknown label within '{_toParse}'");

                    var currentLength = Data.Length;

                    Data = IntToByteArray(_opCode.GetOpCode(i.AccessMode)).Concat(compileResult.Data).ToArray();
                    DebugData = new uint[Data.Length];
                    DebugData[0] = _debugData;
                    for (var j = 1; j < Data.Length; j++)
                        DebugData[j] = _debugData & 0xfffffffe;

                    if (currentLength != 0 && currentLength != Data.Length)
                        throw new CannotCompileException(this, $"Fatal error. While parsing '{_toParse}' the opcode data length has changed.");

                    if (labels != null)
                    {
                        if (Data.Length <= 1)
                            throw new LabelOutOfBoundsException(this, "Cannot apply inline labels to a opcode that doesn't have parameters.");

                        foreach (var l in labels.Select(l => l.Trim()))
                        {
                            if (l.StartsWith('<'))
                            {
                                _state.Procedure.Variables.SetValue(l[1..], Address + 1, VariableDataType.Byte, false, position: Source);
                            }
                            else if (l.StartsWith('>'))
                            {
                                if (Data.Length < 2)
                                    throw new LabelOutOfBoundsException(this, "Cannot use '>' to define a inline label for a opcode that takes a byte.");

                                _state.Procedure.Variables.SetValue(l[1..], Address + 2, VariableDataType.Byte, false, position: Source);
                            }
                            else
                            {
                                var destType = Data.Length <= 2 ? VariableDataType.Byte : VariableDataType.Ushort;
                                _state.Procedure.Variables.SetValue(l, Address + 1, destType, false, position: Source);
                            }
                        }
                    }

                    return;
                }
            }
            catch (ExpressionEvaluatorSyntaxErrorException ex)
            {
                throw new UnknownSymbolException(this, ex.Message);
            }
        }

        throw new CannotCompileException(this, $"Cannot compile line '{_original}'");
    }

    public void WriteToConsole(IEmulatorLogger logger)
    {
        logger.Log($"${Address:X4}:{(RequiresReval ? "* " : "  ")}{string.Join(", ", Data.Select(a => $"${a:X2}")),-22}");
        logger.LogLine($"{_opCode.Code}\t{Params}");
    }
}

public class LabelOutOfBoundsException(IOutputData line, string message) : CompilerLineException(line, message);