using BitMagic.Common;
using BitMagic.Compiler.Cpu;
using System.Collections.Generic;
using System.Linq;

namespace BitMagic.Cpu;

public interface I6502 : ICpu
{
}

public class WDC65c02 : I6502
{
    public string Name => "WDC65c02";
    public IEnumerable<ICpuOpCode> OpCodes => _opCodes;
    public int OpCodeBytes => 1;

    public IReadOnlyDictionary<AccessMode, IParametersDefinition> ParameterDefinitions { get; } = new IParametersDefinition[] {
         new ParametersCommaSeparated() { AccessMode = AccessMode.ZerpPageRel, Order = 10,
             Left = new ParametersDefinitionSurround() {AccessMode = AccessMode.ZeroPage, ParameterSize = ParameterSize.Bit8, Order = 40 },
             Right = new ParamatersDefinitionRelative() {AccessMode = AccessMode.Relative, ParameterSize = ParameterSize.Bit8, Order = 40, Offset = -2 }
         },
         new ParametersDefinitionSurround() {AccessMode = AccessMode.Immediate, StartsWith = "#", ParameterSize = ParameterSize.Bit8, Order = 10 },
         new ParametersDefinitionSurround() {AccessMode = AccessMode.IndirectX, StartsWith = "(", EndsWith = ",X)", ParameterSize = ParameterSize.Bit8, Order = 20 },
         new ParametersDefinitionSurround() {AccessMode = AccessMode.IndirectY, StartsWith = "(", EndsWith = "),Y", ParameterSize = ParameterSize.Bit8, Order = 20 },
         new ParametersDefinitionSurround() {AccessMode = AccessMode.IndAbsoluteX, StartsWith = "(", EndsWith = ",X)", ParameterSize = ParameterSize.Bit16, Order = 20 },
         new ParametersDefinitionSurround() {AccessMode = AccessMode.ZeroPageIndirect, StartsWith = "(", EndsWith = ")", DoesntEndWith = new [] { ",X", ",Y" },  ParameterSize = ParameterSize.Bit8, Order = 25 },
         new ParametersDefinitionSurround() {AccessMode = AccessMode.Indirect, StartsWith = "(", EndsWith = ")",DoesntEndWith = new [] { ",X", ",Y" }, ParameterSize = ParameterSize.Bit16, Order = 25 },
         new ParametersDefinitionSurround() {AccessMode = AccessMode.ZeroPageX, EndsWith = ",X", DoesntStartWith = new [] { "(" }, ParameterSize = ParameterSize.Bit8, Order = 30 },
         new ParametersDefinitionSurround() {AccessMode = AccessMode.ZeroPageY, EndsWith = ",Y", DoesntStartWith = new [] { "(" }, ParameterSize = ParameterSize.Bit8, Order = 30 },
         new ParametersDefinitionSurround() {AccessMode = AccessMode.AbsoluteX, EndsWith = ",X", DoesntStartWith = new [] { "(" }, ParameterSize = ParameterSize.Bit16, Order = 30 },
         new ParametersDefinitionSurround() {AccessMode = AccessMode.AbsoluteY, EndsWith = ",Y", DoesntStartWith = new [] { "(" }, ParameterSize = ParameterSize.Bit16, Order = 30 },
         new ParamatersDefinitionRelative() {AccessMode = AccessMode.Relative, ParameterSize = ParameterSize.Bit8, Order = 40 },
         new ParametersDefinitionEmpty() {AccessMode = AccessMode.Implied, Order = 40 },
         new ParametersDefinitionEmpty() {AccessMode = AccessMode.Accumulator, Order = 40 },
         new ParametersDefinitionSurround() {AccessMode = AccessMode.ZeroPage, DoesntStartWith = new [] { "#", "(" }, ParameterSize = ParameterSize.Bit8, Order = 40 },
         new ParametersDefinitionSurround() {AccessMode = AccessMode.Absolute, DoesntStartWith = new [] { "#", "(" }, ParameterSize = ParameterSize.Bit16, Order = 40 },
    }.ToDictionary(i => i.AccessMode, i => i);

    private readonly CpuOpCode[] _opCodes = new CpuOpCode[]
    {
        new Adc(),
        new And(),
        new Asl(),
        new Bcc(),
        new Bcs(),
        new Beq(),
        new Bit(),
        new Bmi(),
        new Bne(),
        new Bpl(),
        new Bra(),
        new Brk(),
        new Bvc(),
        new Bvs(),
        new Clc(),
        new Cld(),
        new Cli(),
        new Clv(),
        new Cmp(),
        new Cpx(),
        new Cpy(),
        new Dec(),
        new Dex(),
        new Dey(),
        new Eor(),
        new Inc(),
        new Inx(),
        new Iny(),
        new Jmp(),
        new Jsr(),
        new Lda(),
        new Ldx(),
        new Ldy(),
        new Lsr(),
        new Nop(),
        new Ora(),
        new Pha(),
        new Php(),
        new Phx(),
        new Phy(),
        new Pla(),
        new Ply(),
        new Plx(),
        new Plp(),
        new Rol(),
        new Ror(),
        new Rti(),
        new Rts(),
        new Sbc(),
        new Sec(),
        new Sed(),
        new Sei(),
        new Sta(),
        new Stz(),
        new Stx(),
        new Sty(),
        new Stp(),
        new Tax(),
        new Tay(),
        new Tsx(),
        new Txa(),
        new Txs(),
        new Tya(),
        new Trb(),
        new Tsb(),
        new Bbr0(),
        new Bbr1(),
        new Bbr2(),
        new Bbr3(),
        new Bbr4(),
        new Bbr5(),
        new Bbr6(),
        new Bbr7(),
        new Bbs0(),
        new Bbs1(),
        new Bbs2(),
        new Bbs3(),
        new Bbs4(),
        new Bbs5(),
        new Bbs6(),
        new Bbs7(),
        new Rmb0(),
        new Rmb1(),
        new Rmb2(),
        new Rmb3(),
        new Rmb4(),
        new Rmb5(),
        new Rmb6(),
        new Rmb7(),
        new Smb0(),
        new Smb1(),
        new Smb2(),
        new Smb3(),
        new Smb4(),
        new Smb5(),
        new Smb6(),
        new Smb7(),
        new Wai(),
        new Ldd()
    };

    private readonly (CpuOpCode operation, AccessMode Mode, int Timing)?[] _operations;

    public WDC65c02()
    {
        _operations = new (CpuOpCode operation, AccessMode Mode, int Timing)?[256];

        foreach (var op in _opCodes)
        {
            foreach (var i in op.OpCodes)
            {
                _operations[i.OpCode] = (op, i.Mode, i.Timing);
            }
        }
    }
}

public class Stp : CpuOpCode
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0xdb, AccessMode.Implied, 2),
    };
}

public class Adc : CpuOpCode
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new() {
        (0x69, AccessMode.Immediate, 2),
        (0x65, AccessMode.ZeroPage, 3),
        (0x75, AccessMode.ZeroPageX, 4),
        (0x6d, AccessMode.Absolute, 4),
        (0x7d, AccessMode.AbsoluteX, 4),
        (0x79, AccessMode.AbsoluteY, 4),
        (0x61, AccessMode.IndirectX, 6),
        (0x71, AccessMode.IndirectY, 5),
        (0x72, AccessMode.ZeroPageIndirect, 5),
    };
}

public class And : CpuOpCode
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new() {
        (0x29, AccessMode.Immediate, 2),
        (0x25, AccessMode.ZeroPage, 3),
        (0x35, AccessMode.ZeroPageX, 4),
        (0x2d, AccessMode.Absolute, 4),
        (0x3d, AccessMode.AbsoluteX, 4),
        (0x39, AccessMode.AbsoluteY, 4),
        (0x21, AccessMode.IndirectX, 6),
        (0x31, AccessMode.IndirectY, 5),
        (0x32, AccessMode.ZeroPageIndirect, 5),
    };
}

public class Asl : CpuOpCode
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new() {
        (0x0a, AccessMode.Accumulator, 2),
        (0x06, AccessMode.ZeroPage, 5),
        (0x16, AccessMode.ZeroPageX, 6),
        (0x0e, AccessMode.Absolute, 6),
        (0x1e, AccessMode.AbsoluteX, 7),
    };
}

public class Bit : CpuOpCode
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new() {
        (0x24, AccessMode.ZeroPage, 3),
        (0x2c, AccessMode.Absolute, 4),
        (0x89, AccessMode.Immediate, 2),
        (0x34, AccessMode.ZeroPageX, 4),
        (0x3c, AccessMode.AbsoluteX, 4),
    };
}

public abstract class BranchOpCode : CpuOpCode
{
}

public class Bra : BranchOpCode
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0x80, AccessMode.Relative, 2)
    };
}

public class Bpl : BranchOpCode
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new() {
        (0x10, AccessMode.Relative, 2)
    };
}

public class Bmi : BranchOpCode
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new() {
        (0x30, AccessMode.Relative, 2)
    };
}

public class Bvc : BranchOpCode
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0x50, AccessMode.Relative, 2)
    };
}
public class Bvs : BranchOpCode
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0x70, AccessMode.Relative, 2)
    };
}

public class Bcc : BranchOpCode
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0x90, AccessMode.Relative, 2)
    };
}

public class Bcs : BranchOpCode
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0xb0, AccessMode.Relative, 2)
    };
}

public class Bne : BranchOpCode
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0xd0, AccessMode.Relative, 2)
    };
}

public class Beq : BranchOpCode
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0xf0, AccessMode.Relative, 2)
    };
}

public class Brk : CpuOpCode
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0x00, AccessMode.Implied, 7)
    };
}

public abstract class Compare : CpuOpCode
{
}

public class Cmp : Compare
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0xc9, AccessMode.Immediate, 2),
        (0xc5, AccessMode.ZeroPage, 3),
        (0xd5, AccessMode.ZeroPageX, 4),
        (0xcd, AccessMode.Absolute, 4),
        (0xdd, AccessMode.AbsoluteX, 4),
        (0xd9, AccessMode.AbsoluteY, 4),
        (0xc1, AccessMode.IndirectX, 6),
        (0xd1, AccessMode.IndirectY, 5),
        (0xd2, AccessMode.ZeroPageIndirect, 5),
    };
}

public class Cpx : Compare
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0xe0, AccessMode.Immediate, 2),
        (0xe4, AccessMode.ZeroPage, 3),
        (0xec, AccessMode.Absolute, 4),
    };
}

public class Cpy : Compare
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0xc0, AccessMode.Immediate, 2),
        (0xc4, AccessMode.ZeroPage, 3),
        (0xcc, AccessMode.Absolute, 4),
    };
}

public class Dec : CpuOpCode
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0x3a, AccessMode.Implied, 2),
        (0xc6, AccessMode.ZeroPage, 5),
        (0xd6, AccessMode.ZeroPageX, 6),
        (0xce, AccessMode.Absolute, 6),
        (0xde, AccessMode.AbsoluteX, 7),
    };
}

public class Eor : CpuOpCode
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0x49, AccessMode.Immediate, 2),
        (0x45, AccessMode.ZeroPage, 3),
        (0x55, AccessMode.ZeroPageX, 4),
        (0x4d, AccessMode.Absolute, 4),
        (0x5d, AccessMode.AbsoluteX, 4),
        (0x59, AccessMode.AbsoluteY, 4),
        (0x41, AccessMode.IndirectX, 6),
        (0x51, AccessMode.IndirectY, 5),
        (0x52, AccessMode.ZeroPageIndirect, 5),
    };
}

public abstract class FlagInstruction : CpuOpCode
{
}

public class Clc : FlagInstruction
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0x18, AccessMode.Implied, 2)
    };
}

public class Sec : FlagInstruction
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0x38, AccessMode.Implied, 2)
    };

}

public class Cli : FlagInstruction
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0x58, AccessMode.Implied, 2)
    };
}

public class Sei : FlagInstruction
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0x78, AccessMode.Implied, 2)
    };
}

public class Clv : FlagInstruction
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0xb8, AccessMode.Implied, 2)
    };
}

public class Cld : FlagInstruction
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0xd8, AccessMode.Implied, 2)
    };
}

public class Sed : FlagInstruction
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0xf8, AccessMode.Implied, 2)
    };
}

public class Inc : CpuOpCode
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0x1a, AccessMode.Implied, 2),
        (0xe6, AccessMode.ZeroPage, 5),
        (0xf6, AccessMode.ZeroPageX, 6),
        (0xee, AccessMode.Absolute, 6),
        (0xfe, AccessMode.AbsoluteX, 7),
    };
}

public class Jmp : CpuOpCode
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0x4c, AccessMode.Absolute, 3),
        (0x6c, AccessMode.Indirect, 5),
        (0x7c, AccessMode.IndAbsoluteX, 6),
    };
}

public class Jsr : CpuOpCode
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0x20, AccessMode.Absolute, 6),
    };
}

public class Lda : CpuOpCode
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0xa9, AccessMode.Immediate, 2),
        (0xa5, AccessMode.ZeroPage, 3),
        (0xb5, AccessMode.ZeroPageX, 4),
        (0xad, AccessMode.Absolute, 4),
        (0xbd, AccessMode.AbsoluteX, 4),
        (0xb9, AccessMode.AbsoluteY, 4),
        (0xa1, AccessMode.IndirectX, 6),
        (0xb1, AccessMode.IndirectY, 5),
        (0xb2, AccessMode.ZeroPageIndirect, 5)
    };
}

public class Ldd : CpuOpCode
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0xdc, AccessMode.Absolute, 4),
        (0xfc, AccessMode.Absolute, 4)
    };
}

public class Ldx : CpuOpCode
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0xa2, AccessMode.Immediate, 2),
        (0xa6, AccessMode.ZeroPage, 3),
        (0xb6, AccessMode.ZeroPageY, 4),
        (0xae, AccessMode.Absolute, 4),
        (0xbe, AccessMode.AbsoluteY, 4),
    };
}

public class Ldy : CpuOpCode
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0xa0, AccessMode.Immediate, 2),
        (0xa4, AccessMode.ZeroPage, 3),
        (0xb4, AccessMode.ZeroPageX, 4),
        (0xac, AccessMode.Absolute, 4),
        (0xbc, AccessMode.AbsoluteX, 4),
    };
}

public class Lsr : CpuOpCode
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0x4a, AccessMode.Accumulator, 2),
        (0x46, AccessMode.ZeroPage, 5),
        (0x56, AccessMode.ZeroPageX, 6),
        (0x4e, AccessMode.Absolute, 6),
        (0x5e, AccessMode.AbsoluteX, 7),
    };
}

public class Nop : CpuOpCode
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0xea, AccessMode.Implied, 2),
    };
}

public class Ora : CpuOpCode
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0x09, AccessMode.Immediate, 2),
        (0x05, AccessMode.ZeroPage, 3),
        (0x15, AccessMode.ZeroPageX, 4),
        (0x0d, AccessMode.Absolute, 4),
        (0x1d, AccessMode.AbsoluteX, 4),
        (0x19, AccessMode.AbsoluteY, 4),
        (0x01, AccessMode.IndirectX, 6),
        (0x11, AccessMode.IndirectY, 5),
        (0x12, AccessMode.ZeroPageIndirect, 5),
    };
}

public class Rol : CpuOpCode
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0x2a, AccessMode.Accumulator, 2),
        (0x26, AccessMode.ZeroPage, 5),
        (0x36, AccessMode.ZeroPageX, 6),
        (0x2e, AccessMode.Absolute, 6),
        (0x3e, AccessMode.AbsoluteX, 7),
    };
}

public class Ror : CpuOpCode
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0x6a, AccessMode.Accumulator, 2),
        (0x66, AccessMode.ZeroPage, 5),
        (0x76, AccessMode.ZeroPageX, 6),
        (0x6e, AccessMode.Absolute, 6),
        (0x7e, AccessMode.AbsoluteX, 7),
    };
}

public class Rti : CpuOpCode
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0x40, AccessMode.Implied, 6),
    };
}

public class Rts : CpuOpCode
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0x60, AccessMode.Implied, 6),
    };
}

public class Sbc : CpuOpCode
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0xe9, AccessMode.Immediate, 2),
        (0xe5, AccessMode.ZeroPage, 3),
        (0xf5, AccessMode.ZeroPageX, 4),
        (0xed, AccessMode.Absolute, 4),
        (0xfd, AccessMode.AbsoluteX, 4),
        (0xf9, AccessMode.AbsoluteY, 4),
        (0xe1, AccessMode.IndirectX, 6),
        (0xf1, AccessMode.IndirectY, 5),
        (0xf2, AccessMode.ZeroPageIndirect, 5),
    };
}

public class Sta : CpuOpCode
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0x85, AccessMode.ZeroPage, 3),
        (0x95, AccessMode.ZeroPageX, 4),
        (0x8d, AccessMode.Absolute, 4),
        (0x9d, AccessMode.AbsoluteX, 5),
        (0x99, AccessMode.AbsoluteY, 5),
        (0x81, AccessMode.IndirectX, 6),
        (0x91, AccessMode.IndirectY, 6),
        (0x92, AccessMode.ZeroPageIndirect, 5),
    };
}

public class Stz : CpuOpCode
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0x64, AccessMode.ZeroPage, 3),
        (0x74, AccessMode.ZeroPageX, 4),
        (0x9c, AccessMode.Absolute, 4),
        (0x9e, AccessMode.AbsoluteX, 5),
    };
}

public class Stx : CpuOpCode
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0x86, AccessMode.ZeroPage, 3),
        (0x96, AccessMode.ZeroPageY, 4),
        (0x8e, AccessMode.Absolute, 4),
    };
}

public class Sty : CpuOpCode
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0x84, AccessMode.ZeroPage, 3),
        (0x94, AccessMode.ZeroPageX, 4),
        (0x8c, AccessMode.Absolute, 4),
    };
}

public class Tax : CpuOpCode
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0xaa, AccessMode.Implied, 2),
    };
}

public class Txa : CpuOpCode
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0x8a, AccessMode.Implied, 2),
    };
}

public class Dex : CpuOpCode
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0xca, AccessMode.Implied, 2),
    };
}

public class Inx : CpuOpCode
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0xe8, AccessMode.Implied, 2),
    };
}

public class Tay : CpuOpCode
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0xa8, AccessMode.Implied, 2),
    };
}

public class Tya : CpuOpCode
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0x98, AccessMode.Implied, 2),
    };
}

public class Dey : CpuOpCode
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0x88, AccessMode.Implied, 2),
    };
}

public class Iny : CpuOpCode
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0xc8, AccessMode.Implied, 2),
    };
}

public class Txs : CpuOpCode
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0x9a, AccessMode.Implied, 2),
    };
}

public class Tsx : CpuOpCode
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0xba, AccessMode.Implied, 2),
    };
}

public class Pha : CpuOpCode
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0x48, AccessMode.Implied, 3),
    };
}

public class Pla : CpuOpCode
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0x68, AccessMode.Implied, 4),
    };
}

public class Php : CpuOpCode
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0x08, AccessMode.Implied, 3),
    };
}

public class Plp : CpuOpCode
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0x28, AccessMode.Implied, 4),
    };
}

public class Plx : CpuOpCode
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0xfa, AccessMode.Implied, 4),
    };
}

public class Ply : CpuOpCode
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0x7a, AccessMode.Implied, 4),
    };
}

public class Phx : CpuOpCode
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0xda, AccessMode.Implied, 3),
    };
}

public class Phy : CpuOpCode
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0x5a, AccessMode.Implied, 3),
    };
}

public class Trb : CpuOpCode
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0x14, AccessMode.ZeroPage, 5),
        (0x1c, AccessMode.Absolute, 6),
    };
}

public class Tsb : CpuOpCode
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0x04, AccessMode.ZeroPage, 5),
        (0x0c, AccessMode.Absolute, 6),
    };
}

public abstract class BbBase : CpuOpCode
{
}

public class Bbr0 : BbBase
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0x0f, AccessMode.ZerpPageRel, 5),
    };
}

public class Bbr1 : BbBase
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0x1f, AccessMode.ZerpPageRel, 5),
    };
}

public class Bbr2 : BbBase
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0x2f, AccessMode.ZerpPageRel, 5),
    };
}

public class Bbr3 : BbBase
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0x3f, AccessMode.ZerpPageRel, 5),
    };
}

public class Bbr4 : BbBase
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0x4f, AccessMode.ZerpPageRel, 5),
    };
}

public class Bbr5 : BbBase
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0x5f, AccessMode.ZerpPageRel, 5),
    };
}

public class Bbr6 : BbBase
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0x6f, AccessMode.ZerpPageRel, 5),
    };
}

public class Bbr7 : BbBase
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0x7f, AccessMode.ZerpPageRel, 5),
    };
}
public class Bbs0 : BbBase
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0x8f, AccessMode.ZerpPageRel, 5),
    };
}

public class Bbs1 : BbBase
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0x9f, AccessMode.ZerpPageRel, 5),
    };
}

public class Bbs2 : BbBase
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0xaf, AccessMode.ZerpPageRel, 5),
    };
}

public class Bbs3 : BbBase
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0xbf, AccessMode.ZerpPageRel, 5),
    };
}

public class Bbs4 : BbBase
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0xcf, AccessMode.ZerpPageRel, 5),
    };
}

public class Bbs5 : BbBase
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0xdf, AccessMode.ZerpPageRel, 5),
    };
}

public class Bbs6 : BbBase
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0xef, AccessMode.ZerpPageRel, 5),
    };
}

public class Bbs7 : BbBase
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0xff, AccessMode.ZerpPageRel, 5),
    };
}

public abstract class MbBase : CpuOpCode
{
}

public class Rmb0 : MbBase
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0x07, AccessMode.ZeroPage, 5),
    };
}

public class Rmb1 : MbBase
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0x17, AccessMode.ZeroPage, 5),
    };
}

public class Rmb2 : MbBase
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0x27, AccessMode.ZeroPage, 5),
    };
}

public class Rmb3 : MbBase
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0x37, AccessMode.ZeroPage, 5),
    };
}

public class Rmb4 : MbBase
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0x47, AccessMode.ZeroPage, 5),
    };
}

public class Rmb5 : MbBase
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0x57, AccessMode.ZeroPage, 5),
    };
}

public class Rmb6 : MbBase
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0x67, AccessMode.ZeroPage, 5),
    };
}

public class Rmb7 : MbBase
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0x77, AccessMode.ZeroPage, 5),
    };
}

public class Smb0 : MbBase
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0x87, AccessMode.ZeroPage, 5),
    };
}

public class Smb1 : MbBase
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0x97, AccessMode.ZeroPage, 5),
    };
}

public class Smb2 : MbBase
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0xa7, AccessMode.ZeroPage, 5),
    };
}

public class Smb3 : MbBase
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0xb7, AccessMode.ZeroPage, 5),
    };
}

public class Smb4 : MbBase
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0xc7, AccessMode.ZeroPage, 5),
    };
}

public class Smb5 : MbBase
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0xd7, AccessMode.ZeroPage, 5),
    };
}

public class Smb6 : MbBase
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0xe7, AccessMode.ZeroPage, 5),
    };
}

public class Smb7 : MbBase
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0xf7, AccessMode.ZeroPage, 5),
    };
}

public class Wai : CpuOpCode
{
    internal override List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes => new()
    {
        (0xcb, AccessMode.Implied, 3),
    };
}

