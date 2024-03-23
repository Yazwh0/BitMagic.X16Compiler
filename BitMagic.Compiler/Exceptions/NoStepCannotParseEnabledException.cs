using BitMagic.Common;

namespace BitMagic.Compiler.Exceptions;

internal class NoStepCannotParseEnabledException : CompilerSourceException
{
    public NoStepCannotParseEnabledException(SourceFilePosition source, string message) : base(source, message)
    {
    }
}