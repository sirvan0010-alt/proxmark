namespace PM5Control.Core.Bwm;

/// <summary>
/// Project-level safety policy for the PM5 BWM charger target.
/// This is a policy/validation layer; it does not claim that hardware has accepted
/// the value until a real charger-register read-back confirms it.
/// </summary>
public static class BatteryChargeSafetyProfile
{
    /// <summary>Preferred PM5 BWM charge target in millivolts.</summary>
    public const int PreferredChargeVoltageMv = 4100;

    /// <summary>Upstream AW32001E power-on default observed in the current BWM implementation.</summary>
    public const int UpstreamDefaultChargeVoltageMv = 4200;

    /// <summary>Hard project ceiling for automatic configuration.</summary>
    public const int AutomaticConfigurationCeilingMv = PreferredChargeVoltageMv;

    public static bool IsAllowedAutomaticTarget(int millivolts) =>
        millivolts >= 3600 && millivolts <= AutomaticConfigurationCeilingMv;

    public static string GetEvidenceState(bool registerReadbackMatches)
        => registerReadbackMatches ? "HARDWARE_VERIFIED" : "EXPECTED";
}
