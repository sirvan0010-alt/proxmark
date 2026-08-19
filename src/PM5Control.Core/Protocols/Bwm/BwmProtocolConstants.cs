// PM5 Control Center
// BWM protocol constants. Values are based on the upstream research snapshot
// documented in docs/BWM_PROTOCOL.md. They must be versioned and revalidated.
namespace PM5Control.Core.Protocols.Bwm;

public static class BwmProtocolConstants
{
    public const ushort RequestMagic = 0xC77C;
    public const ushort ResponseMagic = 0x3D2D;
    public const ushort BroadcastMagic = 0xD3D2;
    public const int DefaultUartBaudRate = 460800;
    public const int CrcInitial = 0xFFFF;
    public const int CrcPolynomial = 0x1021;
    public const int HeaderSize = 6;
    public const int CrcSize = 2;
}
