using BitMagic.Common;

namespace BitMagic.Compiler.Exceptions;

internal class NoStepNoArgumentsException : CompilerSourceException
{
    public NoStepNoArgumentsException(SourceFilePosition source, string message) : base(source, message)
    {
    }
}