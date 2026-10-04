using BitMagic.Common;

namespace BitMagic.Compiler.Exceptions;

/// <summary>
/// A line's output would be written over data that is already in the output file, or before the start of its segment.
/// </summary>
public class CompilerOutputOverlapException : CompilerLineException
{
    public CompilerOutputOverlapException(IOutputData line, string message) : base(line, message)
    {
    }
}

/// <summary>
/// Raised by FileWriter, which doesn't know the source line. Procedure.Write turns it into a CompilerOutputOverlapException.
/// </summary>
internal class FileWriterOverlapException : System.Exception
{
    public FileWriterOverlapException(string message) : base(message)
    {
    }
}
