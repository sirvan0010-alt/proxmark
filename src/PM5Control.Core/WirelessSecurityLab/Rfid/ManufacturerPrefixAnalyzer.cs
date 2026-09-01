using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace PM5Control.Core.WirelessSecurityLab.Rfid
{
    public sealed class ManufacturerPrefixAnalyzer
    {
        private static readonly Dictionary<byte, string> KnownPrefixes = new()
        {
            { 0x04, "NXP Semiconductors" },
            { 0x08, "Infineon Technologies" },
            { 0x28, "Texas Instruments" },
            { 0x30, "STMicroelectronics" },
            { 0x39, "EM Microelectronic" },
            { 0x40, "Atmel" },
            { 0x50, "MIFARE (NXP)" }
        };

        public string? DetectManufacturer(byte[] uid)
        {
            if (uid is null || uid.Length == 0) return null;
            return KnownPrefixes.TryGetValue(uid[0], out var name) ? name : null;
        }

        public IEnumerable<byte[]> GenerateWithPrefix(byte prefix, int length, int maxCount = 100)
        {
            if (length < 1) yield break;

            var rest = new byte[Math.Max(0, length - 1)];
            for (int i = 0; i < maxCount; i++)
            {
                var full = new byte[length];
                full[0] = prefix;
                if (rest.Length > 0)
                {
                    RandomNumberGenerator.Fill(rest);
                    Buffer.BlockCopy(rest, 0, full, 1, rest.Length);
                }
                yield return full;
            }
        }
    }
}
