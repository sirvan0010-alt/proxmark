using System;

namespace PM5Control.Core.WirelessSecurityLab.Rfid
{
    [Flags]
    public enum FuzzingStrategy
    {
        None = 0,
        BitFlip = 1 << 0,
        NibbleByteMutation = 1 << 1,
        IncrementalDecremental = 1 << 2,
        PatternBased = 1 << 3,
        ChecksumBrute = 1 << 4,
        ManufacturerPrefix = 1 << 5,
        All = BitFlip | NibbleByteMutation | IncrementalDecremental | PatternBased | ChecksumBrute | ManufacturerPrefix
    }
}
