using BitMagic.Common;

namespace BitMagic.Compiler.Exceptions;

public class MachineAlreadySetException : CompilerSourceException
{
    public string ExistingMachineName { get; }
    public string NewMachineName { get; }

    public MachineAlreadySetException(SourceFilePosition source, string existing, string newName) : base(source, $"Machine is already set. Already {existing}, being changed to {newName}.")
    {
        ExistingMachineName = existing;
        NewMachineName = newName;
    }

    public override string ErrorDetail => $"Machine is already set to {ExistingMachineName}, but is being changed to {NewMachineName}.";
}
