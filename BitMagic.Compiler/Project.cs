using BitMagic.Common;
using System;
using System.IO;
using System.Threading.Tasks;

namespace BitMagic.Compiler;

public class Project
{
    //public ProjectTextFile Source { get; } = new ProjectTextFile();
    public ProjectTextFile PreProcess { get; } = new ProjectTextFile();
    public ISourceFile Code { get; set; } = new ProjectTextFile();
    public ProjectTextFile AssemblerObject { get; } = new ProjectTextFile();

    public ProjectBinFile OutputFile { get; } = new ProjectBinFile();
    public ProjectBinFile RomFile { get; } = new ProjectBinFile();

    public Options Options { get; } = new Options();
    public CompileOptions CompileOptions { get; set; } = new CompileOptions();

    public IMachine? Machine { get; set; }
    //public IMachineEmulator? MachineEmulator => Machine as IMachineEmulator;

    public TimeSpan LoadTime { get; set; }
    public TimeSpan PreProcessTime { get; set; }
    public TimeSpan CompileTime { get; set; }
}

public class ProjectBinFile
{
    public string? Filename { get; set; } = null;
    public byte[]? Contents { get; set; } = null;

    public Task Load(string filename)
    {
        Filename = filename;
        return Load();
    }

    public async Task Load()
    {
        if (string.IsNullOrWhiteSpace(Filename))
            throw new ArgumentNullException(nameof(Filename));

        Contents = await File.ReadAllBytesAsync(Filename);
    }

    public Task Save(string filename)
    {
        Filename = filename;
        return Save();
    }

    public async Task Save()
    {
        if (string.IsNullOrWhiteSpace(Filename))
            throw new ArgumentNullException(nameof(Filename));

        if (Contents == null)
            throw new ArgumentNullException(nameof(Contents));

        await File.WriteAllBytesAsync(Filename, Contents);
    }
}

public class ProjectTextFile : ISourceFile
{
    public string? Filename { get; set; } = null;
    public string? Contents { get; set; } = null;

    public string Name => System.IO.Path.GetFileName(Filename);
    public string Path => Filename;
    public int? ReferenceId => null;
    public SourceFileOrigin Origin => SourceFileOrigin.FileSystem;
    public bool Volatile => false;
    public Action Generate => () => { Load().GetAwaiter().GetResult(); };
    public bool ActualFile => true;
    public ISourceFile? Parent => null;

    public ProjectTextFile()
    {
    }

    public ProjectTextFile(string filename)
    {
        Filename = filename;
    }

    public string GetContent()
    {
        return Contents ?? "";
    }

    public Task Load(string filename)
    {
        Filename = System.IO.Path.GetFullPath(filename);
        return Load();
    }

    public async Task Load()
    {
        if (string.IsNullOrWhiteSpace(Filename))
            throw new ArgumentNullException(nameof(Filename));

        Contents = await File.ReadAllTextAsync(Filename);
    }

    public Task Save(string filename)
    {
        Filename = filename;
        return Save();
    }

    public async Task Save()
    {
        if (string.IsNullOrWhiteSpace(Filename))
            throw new ArgumentNullException(nameof(Filename));

        await File.WriteAllTextAsync(Filename, Contents);
    }
}

public class StaticTextFile : ISourceFile
{
    public string Name => "";

    public string Path => "";

    public int? ReferenceId => null;

    public SourceFileOrigin Origin => SourceFileOrigin.Static;

    public bool Volatile => false;

    public Action Generate => () => { };

    public bool ActualFile => false;

    private readonly string _content;
    public string GetContent() => _content;
    public ISourceFile? Parent => null;

    public StaticTextFile(string content)
    {
        _content = content;
    }
}

[Flags]
public enum ApplicationPart
{
    None = 0,
    Macro       = 0b0000_0001,
    Compiler    = 0b0000_0010,
    Emulator    = 0b0000_0100
}

public class Options
{
    public ApplicationPart VerboseDebugging { get; set; }
    public bool Beautify { get; set; }
}

public class CompileOptions
{
    public bool DisplayVariables { get; set; }
    public bool DisplaySegments { get; set; }
    public bool DisplayCode { get; set; }
    public bool DisplayData { get; set; }
    public bool Rebuild { get; set; }
    public string BinFolder { get; set; } = "";
}
