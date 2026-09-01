using System;
using System.Collections.Generic;

namespace PM5Control.Core.WirelessSecurityLab.Rfid
{
    public enum ChecksumType
    {
        None,
        Xor,
        Lrc,
        Bcc,
        Crc
    }

    public sealed class PatternReport
    {
        public byte[]? CommonPrefix { get; init; }
        public double PrefixConfidence { get; init; }
        public Dictionary<int, double> ByteEntropy { get; init; } = new();
        public bool SequentialCorrelationDetected { get; init; }
        public ChecksumType DetectedChecksum { get; init; }
        public byte? ChecksumPosition { get; init; }
    }
}
