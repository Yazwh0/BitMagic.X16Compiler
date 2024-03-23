using BitMagic.Common;

namespace BitMagic.Compiler.Exceptions;

internal class NoStopCannotParseEnabledException : CompilerSourceException
{
    public NoStopCannotParseEnabledException(SourceFilePosition source, string message) : base(source, message)
    {
    }
}