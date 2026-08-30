namespace PM5Control.Core.WirelessSecurityLab;

/// <summary>
/// Passive observation of an 802.11 access point. This model deliberately contains
/// no packet-injection or credential-capture operations.
/// </summary>
public sealed record WirelessAccessPointObservation(
    string Ssid,
    string Bssid,
    int Channel,
    int RssiDbm,
    string Security,
    DateTimeOffset ObservedAt,
    string Source = "unknown");

public enum WirelessFindingSeverity
{
    Info,
    Low,
    Medium,
    High,
    Critical
}

public sealed record WirelessSecurityFinding(
    WirelessFindingSeverity Severity,
    string Code,
    string Title,
    string Explanation,
    IReadOnlyList<string> Evidence);
