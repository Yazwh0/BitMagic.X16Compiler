namespace BitMagic.Compiler.Warnings;

internal class BranchOverPageWarning : CompilerWarning
{
    public Line Line { get; }
    public BranchOverPageWarning(Line line)
    {
        Line = line;
    }

    public override string ToString() => $"Branch to a different page on line {Line.Source.LineNumber} in file '{Line.Source.Name}'. From ${Line.Address + Line.Data.Length:X4} To ${Line.Address + Line.Data.Length + (sbyte)Line.Data[0]:X4}";
}
