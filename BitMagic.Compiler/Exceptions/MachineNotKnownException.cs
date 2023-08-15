using BitMagic.Common;

namespace BitMagic.Compiler.Exceptions;

public class MachineNotKnownException : CompilerSourceException
{
    public string MachineName { get; }

    public MachineNotKnownException(SourceFilePosition source, string name) : base(source, $"Machine '{name}' not known.")
    {
        MachineName = name;
    }

    public override string ErrorDetail => $"Unknown machine '{MachineName}'.";
}
