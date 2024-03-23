using System;
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

    public void Add(byte toAdd, int address, uint debugData)
    {
        var index = _startAddress - address;

        if (index < 0)
            throw new IndexOutOfRangeException();

        while (_data.Count < index)
        {
            _data.Add(0x00);
            _debugData.Add(0x00);
        }

        if (_data[index] != 0)
            throw new Exception("Overwrite detected!");

        _data[index] = toAdd;
        _debugData[index] = debugData;
    }

    public void Add(byte[] toAdd, int address, uint[] debugData)
    {
        var index = address - _startAddress;

        if (index < 0)
            throw new IndexOutOfRangeException();

        while (_data.Count < index + toAdd.Length)
        {
            _data.Add(0x00);
            _debugData.Add(0x00);
        }

        for(var i = 0; i < toAdd.Length; i++)
        {
            if (_data[index] != 0)
                throw new Exception("Overwrite detected!");

            _debugData[index] = debugData[i];
            _data[index++] = toAdd[i];
        }
    }

    public bool HasData => _data.Count > 0;

    public void SetHeader(IEnumerable<byte> toAdd)
    {
        _header = toAdd.ToArray();
    }

    public NamedStream Write() => new (SegmentName, FileName, _header.Concat(_data).ToArray(), _debugData.ToArray(), IsMain);        
}
