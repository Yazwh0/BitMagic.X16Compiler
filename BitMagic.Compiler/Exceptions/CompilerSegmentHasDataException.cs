
namespace BitMagic.Compiler.Exceptions;

internal class CompilerSegmentHasDataException : CompilerException
{
    public string SegmentName { get; }

    public override string ErrorDetail => SegmentName;

    public CompilerSegmentHasDataException(string segmentName) : base($"Segment '{segmentName}' has data, but no filename.")
    {
        SegmentName = segmentName;
    }
}
