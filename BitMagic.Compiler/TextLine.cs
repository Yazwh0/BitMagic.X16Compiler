using BitMagic.Common;
using System;
using System.Collections.Generic;

namespace BitMagic.Compiler;

public class TextLine : IOutputData
{
    public byte[] Data => Array.Empty<byte>();

    public int Address => 0;

    public bool RequiresReval => false;

    public List<string> RequiresRevalNames => new();

    public SourceFilePosition Source { get; }

    public void ProcessParts(bool finalParse)
    {
    }

    public void WriteToConsole(IEmulatorLogger logger)
    {
    }

    public TextLine(SourceFilePosition source)
    {
        Source = source;
    }
}
