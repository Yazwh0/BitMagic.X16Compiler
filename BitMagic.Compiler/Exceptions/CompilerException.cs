using System;

namespace BitMagic.Compiler.Exceptions;

public abstract class CompilerException : Exception
{
    public abstract string ErrorDetail { get; }

    protected CompilerException(string message) : base(message)
    {
    }

    protected CompilerException(string message, Exception inner) : base(message, inner)
    {
    }
}
