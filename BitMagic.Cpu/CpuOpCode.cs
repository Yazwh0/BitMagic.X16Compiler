﻿using BitMagic.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BitMagic.Cpu
{
    public abstract class CpuOpCode : ICpuOpCode
    {
        internal abstract List<(uint OpCode, AccessMode Mode, int Timing)> OpCodes { get; }
        public virtual string Code => GetType().Name.ToUpper();
        public uint GetOpCode(AccessMode mode) => OpCodes.Any(i => i.Mode == mode) ? OpCodes.First(i => i.Mode == mode).OpCode : throw new Exception($"Unknown access mode for {Code} {mode}");
        public IEnumerable<AccessMode> Modes => OpCodes.Select(i => i.Mode);
        public virtual int OpCodeLength => 1;
    }
}
