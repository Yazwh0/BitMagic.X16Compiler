using System;
using BitMagic.Compiler.Exceptions;
using System.Collections.Generic;
using System.Linq;

namespace BitMagic.Compiler;

internal interface IWriter
{
    void Add(byte toAdd, int address, uint debugData);
    void Add(byte[] toAdd, int address, uint[] debugData);
    void SetHeader(IEnumerable<byte> toAdd);
    NamedStream Write();
}

internal class FileWriter : IWriter
{
    public string FileName { get; }
    public string SegmentName { get; }
    public bool IsMain { get; }

    private byte[] _header;
    private List<byte> _data = new List<byte>(0x10000);
    private List<uint> _debugData = new List<uint>();
    private int _startAddress;

    public FileWriter(string segmentName, string fileName, int startAddress, bool main)
    {
        SegmentName = segmentName;
        FileName = fileName;
        _startAddress = startAddress;
        _header = Array.Empty<byte>();
        IsMain = main;
    }

    public void Add(byte toAdd, int address, uint debugData) => Add([toAdd], address, [debugData]);

    public void Add(byte[] toAdd, int address, uint[] debugData)
    {
        var index = address - _startAddress;

        if (index < 0)
            throw new FileWriterOverlapException($"Cannot write to ${address:X4}, it is before the start of segment '{SegmentName}' (${_startAddress:X4}).");

        while (_data.Count < index + toAdd.Length)
        {
            _data.Add(0x00);
            _debugData.Add(0x00);
        }

        for(var i = 0; i < toAdd.Length; i++)
        {
            if (_data[index] != 0)
                throw new FileWriterOverlapException($"Writing to ${_startAddress + index:X4} overwrites existing data in segment '{SegmentName}' ('{FileName}').");

            _debugData[index] = debugData[i];
            _data[index++] = toAdd[i];
        }
    }

    public bool HasData => _data.Count > 0;

    public void SetHeader(IEnumerable<byte> toAdd)
    {
        _header = toAdd.ToArray();
    }

    public NamedStream Write() => new (SegmentName, FileName, _header.Concat(_data).ToArray(), _debugData.ToArray(), IsMain, _header.Length != 0);        
}
