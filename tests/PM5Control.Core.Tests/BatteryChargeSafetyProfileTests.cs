using PM5Control.Core.Bwm;

namespace PM5Control.Core.Tests;

public sealed class BatteryChargeSafetyProfileTests
{
    [Fact]
    public void PreferredTargetIs4100mV()
    {
        Assert.Equal(4100, BatteryChargeSafetyProfile.PreferredChargeVoltageMv);
    }

    [Theory]
    [InlineData(3600)]
    [InlineData(4000)]
    [InlineData(4100)]
    public void AutomaticTargetAcceptsValuesUpToSafetyCeiling(int millivolts)
    {
        Assert.True(BatteryChargeSafetyProfile.IsAllowedAutomaticTarget(millivolts));
    }

    [Theory]
    [InlineData(3500)]
    [InlineData(4101)]
    [InlineData(4200)]
    public void AutomaticTargetRejectsValuesOutsidePolicy(int millivolts)
    {
        Assert.False(BatteryChargeSafetyProfile.IsAllowedAutomaticTarget(millivolts));
    }

    [Fact]
    public void ReadbackControlsHardwareEvidenceState()
    {
        Assert.Equal("EXPECTED", BatteryChargeSafetyProfile.GetEvidenceState(false));
        Assert.Equal("HARDWARE_VERIFIED", BatteryChargeSafetyProfile.GetEvidenceState(true));
    }
}
