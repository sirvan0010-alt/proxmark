namespace PM5Control.Core.WirelessSecurityLab;

/// <summary>
/// Passive/defensive analyzer for detecting suspicious duplicate SSIDs and
/// possible Evil-Twin / rogue-AP conditions from observations supplied by an
/// OS-level Wi-Fi scanner. It does not transmit frames, deauthenticate clients,
/// impersonate APs, or collect credentials.
/// </summary>
public static class RogueApAnalyzer
{
    public static IReadOnlyList<WirelessSecurityFinding> Analyze(
        IEnumerable<WirelessAccessPointObservation> observations)
    {
        var groups = observations
            .Where(x => !string.IsNullOrWhiteSpace(x.Ssid))
            .GroupBy(x => NormalizeSsid(x.Ssid), StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Select(x => NormalizeBssid(x.Bssid)).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1)
            .ToArray();

        var findings = new List<WirelessSecurityFinding>();

        foreach (var group in groups)
        {
            var aps = group
                .GroupBy(x => NormalizeBssid(x.Bssid), StringComparer.OrdinalIgnoreCase)
                .Select(g => g.OrderByDescending(x => x.ObservedAt).First())
                .ToArray();

            foreach (var candidate in aps)
            {
                var peers = aps.Where(x => !string.Equals(
                    NormalizeBssid(x.Bssid), NormalizeBssid(candidate.Bssid),
                    StringComparison.OrdinalIgnoreCase)).ToArray();

                var sameSecurity = peers.Any(x => string.Equals(
                    NormalizeSecurity(x.Security), NormalizeSecurity(candidate.Security),
                    StringComparison.OrdinalIgnoreCase));
                var stronger = peers.Any(x => x.RssiDbm > candidate.RssiDbm + 8);
                var channelMismatch = peers.Any(x => x.Channel != candidate.Channel);

                var score = 0;
                if (sameSecurity) score += 1;
                if (stronger) score += 2;
                if (channelMismatch) score += 1;

                var severity = score >= 3
                    ? WirelessFindingSeverity.High
                    : score >= 2
                        ? WirelessFindingSeverity.Medium
                        : WirelessFindingSeverity.Low;

                findings.Add(new WirelessSecurityFinding(
                    severity,
                    "WSL-ROGUE-SSID",
                    "Duplicate SSID requires verification",
                    $"SSID '{candidate.Ssid}' is advertised by multiple BSSIDs. A stronger or otherwise inconsistent AP can indicate a rogue AP or Evil-Twin condition, but SSID duplication alone is not proof of an attack.",
                    new[]
                    {
                        $"Candidate BSSID: {candidate.Bssid}",
                        $"Candidate channel: {candidate.Channel}",
                        $"Candidate RSSI: {candidate.RssiDbm} dBm",
                        $"Candidate security: {candidate.Security}",
                        $"Other BSSIDs for SSID: {string.Join(", ", peers.Select(x => x.Bssid))}",
                        $"Same security as peer: {sameSecurity}",
                        $"Peer stronger by >8 dB: {stronger}",
                        $"Different channel present: {channelMismatch}"
                    }));
            }
        }

        return findings
            .OrderByDescending(x => x.Severity)
            .ThenBy(x => x.Code)
            .ToArray();
    }

    private static string NormalizeSsid(string value) => value.Trim();
    private static string NormalizeBssid(string value) => value.Trim().ToUpperInvariant();
    private static string NormalizeSecurity(string value) => value.Trim().ToUpperInvariant();
}
