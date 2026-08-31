namespace PM5Control.Core.Bwm;

/// <summary>
/// Capability/policy gate for the PM5 Wireless Security Lab.
/// Safe-by-default: discovery, diagnostics and passive capture are allowed;
/// disruptive RF operations are deliberately not enabled by this layer.
/// </summary>
public static class WirelessSecurityLabPolicy
{
    public const string ModuleName = "Wireless Security Lab";
    public const string TargetChip = "ESP32-C2 / ESP8684";

    public static bool SoftApSupported => true;
    public static bool CaptivePortalSupported => true;
    public static bool WifiScanSupported => true;
    public static bool PromiscuousModeSupported => true;
    public static bool BeaconTxSupported => true;

    // Hardware capability is intentionally not treated as authorization.
    // These remain false until independently verified on the real PM5 BWM hardware.
    public static bool DeauthenticationVerified => false;
    public static bool DisassociationVerified => false;
    public static bool AuthenticationFloodVerified => false;

    public static bool IsSafeLabOperation(string operation) => operation switch
    {
        "SCAN" or "PASSIVE_SNIFF" or "SOFTAP" or "CAPTIVE_PORTAL_TEST" or "BEACON_TEST" => true,
        _ => false
    };
}
