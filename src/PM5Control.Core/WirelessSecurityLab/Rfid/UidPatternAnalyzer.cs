using System;
using System.Collections.Generic;
using System.Linq;

namespace PM5Control.Core.WirelessSecurityLab.Rfid
{
    public sealed class UidPatternAnalyzer
    {
        public PatternReport Analyze(IEnumerable<byte[]> knownUids)
        {
            var list = knownUids?.ToList() ?? new List<byte[]>();
            if (list.Count == 0)
                return new PatternReport();

            var commonPrefix = FindCommonPrefix(list);
            var entropy = CalculateByteEntropy(list);
            var sequential = DetectSequential(list);
            var checksum = DetectChecksum(list);

            double prefixConfidence = 0;
            if (commonPrefix is { Length: > 0 })
            {
                int matching = list.Count(u =>
                    u.Length >= commonPrefix.Length &&
                    u.AsSpan(0, commonPrefix.Length).SequenceEqual(commonPrefix));
                prefixConfidence = (double)matching / list.Count;
            }

            return new PatternReport
            {
                CommonPrefix = commonPrefix,
                PrefixConfidence = prefixConfidence,
                ByteEntropy = entropy,
                SequentialCorrelationDetected = sequential,
                DetectedChecksum = checksum,
                ChecksumPosition = checksum != ChecksumType.None && list[0].Length > 0
                    ? (byte)(list[0].Length - 1)
                    : null
            };
        }

        private static byte[]? FindCommonPrefix(List<byte[]> uids)
        {
            if (uids.Count == 0 || uids[0].Length == 0) return null;

            int minLen = uids.Min(u => u.Length);
            int common = 0;
            for (int i = 0; i < minLen; i++)
            {
                byte b = uids[0][i];
                if (uids.All(u => u.Length > i && u[i] == b))
                    common++;
                else
                    break;
            }
            return common == 0 ? null : uids[0].AsSpan(0, common).ToArray();
        }

        private static Dictionary<int, double> CalculateByteEntropy(List<byte[]> uids)
        {
            var result = new Dictionary<int, double>();
            if (uids.Count == 0) return result;

            int len = uids.Min(u => u.Length);
            for (int pos = 0; pos < len; pos++)
            {
                var values = uids.Select(u => u[pos]).ToList();
                var freq = values.GroupBy(b => b).ToDictionary(g => g.Key, g => g.Count());
                double entropy = 0.0;
                foreach (var count in freq.Values)
                {
                    double p = (double)count / values.Count;
                    entropy -= p * Math.Log2(p);
                }
                result[pos] = entropy;
            }
            return result;
        }

        private static bool DetectSequential(List<byte[]> uids)
        {
            if (uids.Count < 2) return false;

            var ints = uids.Select(u =>
            {
                long val = 0;
                for (int i = 0; i < Math.Min(u.Length, 8); i++)
                    val = (val << 8) | u[i];
                return val;
            }).OrderBy(x => x).ToList();

            long diff = ints[1] - ints[0];
            if (diff == 0) return false;
            for (int i = 2; i < ints.Count; i++)
                if (ints[i] - ints[i - 1] != diff)
                    return false;
            return true;
        }

        private static ChecksumType DetectChecksum(List<byte[]> uids)
        {
            if (uids.Count < 2 || uids[0].Length < 2) return ChecksumType.None;

            int len = uids[0].Length;
            if (uids.Any(u => u.Length != len)) return ChecksumType.None;

            bool xorOk = uids.All(u =>
            {
                byte xor = 0;
                for (int i = 0; i < len - 1; i++) xor ^= u[i];
                return xor == u[len - 1];
            });
            if (xorOk) return ChecksumType.Xor;

            bool lrcOk = uids.All(u =>
            {
                int sum = 0;
                for (int i = 0; i < len - 1; i++) sum += u[i];
                return (byte)(-sum) == u[len - 1];
            });
            if (lrcOk) return ChecksumType.Lrc;

            return ChecksumType.None;
        }
    }
}
