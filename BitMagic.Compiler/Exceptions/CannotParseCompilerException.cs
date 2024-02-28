using BitMagic.Common;

namespace BitMagic.Compiler.Exceptions;

internal class CannotParseCompilerException : CompilerSourceException
{
    public CannotParseCompilerException(SourceFilePosition source, string message) : base(source, message)
    {
    }
}
