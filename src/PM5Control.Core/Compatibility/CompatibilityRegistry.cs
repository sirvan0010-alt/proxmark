/*
 * PM5 Control Center
 * PURPOSE: Load evidence-backed hardware/firmware compatibility data.
 * RULE: Registry expectations never become DETECTED hardware facts.
 * SEE: AI_CONTEXT.md, docs/HARDWARE_COMPARISON.md, compatibility/*.json
 */

using System.Text.Json;
using System.Text.Json.Serialization;

namespace PM5Control.Core.Compatibility;

public sealed record CompatibilityRegistry(
    [property: JsonPropertyName("schema_version")] int SchemaVersion,
    string Purpose,
    IReadOnlyList<CompatibilityEntry> Entries,
    IReadOnlyDictionary<string, CompatibilityFamily> Families,
    [property: JsonPropertyName("comparison_fields")] IReadOnlyList<string> ComparisonFields,
    IReadOnlyList<string> Notes,
    [property: JsonPropertyName("selection_pipeline")] IReadOnlyList<string> SelectionPipeline);

public sealed record CompatibilityEntry(
    string? Id,
    string? Family,
    [property: JsonPropertyName("hardware_revision")] string? HardwareRevision,
    [property: JsonPropertyName("usb_vid")] string? UsbVid,
    [property: JsonPropertyName("usb_pids")] IReadOnlyList<string>? UsbPids,
    [property: JsonPropertyName("arm_firmware")] string? ArmFirmware,
    [property: JsonPropertyName("fpga_firmware")] string? FpgaFirmware,
    [property: JsonPropertyName("bwm_firmware")] string? BwmFirmware,
    string? Status,
    string? Evidence);

public sealed record CompatibilityFamily(
    [property: JsonPropertyName("display_name")] string DisplayName,
    string Status,
    [property: JsonPropertyName("human_summary")] string HumanSummary,
    [property: JsonPropertyName("firmware_rule")] string FirmwareRule);

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
