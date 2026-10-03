using BitMagic.Common;
using System;
using System.Collections.Generic;

namespace BitMagic.Compiler;

public class TextLine : IOutputData
{
    public byte[] Data => Array.Empty<byte>();
    public uint[] DebugData => Array.Empty<uint>();

    public int Address { get; set; } = 0;

    public bool RequiresReval => false;

    public List<string> RequiresRevalNames => new();

    public SourceFilePosition Source { get; }
    public bool CanStep { get; }

    public IScope Scope { get; }

    public void ProcessParts(bool finalParse)
    {
    }

    public void WriteToConsole(IEmulatorLogger logger)
    {
    }

    public TextLine(SourceFilePosition source, bool canStep, IScope scope = null)
    {
        Source = source;
        CanStep = canStep;
        Scope = scope ?? new EmptyScope();
    }
}

public class EmptyScope : IScope
{
    private readonly Variables EmptyVariables = new Variables("");
    public IVariables Variables => EmptyVariables;
    public string Name => "";

    IScope IScope.Parent => null;
    bool IScope.Anonymous => true;

}