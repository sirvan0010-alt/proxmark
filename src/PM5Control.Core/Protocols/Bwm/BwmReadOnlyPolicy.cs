// PM5 Control Center — BWM read-only command policy v2
// PURPOSE: central, explicit allow-list for the first hardware-safe BWM
// inspector. This policy is intentionally narrower than every GET_* command
// in the upstream enum: some GET operations expose credentials/private keys
// or belong to later network-management workflows.
// PROVENANCE: command IDs mirror RfidResearchGroup/Proxmark5_BWM_esp32
// app_com_defs.h at commit b918166128e05455c2dcb4e232216d453bbf29ee.
// SAFETY: do not replace this with enum-name/reflection heuristics. New
// commands must be explicitly reviewed and added.

namespace PM5Control.Core.Protocols.Bwm;

public static class BwmReadOnlyPolicy
{
    private static readonly HashSet<BwmCommandCode> Allowed = new()
    {
        BwmCommandCode.GetVersionInfo,
        BwmCommandCode.GetDeviceModel,
        BwmCommandCode.GetSysFreeHeap,
        BwmCommandCode.GetSysTimestamp,
        BwmCommandCode.GetAppCompileDatetime,
        BwmCommandCode.GetSysTimeZone,
        BwmCommandCode.GetSysBaseMacAddr,
        BwmCommandCode.GetSysUartCmdBaudRate,
        BwmCommandCode.GetSysUartCmdMaxBaudRate,
        BwmCommandCode.GetSysNvsStats,
        BwmCommandCode.GetLogUartForwardEnable,
        BwmCommandCode.GetLogLevel,
        BwmCommandCode.GetSysReadyStatus,
    };

    public static IReadOnlySet<BwmCommandCode> AllowedCommands => Allowed;

    public static bool IsAllowed(BwmCommandCode command) => Allowed.Contains(command);
}
