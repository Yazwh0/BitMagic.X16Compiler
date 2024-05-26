using BitMagic.Common;
using BitMagic.Cpu;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BitMagic.Machines;

public class CommanderX16R38 : IMachine
{

    public string Name => "CommanderX16";
    public int Version => 38;
    ICpu IMachine.Cpu => new WDC65c02();

    private readonly CommanderX16R38Defaults _defaultVariables = new();
    public IVariables Variables => _defaultVariables;
    public bool Initialised { get; private set; } = false;
}

internal class CommanderX16R38Defaults : IVariables
{
    private static Dictionary<string, IAsmVariable> _defaults = new Dictionary<string, int>
    {
        {"ADDRx_L", 0x9F20 },
        {"ADDRx_M", 0x9F21},
        {"ADDRx_H", 0x9F22},
        {"DATA0", 0x9F23},
        {"DATA1", 0x9F24},
        {"CTRL", 0x9F25},
        {"IEN", 0x9F26},
        {"ISR", 0x9F27},
        {"IRQLINE_L", 0x9F28},
        {"DC_VIDEO", 0x9F29},
        {"DC_HSCALE", 0x9F2A},
        {"DC_VSCALE", 0x9F2B},
        {"DC_BORDER", 0x9F2C},
        {"DC_HSTART", 0x9F29},
        {"DC_HSTOP", 0x9F2A},
        {"DC_VSTART", 0x9F2B},
        {"DC_VSTOP", 0x9F2C},
        {"L0_CONFIG", 0x9F2D},
        {"L0_MAPBASE", 0x9F2E},
        {"L0_TILEBASE", 0x9F2F},
        {"L0_HSCROLL_L", 0x9F30},
        {"L0_HSCROLL_H", 0x9F31},
        {"L0_VSCROLL_L", 0x9F32},
        {"L0_VSCROLL_H", 0x9F33},
        {"L1_CONFIG", 0x9F34},
        {"L1_MAPBASE", 0x9F35},
        {"L1_TILEBASE", 0x9F36},
        {"L1_HSCROLL_L", 0x9F37},
        {"L1_HSCROLL_H", 0x9F38},
        {"L1_VSCROLL_L", 0x9F39},
        {"L1_VSCROLL_H", 0x9F3A},
        {"AUDIO_CTRL", 0x9F3B},
        {"AUDIO_RATE", 0x9F3C},
        {"AUDIO_DATA", 0x9F3D},
        {"SPI_DATA", 0x9F3E},
        {"SPI_CTRL", 0x9F3F},

        {"INTERRUPT", 0x0314},
        {"INTERRUPT_L", 0x0314},
        {"INTERRUPT_H", 0x0315},

        {"ROM_BANK", 0x9F60},
        {"RAM_BANK", 0x9F61},

        { "V_PRB", 0x9f00 },
        { "V_PRA",  0x9f01 },
        {"V_DDRB",  0x9f02 },
        {"V_DDRA",  0x9f03 },
        {"V_T1_L",  0x9f04 },
        {"V_T1_H",  0x9f05 },
        {"V_T1L_L", 0x9f06 },
        {"V_T1L_H", 0x9f07 },
        {"V_T2_L", 0x9f08 },
        {"V_T2_H", 0x9f09 },
        {"V_SR", 0x9f0a },
        {"V_ACR", 0x9f0b },
        {"V_PCR", 0x9f0c },
        {"V_IFR", 0x9f0d },
        {"V_IER", 0x9f0e },
        {"V_ORA", 0x9f0f }
    }.ToDictionary(i => i.Key, i => (IAsmVariable)(new AsmVariable { Name = i.Key, Value = i.Value, VariableType = VariableType.Ushort }));

    public IReadOnlyDictionary<string, IAsmVariable> Values => _defaults;

    public IList<IAsmVariable> AmbiguousVariables => Array.Empty<IAsmVariable>();

    // todo: create abstract class or similar.
    public bool TryGetValue(string name, SourceFilePosition source, out IAsmVariable? result) => throw new Exception();

    public bool TryGetValue(int value, SourceFilePosition source, out IAsmVariable? result)
    {
        throw new NotImplementedException();
    }
}
