namespace PM5Control.Core.Protocols.Pm3;

/// <summary>
/// Read-only Proxmark3/PM5 ARM command identifiers verified against
/// RfidResearchGroup/proxmark3 include/pm3_cmd.h.
/// </summary>
public static class Pm3CommandCode
{
    public const ushort DebugPrintString = 0x0100;
    public const ushort DebugPrintIntegers = 0x0101;
    public const ushort DebugPrintBytes = 0x0102;
    public const ushort Version = 0x0107;       // CMD_VERSION
    public const ushort Status = 0x0108;        // CMD_STATUS - read-only runtime status query
    public const ushort Ping = 0x0109;          // CMD_PING - read-only liveness check
    public const ushort Capabilities = 0x0112;  // CMD_CAPABILITIES

    public static bool IsDebugResponse(ushort command) =>
        command is DebugPrintString or DebugPrintIntegers or DebugPrintBytes;
}
