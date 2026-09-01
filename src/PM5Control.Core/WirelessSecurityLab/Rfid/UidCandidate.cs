using System;
using System.Collections.Generic;

namespace PM5Control.Core.WirelessSecurityLab.Rfid
{
    public sealed record UidCandidate
    {
        public byte[] Uid { get; init; } = Array.Empty<byte>();
        public string Protocol { get; init; } = "Unknown";
        public int BitLength => Uid.Length * 8;
        public string Hex => Convert.ToHexString(Uid);
        public string Source { get; init; } = "Generated";
        public double Score { get; set; }
    }

    public sealed class UidEqualityComparer : IEqualityComparer<UidCandidate>
    {
        public bool Equals(UidCandidate? x, UidCandidate? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;
            if (x.Uid.Length != y.Uid.Length) return false;
            for (int i = 0; i < x.Uid.Length; i++)
                if (x.Uid[i] != y.Uid[i]) return false;
            return true;
        }

        public int GetHashCode(UidCandidate obj)
        {
            if (obj.Uid is null || obj.Uid.Length == 0) return 0;
            int hash = 17;
            foreach (var b in obj.Uid)
                hash = hash * 31 + b;
            return hash;
        }
    }
}
