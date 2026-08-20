using PM5Control.Core.Compatibility;

namespace PM5Control.Core.Tests;

public sealed class FirmwareCompatibilityRegistryTests
{
    [Fact]
    public void LoadJson_MapsFirmwareRegistryFields()
    {
        const string json = """
        {
          "schema_version": 2,
          "purpose": "test",
          "entries": [
            {
              "id": "pm5-bwm-1",
              "hardware_family": "PM5",
              "hardware_revision": "rev-test",
              "firmware_component": "BWM",
              "firmware_version": "1.2.3",
              "source_repository": "example/repo",
              "source_commit": "abc123",
              "source_date": "2026-08-20",
              "compatibility_status": "SUPPORTED",
              "notes": "test entry"
            }
          ],
          "selection_fields": ["hardware_family", "firmware_component"],
          "human_selection_policy": {"step_1": "identify hardware"},
          "notes": ["test"]
        }
        """;

        var registry = FirmwareCompatibilityRegistryLoader.LoadJson(json);

        Assert.Equal(2, registry.SchemaVersion);
        var entry = Assert.Single(registry.Entries);
        Assert.Equal("PM5", entry.HardwareFamily);
        Assert.Equal("BWM", entry.FirmwareComponent);
        Assert.Equal("1.2.3", entry.FirmwareVersion);
        Assert.Equal("SUPPORTED", entry.CompatibilityStatus);
        Assert.Equal("identify hardware", registry.HumanSelectionPolicy["step_1"]);
    }

    [Fact]
    public void LoadJson_RejectsBlankInput()
    {
        Assert.Throws<ArgumentException>(() => FirmwareCompatibilityRegistryLoader.LoadJson("   "));
    }
}
