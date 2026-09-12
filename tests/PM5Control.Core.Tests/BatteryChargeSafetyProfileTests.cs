using PM5Control.Core.Bwm;

namespace PM5Control.Core.Tests;

public sealed class BatteryChargeSafetyProfileTests
{
    [Fact]
    public void AutomaticChargerConfigurationIsDisabledUntilHardwareEvidenceExists()
    {
        Assert.False(BatteryChargeSafetyProfile.AutomaticConfigurationEnabled);
        Assert.False(BatteryChargeSafetyProfile.IsAllowedAutomaticTarget(4100));
        Assert.False(BatteryChargeSafetyProfile.IsAllowedAutomaticTarget(4200));
    }

    [Fact]
    public void RegisterReadbackIsRequiredForHardwareVerifiedState()
    {
        Assert.Equal("UNKNOWN", BatteryChargeSafetyProfile.GetEvidenceState(false));
        Assert.Equal("HARDWARE_VERIFIED", BatteryChargeSafetyProfile.GetEvidenceState(true));
    }
}
