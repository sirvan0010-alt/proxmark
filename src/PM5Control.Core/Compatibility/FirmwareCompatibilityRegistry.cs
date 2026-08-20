/*
 * PM5 Control Center
 * PURPOSE: Load the separate firmware compatibility registry.
 * WHY: hardware-family metadata and firmware-package compatibility are related
 * but distinct datasets and must not be deserialized through one model.
 * RULE: registry entries are compatibility evidence, never detected device facts.
 * SEE: compatibility/firmware.json, docs/FIRMWARE_SELECTION.md
 */

using System.Text.Json;
using System.Text.Json.Serialization;

namespace PM5Control.Core.Compatibility;

public sealed record FirmwareCompatibilityRegistry(
    [property: JsonPropertyName("schema_version")] int SchemaVersion,
    string Purpose,
    IReadOnlyList<FirmwareCompatibilityEntry> Entries,
    [property: JsonPropertyName("selection_fields")] IReadOnlyList<string> SelectionFields,
    [property: JsonPropertyName("human_selection_policy")] IReadOnlyDictionary<string, string> HumanSelectionPolicy,
    IReadOnlyList<string> Notes);

public sealed record FirmwareCompatibilityEntry(
    string? Id,
    [property: JsonPropertyName("hardware_family")] string? HardwareFamily,
    [property: JsonPropertyName("hardware_revision")] string? HardwareRevision,
    [property: JsonPropertyName("firmware_component")] string? FirmwareComponent,
    [property: JsonPropertyName("firmware_version")] string? FirmwareVersion,
    [property: JsonPropertyName("source_repository")] string? SourceRepository,
    [property: JsonPropertyName("source_commit")] string? SourceCommit,
    [property: JsonPropertyName("source_date")] string? SourceDate,
    [property: JsonPropertyName("compatibility_status")] string? CompatibilityStatus,
    string? Notes);

public static class FirmwareCompatibilityRegistryLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static FirmwareCompatibilityRegistry Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return LoadJson(File.ReadAllText(path));
    }

    public static FirmwareCompatibilityRegistry LoadJson(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        return JsonSerializer.Deserialize<FirmwareCompatibilityRegistry>(json, Options)
            ?? throw new InvalidDataException("Firmware compatibility registry is empty or invalid.");
    }
}
