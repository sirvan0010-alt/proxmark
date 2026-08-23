using PM5Control.Core.Diagnostics;

namespace PM5Control.Core.Protocols.Bwm;

public enum BwmHardwareFamily
{
    Unknown,
    Pm5,
    Pm3,
    IcemanPm3Variant,
    RrgPm3Variant,
}

public sealed record BwmIdentityAssessment(
    DiagnosticValue<string> Family,
    DiagnosticValue<string> HumanSummary,
    DiagnosticValue<string> FirmwareRecommendation);

/// <summary>
/// Separates protocol evidence from physical hardware identity.
/// A BWM response is never sufficient by itself to label hardware as PM5.
/// Exact USB/device evidence must be supplied before a firmware branch is
/// recommended. This deliberately prevents PM3/Iceman/RRG assumptions.
/// </summary>
public static class BwmIdentityInspector
{
    public static BwmIdentityAssessment Assess(
        ushort? modelId,
        string? bwmFirmware,
        string? usbVidPid,
        string? armFirmware,
        string? fpgaFirmware)
    {
        var hasUsbEvidence = !string.IsNullOrWhiteSpace(usbVidPid);
        var hasArmEvidence = !string.IsNullOrWhiteSpace(armFirmware);
        var hasFpgaEvidence = !string.IsNullOrWhiteSpace(fpgaFirmware);
        var hasBwmEvidence = !string.IsNullOrWhiteSpace(bwmFirmware);

        if (!hasUsbEvidence && !hasArmEvidence && !hasFpgaEvidence)
        {
            var summary = hasBwmEvidence || modelId.HasValue
                ? "BWM protocol endpoint detected, but this is not sufficient to distinguish PM5 hardware from PM3/Iceman/RRG variants. USB and subsystem firmware evidence is required."
                : "No hardware-family evidence is available. Do not classify the device as PM5, PM3, Iceman or RRG.";

            return UnknownAssessment(summary);
        }

        // No PM5 VID/PID or subsystem fingerprint is asserted here until the
        // repository has an evidence-backed entry for the physical hardware.
        // Supplying identifiers alone is therefore not allowed to create a
        // false positive based on a guessed vendor/product mapping.
        return UnknownAssessment(
            "Hardware evidence is present, but no evidence-backed registry entry matches it yet. Keep the family UNKNOWN and do not recommend a firmware branch until the exact device/revision is registered.");
    }

    private static BwmIdentityAssessment UnknownAssessment(string summary) =>
        new(
            new DiagnosticValue<string>(
                "UNKNOWN",
                DiagnosticSourceState.Unknown,
                DiagnosticConfidence.Unknown,
                "Physical PM5/PM3/Iceman/RRG family is not established by the current evidence registry.",
                DateTimeOffset.UtcNow),
            new DiagnosticValue<string>(
                summary,
                DiagnosticSourceState.Unknown,
                DiagnosticConfidence.Unknown,
                "Human-readable safety boundary: protocol compatibility is not hardware identity.",
                DateTimeOffset.UtcNow),
            new DiagnosticValue<string>(
                "DO_NOT_RECOMMEND_FIRMWARE",
                DiagnosticSourceState.Unknown,
                DiagnosticConfidence.Unknown,
                "Identify exact hardware family/revision first; never recommend PM3/Iceman/RRG firmware for PM5 by repository similarity.",
                DateTimeOffset.UtcNow));
}
