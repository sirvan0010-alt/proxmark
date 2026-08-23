namespace PM5Control.Core.Protocols.Pm3;

/// <summary>
/// Read-only Proxmark3/PM5 ARM command identifiers verified against
/// RfidResearchGroup/proxmark3 include/pm3_cmd.h.
/// </summary>
public static class Pm3CommandCode
{
    public const ushort Version = 0x0107;       // CMD_VERSION
    public const ushort Capabilities = 0x0112;  // CMD_CAPABILITIES
}
