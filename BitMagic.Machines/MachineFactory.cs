﻿using BitMagic.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace BitMagic.Machines;

public enum Machine
{
    NoMachine,
    CommanderX16R38,
    CommanderX16R39,
    CommanderX16R40,
    CommanderX16R41,
    CommanderX16R42,
    CommanderX16R43,
    CommanderX16R44,
    CommanderX16R45,
    CommanderX16R46,
    CommanderX16R47,
    CommanderX16R48,
    CommanderX16
}

public static class MachineFactory
{
    public static IMachine? GetMachine(string name)
    {
        var machine = Enum.Parse<Machine>(name);
        return GetMachine(machine);
    }

    public static IMachine? GetMachine(Machine machine) => machine switch
    {
        Machine.NoMachine => new NoMachine(),
        Machine.CommanderX16R38 => new CommanderX16R38(),
        Machine.CommanderX16R39 => new CommanderX16R39(),
        Machine.CommanderX16R40 => new CommanderX16R39(),
        Machine.CommanderX16R41 => new CommanderX16R39(),
        Machine.CommanderX16R42 => new CommanderX16R39(),
        Machine.CommanderX16R43 => new CommanderX16R39(),
        Machine.CommanderX16R44 => new CommanderX16R39(),
        Machine.CommanderX16R45 => new CommanderX16R39(),
        Machine.CommanderX16R46 => new CommanderX16R39(),
        Machine.CommanderX16R47 => new CommanderX16R39(),
        Machine.CommanderX16R48 => new CommanderX16R39(),
        Machine.CommanderX16 => new CommanderX16R39(),
        _ => null
    };
}

public class NoMachine : IMachine
{
    public string Name => "NoMachine";

    public int Version => 0;

    public ICpu Cpu { get; set; } = new NoCpu();
    ICpu IMachine.Cpu => Cpu;

    private IVariables _variables = new NoVariables();
    IVariables IMachine.Variables => _variables;
}

public class NoCpu : ICpu
{
    public string Name => "NoCpu";

    public IEnumerable<ICpuOpCode> OpCodes => Array.Empty<ICpuOpCode>();

    public IReadOnlyDictionary<AccessMode, IParametersDefinition> ParameterDefinitions => throw new NotImplementedException();

    public int OpCodeBytes => 1;
}

internal class NoVariables : IVariables
{
    public IReadOnlyDictionary<string, IAsmVariable> Values => new Dictionary<string, IAsmVariable>();
    public IList<IAsmVariable> AmbiguousVariables => Array.Empty<IAsmVariable>();

    public bool TryGetValue(string name, SourceFilePosition source, out IAsmVariable? result)
    {
        result = default;
        return false;
    }

    public bool TryGetValue(int value, SourceFilePosition source, out IAsmVariable? result)
    {
        result = default;
        return false;
    }
}
