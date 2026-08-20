using PM5Control.Simulator;

namespace PM5Control.Core.Tests;

public sealed class SimulatorBehaviorTests
{
    [Fact]
    public void Same_command_and_same_state_are_deterministic()
    {
        var device = new PM5SimulatedDevice();

        var first = device.Execute("GET_DEVICE_INFO");
        var second = device.Execute("GET_DEVICE_INFO");

        Assert.Equal(first, second);
    }

    [Fact]
    public void Unknown_hardware_specific_information_stays_unknown()
    {
        var device = new PM5SimulatedDevice();

        var result = device.Execute("GET_FIRMWARE_INFO");

        Assert.Equal("UNKNOWN", result.Status);
        Assert.Null(result.Value);
        Assert.Contains("unknown", result.Evidence, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Unsupported_command_returns_explicit_error()
    {
        var device = new PM5SimulatedDevice();

        var result = device.Execute("FLASH_FIRMWARE");

        Assert.Equal("ERROR", result.Status);
        Assert.Equal("UNSUPPORTED_SIMULATED_COMMAND", result.Evidence);
    }

    [Fact]
    public void Bwm_state_controls_bwm_result()
    {
        var device = new PM5SimulatedDevice();

        device.State.BwmAvailable = false;
        var unavailable = device.Execute("GET_BWM_STATUS");

        device.State.BwmAvailable = true;
        var available = device.Execute("GET_BWM_STATUS");

        Assert.Equal("UNKNOWN", unavailable.Status);
        Assert.Equal("OK", available.Status);
        Assert.Equal("BWM_AVAILABLE", available.Value);
    }

    [Fact]
    public void Simulator_never_claims_hardware_verification()
    {
        var device = new PM5SimulatedDevice();
        device.State.BwmAvailable = true;

        var result = device.Execute("GET_BWM_STATUS");

        Assert.NotEqual("HARDWARE_VERIFIED", result.Evidence);
        Assert.Equal("SIMULATED", result.Evidence);
    }
}
