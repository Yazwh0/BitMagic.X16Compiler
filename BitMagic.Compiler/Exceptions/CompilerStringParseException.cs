using BitMagic.Common;

namespace BitMagic.Compiler.Exceptions;

public class CompilerStringParseException : CompilerLineException
{
    private readonly string _input;
    public CompilerStringParseException(IOutputData line, string message, string inp) : base(line, $"Cannot parse '{inp}'. {message}")
    {
        _input = inp;
    }

    public override string ErrorDetail => _input;
}
