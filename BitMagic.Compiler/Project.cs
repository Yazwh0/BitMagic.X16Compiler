using BitMagic.Common;
using BitMagic.Compiler.Files;
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;

namespace BitMagic.Compiler;

public class Project
{
    //public BitMagicProjectFile PreProcess { get; } = new BitMagicProjectFile();
    public SourceFileBase Code { get; set; } = new BitMagicProjectFile();

    public ProjectBinFile OutputFile { get; } = new ProjectBinFile();
    public ProjectBinFile RomFile { get; } = new ProjectBinFile();

    public Options Options { get; } = new Options();
    public CompileOptions CompileOptions { get; set; } = new CompileOptions();

    public IMachine? Machine { get; set; }

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

[JsonObject("compileOptions")]
public class CompileOptions
{
    [JsonProperty("displayVariables", DefaultValueHandling = DefaultValueHandling.Ignore)]
    [Description("Display all the variables and their values.")]
    public bool DisplayVariables { get; set; }

    [JsonProperty("displaySegments", DefaultValueHandling = DefaultValueHandling.Ignore)]
    [Description("Display segments.")]
    public bool DisplaySegments { get; set; }

    [JsonProperty("displayCode", DefaultValueHandling = DefaultValueHandling.Ignore)]
    [Description("Display generated code.")]
    public bool DisplayCode { get; set; }

    [JsonProperty("displayData", DefaultValueHandling = DefaultValueHandling.Ignore)]
    [Description("Display generated data.")]
    public bool DisplayData { get; set; }

    [JsonProperty("rebuild", DefaultValueHandling = DefaultValueHandling.Ignore)]
    [Description("Force rebuild.")]
    public bool Rebuild { get; set; }

    [JsonProperty("binFolder", DefaultValueHandling = DefaultValueHandling.Ignore)]
    [Description("Bin folder for the dlls.")]
    public string BinFolder { get; set; } = "";

    [JsonProperty("saveGeneratedBmasm", DefaultValueHandling = DefaultValueHandling.Ignore)]
    [Description("Save Generated BMASM files in the bin folder.")]
    public bool SaveGeneratedBmasm { get; set; } = true;

    [JsonProperty("saveGeneratedTemplate", DefaultValueHandling = DefaultValueHandling.Ignore)]
    [Description("Save Generated Template C# files in the bin folder.")]
    public bool SaveGeneratedTemplate { get; set; } = false;

    [JsonProperty("savePreGeneratedTemplate", DefaultValueHandling = DefaultValueHandling.Ignore)]
    [Description("Save Generated Pre-Template cp,bomed BMASM \\ C# files in the bin folder.")]
    public bool SavePreGeneratedTemplate { get; set; } = false;
}
