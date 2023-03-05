using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static BitMagic.Decompiler.Addressing;

namespace BitMagic.Decompiler;

public class Decompiler
{
    /// <summary>
    /// Decompile data, using the symbols passed.
    /// </summary>
    /// <param name="data">Data to decompile.</param>
    /// <param name="baseAddress">Base address of the data.</param>
    /// <param name="bank">Bank number for the data.</param>
    /// <param name="symbols">Symbols lookup. Code will never span a symbol.</param>
    /// <returns></returns>
    public DecompileReturn Decompile(byte[] data, int baseAddress, int bank, IReadOnlyDictionary<int, string>? symbols)
    {
        var toReturn = new DecompileReturn();

        var address = baseAddress & 0xffff;
        var bankAddress = (bank & 0xff) << 16;
        var idx = 0;
        var lineNumber = 0;
        symbols ??= new Dictionary<int, string>();

        while (idx < data.Length)
        {
            var item = new DissasemblyItem();
            item.Address = address;
            var debuggerAddress = address;
            if (symbols.ContainsKey(debuggerAddress))
            {
                item.Symbol = symbols[debuggerAddress];
                var symbolLine = new DissasemblyItem()
                {
                    LineNumber = lineNumber,
                    Instruction = $".{item.Symbol}:"
                };
                toReturn.Items.Add(lineNumber, symbolLine);
                lineNumber++;
            }

            var maxLen = 3;
            if (symbols.ContainsKey(debuggerAddress + 1))
                maxLen = 1;
            else if (symbols.ContainsKey(debuggerAddress + 2))
                maxLen = 2;

            var instruction = GetValidInstruction(data, idx, maxLen);

            var values = Addressing.GetModeValue(instruction.AddressMode, instruction.Parameter, address);

            if (values.Value >= 0xc000)
                values.Value += bankAddress;

            if (values.ValueB >= 0xc000)
                values.ValueB += bankAddress;

            string parameterSymbol;
            string parameterSymbolB;
            bool includeComment = false;
            if (symbols.ContainsKey(values.Value))
            {
                parameterSymbol = symbols[values.Value];
                includeComment = true;
            }
            else
                parameterSymbol = Addressing.GetPrimaryValue(instruction.AddressMode, instruction.Parameter, address);

            if (symbols.ContainsKey(values.ValueB))
            {
                parameterSymbolB = symbols[values.ValueB];
                includeComment = true;
            }
            else
                parameterSymbolB = Addressing.GetSecondaryValue(instruction.AddressMode, instruction.Parameter, address);

            item.LineNumber = lineNumber;
            if (instruction.Valid)
            {
                item.Instruction = GetDisassemblyDisplay(instruction.OpCode, instruction.AddressMode, instruction.Parameter, parameterSymbol, parameterSymbolB, address, includeComment);
            }
            else
            {
                item.Instruction = ".byte " + string.Join(", ", instruction.Data.Select(i => i.ToString("X2")));
            }
            item.Data = instruction.Data ?? Array.Empty<byte>();

            toReturn.Items.Add(lineNumber, item);

            idx += instruction.IndexChange;
            address += instruction.IndexChange;
            lineNumber++;
        }

        toReturn.LastAddress = address + bankAddress;
        return toReturn;
    }

    private (bool Valid, string OpCode, AddressMode AddressMode, int Parameter, byte[] Data, int IndexChange) GetValidInstruction(byte[] data, int index, int maxLen)
    {
        var opCode = OpCodes.GetOpcode(data[index]);
        maxLen = Math.Min(maxLen, data.Length - index);

        if (string.IsNullOrWhiteSpace(opCode.OpCode))
            return (false, "", AddressMode.Implied, 0, new byte[] { data[index] }, 1);

        var instructionLength = Addressing.GetInstructionLenth(opCode.AddressMode);

        var instructionData = Math.Min(maxLen, instructionLength) switch
        {
            1 => new byte[] { data[index] },
            2 => new byte[] { data[index], data[index + 1] },
            3 => new byte[] { data[index], data[index + 1], data[index + 2] },
            _ => Array.Empty<byte>(),
        };

        if (index + instructionLength > data.Length)
        {
            return (false, "", AddressMode.Implied, 0, instructionData, instructionData.Length);
        }


        if (instructionLength > maxLen)
        {
            return (false, "", AddressMode.Implied, data[index], instructionData, instructionData.Length);
        }

        var parameter = instructionLength switch
        {
            2 => instructionData[1],
            3 => instructionData[1] + (instructionData[2] << 8),
            _ => 0
        };

        return (true, opCode.OpCode, opCode.AddressMode, parameter, instructionData, instructionData.Length);
    }

    private string GetDisassemblyDisplay(string opCode, AddressMode addressMode, int parameter, string symbol, string symbolB, int address, bool includeComment)
    {
        opCode = opCode.ToLower();

        if (addressMode == AddressMode.Implied || addressMode == AddressMode.Accumulator || addressMode == AddressMode.Immediate || string.IsNullOrWhiteSpace(symbol))
        {
            return $"{opCode} {Addressing.GetModeText(addressMode, parameter, address)}";
        }

        var baseString = $"{opCode} {Addressing.GetModeSymbol(addressMode, symbol, symbolB)}";

        return (includeComment ?
            baseString.PadRight(45) + " ; " + Addressing.GetModeText(addressMode, parameter, address) : baseString);
    }
}

public class DecompileReturn
{
    public int LastAddress { get; set; }
    public Dictionary<int, DissasemblyItem> Items { get; set; } = new();
}

public class DissasemblyItem
{
    public int Address { get; set; }
    public string Symbol { get; set; } = "";
    public string Instruction { get; set; } = "";
    public byte[] Data { get; set; } = Array.Empty<byte>();
    public int LineNumber { get; set; }
}
