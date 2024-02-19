using BitMagic.Common;

namespace BitMagic.Compiler.Exceptions;

internal class CompilerCannotParseVerbParameters : CompilerSourceException
{
    public CompilerCannotParseVerbParameters(SourceFilePosition source, string message) : base(source, message) { 
    }
}
