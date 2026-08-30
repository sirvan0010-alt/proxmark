using PM5Control.Core.WirelessSecurityLab;

namespace PM5Control.Core.Tests;

public sealed class WirelessSecurityLabTests
{
    [Fact]
    public void DuplicateSsidWithStrongerPeerProducesFinding()
    {
        var now = DateTimeOffset.UtcNow;
        var observations = new[]
        {
            new WirelessAccessPointObservation("LabNet", "AA:BB:CC:00:00:01", 1, -72, "WPA2", now),
            new WirelessAccessPointObservation("LabNet", "AA:BB:CC:00:00:02", 6, -48, "WPA2", now)
        };

        var findings = RogueApAnalyzer.Analyze(observations);

        Assert.NotEmpty(findings);
        Assert.Contains(findings, x => x.Code == "WSL-ROGUE-SSID");
        Assert.Contains(findings, x => x.Severity >= WirelessFindingSeverity.Medium);
    }

    [Fact]
    public void SingleBssidDoesNotProduceRogueFinding()
    {
        var observations = new[]
        {
            new WirelessAccessPointObservation("LabNet", "AA:BB:CC:00:00:01", 1, -48, "WPA2", DateTimeOffset.UtcNow)
        };

        Assert.Empty(RogueApAnalyzer.Analyze(observations));
    }

    [Fact]
    public void DuplicateSsidIsHeuristicNotAutomaticCriticalFinding()
    {
        var now = DateTimeOffset.UtcNow;
        var observations = new[]
        {
            new WirelessAccessPointObservation("Mesh", "AA:BB:CC:00:00:01", 1, -50, "WPA3", now),
            new WirelessAccessPointObservation("Mesh", "AA:BB:CC:00:00:02", 6, -52, "WPA3", now)
        };

        var findings = RogueApAnalyzer.Analyze(observations);

        Assert.NotEmpty(findings);
        Assert.All(findings, x => Assert.NotEqual(WirelessFindingSeverity.Critical, x.Severity));
    }
}
