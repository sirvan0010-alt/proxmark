namespace PM5Control.Core.Tests;

public sealed class BwmTransportCapabilityTests
{
    [Fact]
    public void UnknownDoesNotAuthorizeCommands()
    {
        var capability = PM5Control.Core.Protocols.Bwm.BwmTransportCapability.Unknown();

        Assert.False(capability.CanSendReadOnlyCommands);
        Assert.Equal(PM5Control.Core.Protocols.Bwm.BwmTransportPath.Unknown, capability.Path);
    }

    [Fact]
    public void Pm5ArmBridgeIsExplicitlyBlockedUntilDriverExists()
    {
        var capability = PM5Control.Core.Protocols.Bwm.BwmTransportCapability.Pm5ArmBridgeUnavailable();

        Assert.False(capability.CanSendReadOnlyCommands);
        Assert.Equal(PM5Control.Core.Protocols.Bwm.BwmTransportPath.Pm5ArmBridge, capability.Path);
        Assert.Contains("ARM", capability.Evidence, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("TODO", capability.Evidence, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DirectBwmUartIsTheOnlyCurrentPathThatAuthorizesReadOnlyCommands()
    {
        var capability = PM5Control.Core.Protocols.Bwm.BwmTransportCapability.DirectBwmUart();

        Assert.True(capability.CanSendReadOnlyCommands);
        Assert.Equal(PM5Control.Core.Protocols.Bwm.BwmTransportPath.DirectBwmUart, capability.Path);
    }
}
