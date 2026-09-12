namespace PM5Control.Core.Bwm;

/// <summary>
/// Safety boundary for PM5 BWM charger work.
///
/// This class intentionally contains no automatic charger target. The current
/// upstream evidence is not sufficient to authorize a write to the charger
/// configuration on an arbitrary physical PM5+BWM unit.
/// </summary>
public static class BatteryChargeSafetyProfile
{
    /// <summary>
    /// Automatic charger configuration is disabled until a verified PM5+BWM
    /// protocol and charger-register read/write path exists and is hardware tested.
    /// </summary>
    public static bool AutomaticConfigurationEnabled => false;

    /// <summary>
    /// Returns whether an automatic charger write is permitted.
    /// It is deliberately false for every value until hardware/protocol evidence
    /// is promoted by an explicit engineering change.
    /// </summary>
    public static bool IsAllowedAutomaticTarget(int millivolts) => false;

    /// <summary>
    /// Charger configuration evidence is never promoted by this policy layer.
    /// A real register read-back must be represented by the device evidence model.
    /// </summary>
    public static string GetEvidenceState(bool registerReadbackMatches)
        => registerReadbackMatches ? "HARDWARE_VERIFIED" : "UNKNOWN";
}
