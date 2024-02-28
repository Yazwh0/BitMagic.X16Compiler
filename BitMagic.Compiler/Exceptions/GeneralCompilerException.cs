using BitMagic.Common;

namespace BitMagic.Compiler.Exceptions;

internal class GeneralCompilerException : CompilerSourceException
{
    public GeneralCompilerException(SourceFilePosition source, string message) : base(source, message)
    {
    }
}
