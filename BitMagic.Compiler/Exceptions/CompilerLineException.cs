using BitMagic.Common;

namespace BitMagic.Compiler.Exceptions;

public abstract class CompilerLineException : CompilerException
{
    public IOutputData Line { get; }

    protected CompilerLineException(IOutputData line, string message) : base(message)
    {
        Line = line;
    }

    public override string ErrorDetail => Line.Source.ToString();
}
