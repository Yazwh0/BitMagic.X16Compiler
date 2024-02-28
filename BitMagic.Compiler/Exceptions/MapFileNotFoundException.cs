using BitMagic.Common;

namespace BitMagic.Compiler.Exceptions;

internal class MapFileNotFoundException : CompilerSourceException
{
    public MapFileNotFoundException(SourceFilePosition source, string message) : base(source, message)
    {
    }
}
