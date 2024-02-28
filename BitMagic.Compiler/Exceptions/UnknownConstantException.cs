using BitMagic.Common;

namespace BitMagic.Compiler.Exceptions;

public class UnknownConstantException : CompilerSourceException
{
    public UnknownConstantException(SourceFilePosition source, string message) : base(source, message)
    {
    }

}