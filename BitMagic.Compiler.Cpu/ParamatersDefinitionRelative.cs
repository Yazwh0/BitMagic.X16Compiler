using BitMagic.Common;

namespace BitMagic.Compiler.Cpu
{
    public class BranchOutOfRangeException(string target, long offset, long outBy, long min, long max)
        : System.Exception($"Branch target '{target}' is out of range by {outBy} byte{(outBy == 1 ? "" : "s")} (offset {offset}, allowed {min} to {max}).")
    {
        public string Target { get; } = target;
        public long Offset { get; } = offset;
        public long OutBy { get; } = outBy;
    }

    public class ParamatersDefinitionRelative: ParametersDefinitionSurround
    {
        public int Offset { get; init; } = -1;

        public override (byte[]? Data, bool RequiresRecalc) Compile(string parameters, IOutputData line, ICpuOpCode opCode, IExpressionEvaluator expressionEvaluator, IVariables variables, bool final)
        {
            if (!Valid(parameters))
                return (null, false);

            if (string.IsNullOrWhiteSpace(parameters))
                return (null, false);

            var toParse = GetParameter(parameters);

            var (Result, RequiresRecalc) = expressionEvaluator.Evaluate(toParse, line.Source, variables, line.Address, final);

            var offset = Result - line.Address - opCode.OpCodeLength + Offset;

            var (min, max) = ParameterSize switch
            {
                ParameterSize.Bit8 => ((long)sbyte.MinValue, (long)sbyte.MaxValue),
                ParameterSize.Bit16 => (short.MinValue, short.MaxValue),
                _ => (int.MinValue, int.MaxValue)
            };

            if (offset < min || offset > max)
            {
                if (final)
                    throw new BranchOutOfRangeException(toParse, offset, offset > max ? offset - max : min - offset, min, max);

                offset = 0;
                RequiresRecalc = true;
            }

            return ParameterSize switch
            {
                ParameterSize.Bit8 => (new byte[] { (byte)offset }, RequiresRecalc),
                ParameterSize.Bit16 => (new byte[] { (byte)(offset & 0xff), (byte)((offset >> 8) & 0xff) }, RequiresRecalc),
                ParameterSize.Bit32 => (new byte[] { (byte)(offset & 0xff), (byte)((offset >> 8) & 0xff), (byte)((offset >> 16) & 0xff), (byte)((offset >> 24) & 0xff) }, RequiresRecalc),
                _ => throw new System.Exception($"Unknown ParametersSize {ParameterSize}")
            };
        }

        public new (int BytesUsed, string DecompiledCode) Decompile(IEnumerable<byte> inputBytes)
        {
            throw new System.NotImplementedException();
        }
    }
}
