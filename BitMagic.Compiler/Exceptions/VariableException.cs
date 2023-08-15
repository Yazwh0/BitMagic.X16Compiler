using BitMagic.Common;
using BitMagic.Compiler.Exceptions;

namespace BitMagic.Compiler;

public class VariableException : CompilerSourceException
{
    public string VariableName { get; }

    public VariableException(SourceFilePosition source, string variableName, string message) : base(source, message)
    {
        VariableName = variableName;
    }

    public override string ErrorDetail => VariableName;
}
