using BitMagic.Common;
using BitMagic.Compiler.Exceptions;
using CodingSeb.ExpressionEvaluator;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BitMagic.Compiler
{
    public class DataLine : IOutputData
    {
        public byte[] Data { get; private set; } = new byte[] { };
        public uint[] DebugData { get; private set; } = new uint[] { };
        private uint _debugData;
        public int Address { get; }
        public bool RequiresReval { get; private set; }
        public List<string> RequiresRevalNames { get; } = new List<string>();
        private Procedure _procedure { get; }
        private LineType _lineType { get; }
        public SourceFilePosition Source { get; }
        public IScope Scope => _procedure;

        public bool CanStep { get; }

        internal DataLine(Procedure proc, SourceFilePosition source, int address, LineType type, bool canStep, CompileState state)
        {
            Source = source;
            Address = address;
            _procedure = proc;
            _lineType = type;
            CanStep = canStep;
            _debugData = canStep ? state.GetDebugData() : 0u;
        }

        internal enum LineType
        {
            IsByte,
            IsWord
        }

        public void ProcessParts(bool finalParse)
        {
            var data = new List<byte>();

            var toProcess = Source.Source.Trim();//.ToLower();

            var idx = toProcess.IndexOf(';');
            if (idx != -1)
                toProcess = toProcess.Substring(0, idx);

            idx = toProcess.IndexOf('.');

            if (idx == -1)
            {
                throw new CannotCompileException(this, "Cannot find data on the line");
            }

            toProcess = toProcess.Substring(idx + 5).Trim();

            RequiresRevalNames.Clear();
            Line._evaluator.PreEvaluateVariable += _evaluator_PreEvaluateVariable;
            object rawResult;
            try
            {
                rawResult = Line._evaluator.Evaluate($"Array({toProcess})");
            } 
            catch (Exception e)
            {
                throw new CannotCompileException(this, e.Message);
            }
            Line._evaluator.PreEvaluateVariable -= _evaluator_PreEvaluateVariable;

            var result = rawResult as object[];

            if (result == null)
                throw new CannotCompileException(this, $"Expected object[] back, actually have {rawResult.GetType().Name}");

            foreach (var r in result) 
            {
                var i = r as int?;

                if (i == null)
                    throw new CannotCompileException(this, $"Expected int? value back, actually have {r.GetType().Name} for {r}");

                if (_lineType == LineType.IsByte)
                {
                    data.Add((byte)(i.Value & 0xff));
                } 
                else
                {
                    var us = (ushort)i.Value;

                    data.Add((byte)(us & 0xff));
                    data.Add((byte)((us & 0xff00) >> 8));
                }
            }

            Data = data.ToArray();
            DebugData = new uint[Data.Length];
            DebugData[0] = _debugData;
            for (var j = 1; j < Data.Length; j++)
                DebugData[j] = _debugData & 0xffff_fffe;
        }

        private void _evaluator_PreEvaluateVariable(object? sender, VariablePreEvaluationEventArg e)
        {
            if (_procedure.Variables.TryGetValue(e.Name, Source, out var result))
            {
                e.Value = result.Value;
                RequiresReval = false;
            }
            else
            {
                RequiresRevalNames.Add(e.Name);
                RequiresReval = true;
                e.Value = 0xaaaa; // random two byte number
            }
        }

        public void WriteToConsole(IEmulatorLogger logger)
        {
            logger.LogLine($"${Address:X4}:\t{string.Join(", ", Data.Select(a => $"${a:X2}")),-22}");
        }
    }
}
