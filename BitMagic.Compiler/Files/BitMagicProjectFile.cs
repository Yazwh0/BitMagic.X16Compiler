using BitMagic.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace BitMagic.Compiler.Files;

public class BitMagicProjectFile : SourceFileBase
{
    public override IReadOnlyList<ISourceFile> Parents { get; } = Array.Empty<ISourceFile>();
    public override IReadOnlyList<string> Content { get; protected set; }
    public override IReadOnlyList<ParentSourceMapReference> ParentMap { get; } = Array.Empty<ParentSourceMapReference>();

    public BitMagicProjectFile()
    {
        Origin = SourceFileType.FileSystem;
        ActualFile = true;
    }

    public BitMagicProjectFile(string filename)
    {
        Name = System.IO.Path.GetFileName(filename);
        Path = filename;
        Origin = SourceFileType.FileSystem;
        ActualFile = true;
    }

    public Task Load(string filename)
    {
        Name = System.IO.Path.GetFileName(filename);
        Path = filename;
        return Load();
    }

    public async Task Load()
    {
        if (string.IsNullOrWhiteSpace(Path))
            throw new BitMagicProjectFileNotInitialised("Path not set");

        Content = (await File.ReadAllTextAsync(Path)).Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
    }

    public Task Save(string filename)
    {
        Name = System.IO.Path.GetFileName(filename);
        Path = filename;
        return Save();
    }

    public async Task Save()
    {
        if (string.IsNullOrWhiteSpace(Path))
            throw new BitMagicProjectFileNotInitialised("Path not set");

        await File.WriteAllLinesAsync(Path, Content);
    }

    public override Task UpdateContent() => Load();
}

public class BitMagicProjectFileNotInitialised : Exception
{
    public BitMagicProjectFileNotInitialised(string message) : base(message) { }
}