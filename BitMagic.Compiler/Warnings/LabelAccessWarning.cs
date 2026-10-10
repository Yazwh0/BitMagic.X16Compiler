using BitMagic.Common;

namespace BitMagic.Compiler.Warnings;

// A label (or operand label) used from outside its procedure. Labels are private, but for now
// that's a warning so existing code keeps building.
public class LabelAccessWarning : CompilerWarning
{
    public int LineNumber { get; }
    public string FileName { get; }
    public string Message { get; }

    public LabelAccessWarning(SourceFilePosition source, string message)
    {
        LineNumber = source?.LineNumber ?? 0;
        FileName = source?.Name ?? "";
        Message = message;
    }

    public override string ToString() => $"{Message} On line {LineNumber} in file '{FileName}'.";
}
