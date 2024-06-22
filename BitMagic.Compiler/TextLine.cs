using BitMagic.Common;
using System;
using System.Collections.Generic;

namespace BitMagic.Compiler;

public class TextLine : IOutputData
{
    public byte[] Data => Array.Empty<byte>();
    public uint[] DebugData => Array.Empty<uint>();

    public int Address => 0;

    public bool RequiresReval => false;

    public List<string> RequiresRevalNames => new();

    public SourceFilePosition Source { get; }
    public bool CanStep { get; }

    private readonly EmptyScope EmptyScope = new EmptyScope();
    public IScope Scope => EmptyScope;

    public void ProcessParts(bool finalParse)
    {
    }

    public void WriteToConsole(IEmulatorLogger logger)
    {
    }

    public TextLine(SourceFilePosition source, bool canStep)
    {
        Source = source;
        CanStep = canStep;
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