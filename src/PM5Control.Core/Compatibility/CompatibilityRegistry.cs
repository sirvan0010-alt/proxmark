/*
 * PM5 Control Center
 * PURPOSE: Load evidence-backed hardware/firmware compatibility data.
 * RULE: Registry expectations never become DETECTED hardware facts.
 * SEE: AI_CONTEXT.md, docs/HARDWARE_COMPARISON.md, compatibility/*.json
 */

using System.Text.Json;

namespace PM5Control.Core.Compatibility;

public sealed record CompatibilityRegistry(
    int SchemaVersion,
    string Purpose,
    IReadOnlyList<CompatibilityEntry> Entries,
    IReadOnlyDictionary<string, CompatibilityFamily> Families,
    IReadOnlyList<string> ComparisonFields,
    IReadOnlyList<string> Notes,
    IReadOnlyList<string> SelectionPipeline);

public sealed record CompatibilityEntry(
    string? Id,
    string? Family,
    string? HardwareRevision,
    string? UsbVid,
    IReadOnlyList<string>? UsbPids,
    string? ArmFirmware,
    string? FpgaFirmware,
    string? BwmFirmware,
    string? Status,
    string? Evidence);

public sealed record CompatibilityFamily(
    string DisplayName,
    string Status,
    string HumanSummary,
    string FirmwareRule);

public static class CompatibilityRegistryLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static CompatibilityRegistry Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return LoadJson(File.ReadAllText(path));
    }

    public static CompatibilityRegistry LoadJson(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        return JsonSerializer.Deserialize<CompatibilityRegistry>(json, Options)
            ?? throw new InvalidDataException("Compatibility registry is empty or invalid.");
    }
}
