namespace PM5Control.Core.Protocols.Bwm;

/// <summary>
/// Read-only capability metadata for PM5 BWM hardware features identified by
/// the current RfidResearchGroup/NielDK firmware tree.
///
/// This type deliberately describes capabilities only. It does not send
/// charger, LED, buzzer, power, Wi-Fi, OTA, reboot, or configuration commands.
/// </summary>
public enum BwmHardwareFeature
{
    ChargerAndFuelGauge,
    ConfigurableChargeVoltage,
    RgbPowerBatteryIndicator,
    Buzzer,
    WifiForwarding,
    WifiConfiguration,
    TcpUdpNetworking,
    Mqtt,
    Bluetooth,
    Passthrough
}

public static class BwmHardwareFeatureCatalog
{
    /// <summary>
    /// Features evidenced by the upstream PM5/BWM source audit. These are
    /// protocol/source capabilities, not proof that a particular connected
    /// device contains or exposes the hardware.
    /// </summary>
    public static IReadOnlySet<BwmHardwareFeature> UpstreamPm5Features { get; } =
        new HashSet<BwmHardwareFeature>
        {
            BwmHardwareFeature.ChargerAndFuelGauge,
            BwmHardwareFeature.ConfigurableChargeVoltage,
            BwmHardwareFeature.RgbPowerBatteryIndicator,
            BwmHardwareFeature.Buzzer,
            BwmHardwareFeature.WifiForwarding,
            BwmHardwareFeature.WifiConfiguration,
            BwmHardwareFeature.TcpUdpNetworking,
            BwmHardwareFeature.Mqtt,
            BwmHardwareFeature.Bluetooth,
            BwmHardwareFeature.Passthrough
        };

    /// <summary>
    /// Upstream PM5 charger/fuel-gauge components identified by NielDK's
    /// 2026-08-28/30 firmware changes.
    /// </summary>
    public const string ChargerController = "AW32001";
    public const string FuelGauge = "BQ27427";

    /// <summary>
    /// Firmware safety target introduced upstream by PR #3552.
    /// The requested 4100 mV target snaps to 4095 mV because the AW32001E
    /// regulates in 15 mV steps. Firmware reapplies the target at boot.
    /// </summary>
    public const ushort DefaultChargeVoltageTargetMv = 4100;
    public const ushort EffectiveDefaultChargeVoltageMv = 4095;
    public const ushort ChargeVoltageStepMv = 15;
    public const ushort ChargeVoltageMinMv = 3600;
    public const ushort ChargeVoltageMaxMv = 4200;

    /// <summary>
    /// Indicates that this catalog is source-derived and must not be treated
    /// as hardware-verified until the connected PM5/BWM reports the feature.
    /// </summary>
    public const string EvidenceLevel = "PROTOCOL/SOURCE VERIFIED; HARDWARE UNVERIFIED";
}
