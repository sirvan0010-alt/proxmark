using PM5Control.Core.Compatibility;

namespace PM5Control.Core.Tests;

public sealed class CompatibilityRegistryTests
{
    [Fact]
    public void LoadJson_PreservesFamilyRulesAndEmptyEntries()
    {
        const string json = """
        {
          "schema_version": 2,
          "purpose": "test",
          "entries": [],
          "families": {
            "PM5": {
              "display_name": "Proxmark5",
              "status": "HARDWARE_ID_PENDING",
              "human_summary": "PM5 family",
              "firmware_rule": "Identify exact hardware first."
            }
          },
          "comparison_fields": ["family"],
          "notes": ["unknown remains unknown"],
          "selection_pipeline": ["detect", "compare", "explain"]
        }
        """;

        var registry = CompatibilityRegistryLoader.LoadJson(json);

        Assert.Equal(2, registry.SchemaVersion);
        Assert.Empty(registry.Entries);
        Assert.Equal("Proxmark5", registry.Families["PM5"].DisplayName);
        Assert.Equal("HARDWARE_ID_PENDING", registry.Families["PM5"].Status);
        Assert.Contains("exact hardware", registry.Families["PM5"].FirmwareRule);
    }
}
