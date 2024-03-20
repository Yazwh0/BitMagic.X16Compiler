using BitMagic.Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BitMagic.Compiler.Files;

public class StaticTextFile : SourceFileBase
{
    public override IReadOnlyList<ISourceFile> Parents { get; } = Array.Empty<ISourceFile>();
    public override IReadOnlyList<string> Content { get; protected set; }
    public override IReadOnlyList<ParentSourceMapReference> ParentMap { get; } = Array.Empty<ParentSourceMapReference>();

    public override bool X16File => false;

    public override Task UpdateContent() => Task.CompletedTask;

    public StaticTextFile(string content, string name = "", bool actualFile = false)
    {
        Content = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        Name = name;
        Path = name;
        Origin = SourceFileType.Static;
        ActualFile = actualFile;
    }
}
