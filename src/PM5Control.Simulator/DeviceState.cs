namespace PM5Control.Simulator;

/// <summary>
/// Minimal evidence-aware state model used before physical PM5 behaviour is verified.
/// This model intentionally contains no guessed PM5 identifiers or firmware values.
/// </summary>
public sealed class DeviceState
{
    public string HardwareFamily { get; set; } = "PM5";
    public string? HardwareRevision { get; set; }
    public string? ArmFirmware { get; set; }
    public string? FpgaFirmware { get; set; }
    public string? BwmFirmware { get; set; }
    public string Readiness { get; set; } = "UNKNOWN";
    public bool BwmAvailable { get; set; }
    public bool FpgaAvailable { get; set; }
    public Dictionary<string, string> Evidence { get; } = new(StringComparer.OrdinalIgnoreCase);
}
