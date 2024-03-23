using BitMagic.Common;

namespace BitMagic.Compiler.Exceptions;

internal class NoStopNoArgumentsException : CompilerSourceException
{
    public NoStopNoArgumentsException(SourceFilePosition source, string message) : base(source, message)
    {
    }
}