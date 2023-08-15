using BitMagic.Common;
using BitMagic.Compiler.Exceptions;

namespace BitMagic.Compiler.Exceptions;

public class CannotCompileException : CompilerLineException
{
    public CannotCompileException(IOutputData line, string message) : base(line, message)
    {
    }
}
