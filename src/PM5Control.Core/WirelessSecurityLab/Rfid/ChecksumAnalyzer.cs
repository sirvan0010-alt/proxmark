using System;
using System.Linq;

namespace PM5Control.Core.WirelessSecurityLab.Rfid
{
    public sealed class ChecksumAnalyzer
    {
        public ChecksumType DetectChecksumType(byte[] uid)
        {
            if (uid is null || uid.Length < 2) return ChecksumType.None;

            byte xor = 0;
            for (int i = 0; i < uid.Length - 1; i++) xor ^= uid[i];
            if (xor == uid[^1]) return ChecksumType.Xor;

            int sum = 0;
            for (int i = 0; i < uid.Length - 1; i++) sum += uid[i];
            if ((byte)(-sum) == uid[^1]) return ChecksumType.Lrc;

            return ChecksumType.None;
        }

        public byte[]? RecalculateChecksum(byte[] uidWithoutChecksum, ChecksumType type)
        {
            if (uidWithoutChecksum is null || uidWithoutChecksum.Length == 0) return null;

            byte cs;
            switch (type)
            {
                case ChecksumType.Xor:
                    cs = 0;
                    for (int i = 0; i < uidWithoutChecksum.Length; i++) cs ^= uidWithoutChecksum[i];
                    break;
                case ChecksumType.Lrc:
                    int sum = 0;
                    for (int i = 0; i < uidWithoutChecksum.Length; i++) sum += uidWithoutChecksum[i];
                    cs = (byte)(-sum);
                    break;
                default:
                    return null;
            }

            var result = new byte[uidWithoutChecksum.Length + 1];
            Buffer.BlockCopy(uidWithoutChecksum, 0, result, 0, uidWithoutChecksum.Length);
            result[^1] = cs;
            return result;
        }
    }
}
