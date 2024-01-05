
using BitMagic.Common;

namespace BitMagic.Compiler.Exceptions;

public class RelativeLabelException : CompilerSourceException
{
    public RelativeLabelException(SourceFilePosition source, string message) : base(source, message)
    {
    }
}
