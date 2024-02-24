using BitMagic.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BitMagic.Compiler;

internal class DataBlock : IOutputData
{
    public byte[] Data { get; private set; } = new byte[] { };
    public int Address { get; }
    public bool RequiresReval { get; private set; }
    public List<string> RequiresRevalNames { get; } = new List<string>();
    public SourceFilePosition Source { get; }
    public bool CanStep { get; }
    public IScope Scope => _procedure;

    private readonly int _length;
    private readonly int _size;
    private readonly string _expression;
    private readonly VariableType _type;
    private readonly IExpressionEvaluator _expressionEvaluator;
    private readonly Procedure _procedure;

    internal DataBlock(int address, SourceFilePosition source, int length, VariableType type, string expression,
        Procedure procedure, IExpressionEvaluator expressionEvaluator, bool canStep)
    {
        Address = address;
        Source = source;
        _length = length;
        _size = type switch
        {
            VariableType.Byte => 1,
            VariableType.Sbyte => 1,
            VariableType.Short => 2,
            VariableType.Ushort => 2,
            VariableType.Int => 4,
            VariableType.Uint => 4,
            VariableType.Long => 8,
            VariableType.Ulong => 8,
            VariableType.FixedStrings => 1,
            VariableType.ProcStart => 2,
            _ => throw new Exception($"Unhandled type {type}")
        };
        _expression = expression;
        _type = type;
        _expressionEvaluator = expressionEvaluator;
        _procedure = procedure;
        CanStep = canStep;
    }

    public void ProcessParts(bool finalParse)
    {
        Data = new byte[_length * _size];

        var data = new byte[_size];

        if (_type == VariableType.FixedStrings)
        {
            RequiresReval = false;
            for (var i = 0; i < _length; i++)
            {
                if (i >= _expression.Length)
                    Data[i] = 0;
                else
                    Data[i] = (byte)_expression[i];
            }
            return;
        }
        else
        {
            var (result, requiresReval) = _expressionEvaluator.Evaluate(_expression, Source, _procedure.Variables, Address, finalParse);
            RequiresReval = requiresReval;

            BitConverter.GetBytes(result)[.._size].CopyTo(data, 0);
        }

        for (var i = 0; i < _length; i++)
        {
            for (var j = 0; j < _size; j++)
            {
                Data[i * _size + j] = data[j];
            }
        }
    }

    public void WriteToConsole(IEmulatorLogger logger)
    {
        logger.LogLine($"${Address:X4}:\t{string.Join(", ", Data.Select(a => $"${a:X2}")),-22}");
    }
}
