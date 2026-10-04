using System;

namespace BitMagic.Compiler.Exceptions;

/// <summary>
/// A build error that has no source position to report against.
/// </summary>
public class CompilerGeneralException : CompilerException
{
    public CompilerGeneralException(string message, Exception inner = null) : base(message, inner)
    {
    }

    public override string ErrorDetail => InnerException?.Message ?? "";
}
