using BitMagic.Common;

namespace BitMagic.Compiler.Exceptions;

internal class UnknownDataTypeCompilerException : CompilerSourceException
{
    public UnknownDataTypeCompilerException(SourceFilePosition source, string message) : base(source, message)
    {
    }

}
