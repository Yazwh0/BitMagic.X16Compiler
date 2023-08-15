using BitMagic.Common;
using BitMagic.Compiler.Exceptions;

namespace BitMagic.Compiler.Exceptions;

public class CompilerBranchToFarException : CompilerLineException
{
    public CompilerBranchToFarException(IOutputData line, string message) : base(line, message)
    {
    }
}
