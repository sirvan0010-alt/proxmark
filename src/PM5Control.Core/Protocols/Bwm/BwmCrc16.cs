// PM5 Control Center
// PURPOSE: deterministic BWM CRC implementation.
// WHY: packet integrity must be tested independently of transport.
// Do not assume this applies to unrelated Proxmark protocols.
namespace PM5Control.Core.Protocols.Bwm;

public static class BwmCrc16
{
    public static ushort Compute(ReadOnlySpan<byte> data)
    {
        ushort crc = BwmProtocolConstants.CrcInitial;
        foreach (byte b in data)
        {
            crc ^= (ushort)(b << 8);
            for (int i = 0; i < 8; i++)
            {
                crc = (crc & 0x8000) != 0
                    ? (ushort)((crc << 1) ^ BwmProtocolConstants.CrcPolynomial)
                    : (ushort)(crc << 1);
            }
        }
        return crc;
    }
}
