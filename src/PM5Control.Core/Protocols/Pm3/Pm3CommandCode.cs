namespace PM5Control.Core.Protocols.Pm3;

/// <summary>
/// PM3/PM5 ARM command identifiers. Only commands explicitly marked read-only
/// are exposed to the safe diagnostic probe.
/// </summary>
public static class Pm3CommandCode
{
    public const ushort DebugPrintString = 0x0100;
    public const ushort DebugPrintIntegers = 0x0101;
    public const ushort DebugPrintBytes = 0x0102;
    public const ushort Version = 0x0107;              // CMD_VERSION
    public const ushort Status = 0x0108;               // CMD_STATUS
    public const ushort Ping = 0x0109;                 // CMD_PING
    public const ushort Capabilities = 0x0112;         // CMD_CAPABILITIES
    public const ushort GetDebugMode = 0x0120;         // CMD_GET_DBGMODE
    public const ushort FlashMemInfo = 0x0125;         // CMD_FLASHMEM_INFO (read-only query)
    public const ushort FlashMemGetSignature = 0x0147; // CMD_FLASHMEM_GET_SIGNATURE (read-only query)
    public const ushort FlashMemGetInfo = 0x0148;      // CMD_FLASHMEM_GET_INFO (read-only query)
    public const ushort LfSamplingGetConfig = 0x0228;  // CMD_LF_SAMPLING_GET_CONFIG

    public const ushort BreakLoop = 0x0118;            // CMD_BREAK_LOOP; not a read-only probe.

    // PM5/BWM commands verified against RfidResearchGroup/proxmark3:
    // 5661f21d6099ac0faf3be52138cea650aa6bd885 and
    // ac9c402e13d2cf735fe7348c975d876ebe324010.
    public const ushort Pm5BwmSetChargeVoltage = 0x017D; // CMD_PM5_BWM_SET_VCHG
    public const ushort Pm5BwmEspOta = 0x017E;             // CMD_PM5_BWM_ESP_OTA
    public const ushort Pm5BwmBleName = 0x017F;            // CMD_PM5_BWM_BLE_NAME
    public const ushort Pm5BwmPowerSave = 0x0181;          // CMD_PM5_BWM_POWERSAVE
    public const ushort Pm5BwmWifiPowerSave = 0x0182;     // CMD_PM5_BWM_WIFI_PS
    public const ushort Pm5BwmBle = 0x0183;                // CMD_PM5_BWM_BLE
    public const ushort Pm5BwmGetBattery = 0x0184;         // CMD_PM5_BWM_GET_BATTERY
    public const ushort CepStatus = 0x0185;                // CMD_CEP_STATUS

    public static bool IsDebugResponse(ushort command) =>
        command is DebugPrintString or DebugPrintIntegers or DebugPrintBytes;

    public static bool IsSafeReadOnlyProbe(ushort command) => command is
        Version or Status or Ping or Capabilities or GetDebugMode or FlashMemInfo or
        FlashMemGetSignature or FlashMemGetInfo or LfSamplingGetConfig or
        Pm5BwmGetBattery or CepStatus;
}
