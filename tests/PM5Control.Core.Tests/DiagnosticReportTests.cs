using PM5Control.Core.Diagnostics;

namespace PM5Control.Core.Tests;

public sealed class DiagnosticReportTests
{
    [Fact]
    public void ToJson_PreservesEvidenceAndConfidence()
    {
        var evidence = new DiagnosticEvidence(
            DateTimeOffset.Parse("2026-08-20T12:00:00Z"),
            "bwm.version",
            "USB",
            "request",
            "response",
            "1.2.3",
            12.5,
            0,
            "official firmware source + host observation pending",
            DiagnosticConfidence.Medium,
            "test-sha");

        var report = new DiagnosticReport(
            evidence.Timestamp,
            "0.1.0",
            "test-sha",
            new Dictionary<string, object?>
            {
                ["bwmFirmware"] = new DiagnosticValue<string>(
                    "SIMULATED",
                    DiagnosticSourceState.Unknown,
                    DiagnosticConfidence.Medium,
                    "test",
                    evidence.Timestamp)
            },
            new[] { evidence });

        var json = DiagnosticReportExporter.ToJson(report);

        Assert.Contains("bwm.version", json);
        Assert.Contains("Medium", json);
        Assert.Contains("test-sha", json);
        Assert.Contains("12.5", json);
    }
}
