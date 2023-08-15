using BitMagic.Common;

namespace BitMagic.Compiler.Exceptions;

public class CpuNotKnownException : CompilerSourceException
{
    public string CpuName { get; }

    public CpuNotKnownException(SourceFilePosition source, string name) : base(source, $"Cpu '{name}' not known.")
    {
        CpuName = name;
    }

    public override string ErrorDetail => $"Unknown Cpu '{CpuName}'.";
}
