using BitMagic.Common;
using BitMagic.Compiler.Exceptions;

namespace BitMagic.Compiler.Exceptions;

public class UnknownSymbolException : CompilerLineException
{
    public UnknownSymbolException(IOutputData line, string message) : base(line, message)
    {
    }
}
