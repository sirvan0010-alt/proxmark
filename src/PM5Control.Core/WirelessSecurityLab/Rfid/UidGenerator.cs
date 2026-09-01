using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace PM5Control.Core.WirelessSecurityLab.Rfid
{
    /// <summary>
    /// Offline UID candidate generator. Multiple strategies are combined (union).
    /// Does not communicate with hardware.
    /// </summary>
    public sealed class UidGenerator
    {
        public IEnumerable<UidCandidate> GenerateCandidates(
            byte[] baseUid,
            FuzzingStrategy strategies,
            int maxCandidates = 1000)
        {
            if (baseUid is null) throw new ArgumentNullException(nameof(baseUid));
            if (baseUid.Length == 0) return Array.Empty<UidCandidate>();

            var results = new HashSet<UidCandidate>(new UidEqualityComparer());

            if (strategies.HasFlag(FuzzingStrategy.BitFlip))
                foreach (var c in GenerateBitFlips(baseUid)) results.Add(c);

            if (strategies.HasFlag(FuzzingStrategy.NibbleByteMutation))
                foreach (var c in GenerateNibbleMutations(baseUid)) results.Add(c);

            if (strategies.HasFlag(FuzzingStrategy.IncrementalDecremental))
                foreach (var c in GenerateIncrementalDecremental(baseUid)) results.Add(c);

            if (strategies.HasFlag(FuzzingStrategy.PatternBased))
                foreach (var c in GeneratePatternBased(baseUid)) results.Add(c);

            if (strategies.HasFlag(FuzzingStrategy.ChecksumBrute))
                foreach (var c in GenerateChecksumBrute(baseUid)) results.Add(c);

            if (strategies.HasFlag(FuzzingStrategy.ManufacturerPrefix))
                foreach (var c in GenerateManufacturerPrefix(baseUid)) results.Add(c);

            return results.Take(maxCandidates);
        }

        private static IEnumerable<UidCandidate> GenerateBitFlips(byte[] uid)
        {
            for (int i = 0; i < uid.Length; i++)
            {
                for (int bit = 0; bit < 8; bit++)
                {
                    var mutated = (byte[])uid.Clone();
                    mutated[i] ^= (byte)(1 << bit);
                    yield return new UidCandidate { Uid = mutated, Protocol = "ISO14443-A", Source = "BitFlip" };
                }
            }
        }

        private static IEnumerable<UidCandidate> GenerateNibbleMutations(byte[] uid)
        {
            for (int i = 0; i < uid.Length; i++)
            {
                for (int nibble = 0; nibble < 16; nibble++)
                {
                    var high = (byte[])uid.Clone();
                    high[i] = (byte)((high[i] & 0x0F) | (nibble << 4));
                    yield return new UidCandidate { Uid = high, Protocol = "ISO14443-A", Source = "Nibble" };

                    var low = (byte[])uid.Clone();
                    low[i] = (byte)((low[i] & 0xF0) | nibble);
                    yield return new UidCandidate { Uid = low, Protocol = "ISO14443-A", Source = "Nibble" };
                }
            }
        }

        private static IEnumerable<UidCandidate> GenerateIncrementalDecremental(byte[] uid, int radius = 10)
        {
            for (int offset = -radius; offset <= radius; offset++)
            {
                if (offset == 0) continue;
                var candidate = (byte[])uid.Clone();
                if (TryAddOffset(candidate, offset))
                    yield return new UidCandidate { Uid = candidate, Protocol = "ISO14443-A", Source = "Sequential" };
            }
        }

        private static bool TryAddOffset(byte[] uid, int offset)
        {
            int carry = offset;
            for (int i = uid.Length - 1; i >= 0 && carry != 0; i--)
            {
                int sum = uid[i] + carry;
                uid[i] = (byte)(sum & 0xFF);
                carry = sum >> 8;
            }
            return carry == 0;
        }

        private static IEnumerable<UidCandidate> GeneratePatternBased(byte[] uid)
        {
            var mirrored = uid.Reverse().ToArray();
            yield return new UidCandidate { Uid = mirrored, Protocol = "ISO14443-A", Source = "Pattern" };

            var repeated = Enumerable.Repeat(uid[0], uid.Length).ToArray();
            yield return new UidCandidate { Uid = repeated, Protocol = "ISO14443-A", Source = "Pattern" };

            if (uid.Length <= 4)
            {
                for (int start = 0; start <= 255 - uid.Length; start++)
                {
                    var seq = Enumerable.Range(start, uid.Length).Select(b => (byte)b).ToArray();
                    yield return new UidCandidate { Uid = seq, Protocol = "ISO14443-A", Source = "Pattern" };
                }
            }
        }

        private static IEnumerable<UidCandidate> GenerateChecksumBrute(byte[] uid)
        {
            if (uid.Length == 0) yield break;
            var prefix = uid.AsSpan(0, uid.Length - 1).ToArray();
            for (int cs = 0; cs < 256; cs++)
            {
                var mutated = new byte[uid.Length];
                Buffer.BlockCopy(prefix, 0, mutated, 0, prefix.Length);
                mutated[^1] = (byte)cs;
                yield return new UidCandidate { Uid = mutated, Protocol = "ISO14443-A", Source = "Checksum" };
            }
        }

        private static IEnumerable<UidCandidate> GenerateManufacturerPrefix(byte[] uid)
        {
            if (uid.Length < 2) yield break;
            byte prefix = uid[0];
            int restLen = uid.Length - 1;
            var randomBytes = new byte[restLen];

            if (restLen <= 2)
            {
                int maxVal = 1 << (8 * restLen);
                for (int i = 0; i < maxVal; i++)
                {
                    var full = new byte[uid.Length];
                    full[0] = prefix;
                    for (int j = 0; j < restLen; j++)
                        full[1 + j] = (byte)((i >> (8 * j)) & 0xFF);
                    yield return new UidCandidate { Uid = full, Protocol = "ISO14443-A", Source = "ManufacturerPrefix" };
                }
            }
            else
            {
                for (int i = 0; i < 100; i++)
                {
                    RandomNumberGenerator.Fill(randomBytes);
                    var full = new byte[uid.Length];
                    full[0] = prefix;
                    Buffer.BlockCopy(randomBytes, 0, full, 1, restLen);
                    yield return new UidCandidate { Uid = full, Protocol = "ISO14443-A", Source = "ManufacturerPrefix" };
                }
            }
        }
    }
}
