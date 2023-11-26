using BitMagic.Common;
using BitMagic.Cpu;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BitMagic.Machines
{
    public class CommanderX16R39 : IMachine
    {
        public string Name => "CommanderX16";
        public int Version => 39;

        ICpu IMachine.Cpu => new WDC65c02();
        private readonly CommanderX16R39Defaults _defaultVariables = new();
        public IVariables Variables => _defaultVariables;

        public bool Initialised { get; private set; } = false;
    }

    internal class CommanderX16R39Defaults : IVariables
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

            {"RAM_BANK", 0x00 },
            {"ROM_BANK" ,0x01 },

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
            {"V_ORA", 0x9f0f },

            // From the kernel https://github.com/X16Community/x16-docs/blob/master/X16%20Reference%20-%2004%20-%20KERNAL.md
            {"ACPTR", 0xFFA5},
            {"BASIN", 0xFFCF},
            {"BSAVE", 0xFEBA},
            {"BSOUT", 0xFFD2},
            {"CIOUT", 0xFFA8},
            {"CLALL", 0xFFE7},
            {"CLOSE", 0xFFC3},
            {"CHKIN", 0xFFC6},
            {"clock_get_date_time", 0xFF50},
            {"clock_set_date_time", 0xFF4D},
            {"CHRIN", 0xFFCF},
            {"CHROUT", 0xFFD2},
            {"CLOSE_ALL", 0xFF4A},
            {"CLRCHN", 0xFFCC},
            {"console_init", 0xFEDB},
            {"console_get_char", 0xFEE1},
            {"console_put_char", 0xFEDE},
            {"console_put_image", 0xFED8},
            {"console_set_paging_message", 0xFED5},
            {"enter_basic", 0xFF47},
            {"entropy_get", 0xFECF},
            {"fetch", 0xFF74},
            {"FB_cursor_next_line †", 0xFF02},
            {"FB_cursor_position", 0xFEFF},
            {"FB_fill_pixels", 0xFF17},
            {"FB_filter_pixels", 0xFF1A},
            {"FB_get_info", 0xFEF9},
            {"FB_get_pixel", 0xFF05},
            {"FB_get_pixels", 0xFF08},
            {"FB_init", 0xFEF6},
            {"FB_move_pixels", 0xFF1D},
            {"FB_set_8_pixels", 0xFF11},
            {"FB_set_8_pixels_opaque", 0xFF14},
            {"FB_set_palette", 0xFEFC},
            {"FB_set_pixel", 0xFF0B},
            {"FB_set_pixels", 0xFF0E},
            {"GETIN", 0xFFE4},
            {"GRAPH_clear", 0xFF23},
            {"GRAPH_draw_image", 0xFF38},
            {"GRAPH_draw_line", 0xFF2C},
            {"GRAPH_draw_oval", 0xFF35},
            {"GRAPH_draw_rect", 0xFF2F},
            {"GRAPH_get_char_size", 0xFF3E},
            {"GRAPH_init", 0xFF20},
            {"GRAPH_move_rect", 0xFF32},
            {"GRAPH_put_char", 0xFF41},
            {"GRAPH_set_colors", 0xFF29},
            {"GRAPH_set_font", 0xFF3B},
            {"GRAPH_set_window", 0xFF26},
            {"i2c_batch_read", 0xFEB4},
            {"i2c_batch_write", 0xFEB7},
            {"i2c_read_byte", 0xFEC6},
            {"i2c_write_byte", 0xFEC9},
            {"IOBASE", 0xFFF3},
            {"JSRFAR", 0xFF6E},
            {"joystick_get", 0xFF56},
            {"joystick_scan", 0xFF53},
            {"kbdbuf_get_modifiers", 0xFEC0},
            {"kbdbuf_peek", 0xFEBD},
            {"kbdbuf_put", 0xFEC3},
            {"keymap", 0xFED2},
            {"LISTEN", 0xFFB1},
            {"LKUPLA", 0xFF59},
            {"LKUPSA", 0xFF5C},
            {"LOAD", 0xFFD5},
            {"MACPTR", 0xFF44},
            {"MCIOUT", 0xFEB1},
            {"MEMBOT", 0xFF9C},
            {"memory_copy", 0xFEE7},
            {"memory_crc", 0xFEEA},
            {"memory_decompress", 0xFEED},
            {"memory_fill", 0xFEE4},
            {"MEMTOP", 0xFF99},
            {"monitor", 0xFECC},
            {"mouse_config", 0xFF68},
            {"mouse_get", 0xFF6B},
            {"mouse_scan", 0xFF71},
            {"OPEN", 0xFFC0},
            {"PFKEY", 0xFF65},
            {"PLOT", 0xFFF0},
            {"PRIMM", 0xFF7D},
            {"RDTIM", 0xFFDE},
            {"READST", 0xFFB7},
            {"SAVE", 0xFFD8},
            {"SCNKEY", 0xFF9F},
            {"SCREEN", 0xFFED},
            {"screen_mode", 0xFF5F},
            {"screen_set_charset", 0xFF62},
            {"SECOND", 0xFF93},
            {"SETLFS", 0xFFBA},
            {"SETMSG", 0xFF90},
            {"SETNAM", 0xFFBD},
            {"SETTIM", 0xFFDB},
            {"SETTMO", 0xFFA2},
            {"sprite_set_image", 0xFEF0},
            {"sprite_set_position", 0xFEF3},
            {"stash", 0xFF77},
            {"STOP", 0xFFE1},
            {"TALK", 0xFFB4},
            {"TKSA", 0xFF96},
            {"UDTIM", 0xFFEA},
            {"UNLSN", 0xFFAE},
            {"UNTLK", 0xFFAB}

        }.ToDictionary(i => i.Key, i => (IAsmVariable)(new AsmVariable { Name = i.Key, Value = i.Value, VariableType = VariableType.Ushort }));

        public IReadOnlyDictionary<string, IAsmVariable> Values => _defaults;
        public IList<IAsmVariable> AmbiguousVariables => Array.Empty<IAsmVariable>();

        // todo: create abstract class or similar.
        public bool TryGetValue(string name, SourceFilePosition source, out int result) => throw new Exception();
    }
}
