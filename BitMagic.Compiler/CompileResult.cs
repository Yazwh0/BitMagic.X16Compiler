using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BitMagic.Compiler;

public class CompileResult
{
    public string[] Warnings { get; init; }
    public Dictionary<string, NamedStream> Data { get; init; }
    public Project Project { get; set; }
    public CompileState State { get; }

    public CompileResult(IEnumerable<string> warnings, Dictionary<string, NamedStream> result, Project project, CompileState state)
    {
        Warnings = warnings.ToArray();
        Data = result;
        Project = project;
        State = state;
    }
}

public class NamedStream : MemoryStream
{
    public string SegmentName { get; set; }
    public string FileName { get; set; }

    public NamedStream(string name, string fileName, byte[] data) : base(data, false)
    {
        SegmentName = name;
        FileName = fileName;
    }
}
