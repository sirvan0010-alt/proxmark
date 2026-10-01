using PM5Control.Core.Devices;

namespace PM5Control.Core.Tests;

public sealed class Pm5TransportCapabilitiesTests
{
    [Fact]
    public void Schema13SeparatesBwmAndCepCapabilities()
    {
        var caps = Pm5TransportCapabilities.FromCapabilities(
            schemaVersion: 13,
            viaUsb: true,
            compiledWithBwm: true,
            compiledWithCep: false,
            bigBufferSize: 65536,
            maxCommandDataSize: 4064);

        Assert.Equal(Pm5CapabilityState.Reported, caps.Usb);
        Assert.Equal(Pm5CapabilityState.Compiled, caps.Bwm);
        Assert.Equal(Pm5CapabilityState.NotCompiled, caps.Cep);
        Assert.Equal(Pm5CapabilityState.Compiled, caps.BleForwarding);
        Assert.Equal(Pm5CapabilityState.Compiled, caps.WifiForwarding);
        Assert.Equal((ushort)4064, caps.MaxCommandDataSize);
    }

    [Fact]
    public void OlderSchemaDoesNotInventBwmOrCep()
    {
        var caps = Pm5TransportCapabilities.FromCapabilities(
            schemaVersion: 11,
            viaUsb: true,
            compiledWithBwm: false,
            compiledWithCep: false,
            bigBufferSize: 1,
            maxCommandDataSize: 512);

        Assert.Equal(Pm5CapabilityState.Unknown, caps.Bwm);
        Assert.Equal(Pm5CapabilityState.Unknown, caps.Cep);
        Assert.Equal(Pm5CapabilityState.Unknown, caps.BleForwarding);
        Assert.Equal(Pm5CapabilityState.Unknown, caps.WifiForwarding);
    }

    [Fact]
    public void Schema12CanReportBwmWithoutCep()
    {
        var caps = Pm5TransportCapabilities.FromCapabilities(
            schemaVersion: 12,
            viaUsb: true,
            compiledWithBwm: true,
            compiledWithCep: false,
            bigBufferSize: 1,
            maxCommandDataSize: 512);

        Assert.Equal(Pm5CapabilityState.Compiled, caps.Bwm);
        Assert.Equal(Pm5CapabilityState.Unknown, caps.Cep);
    }

    [Fact]
    public void Schema13CanReportBwmNotCompiled()
    {
        var caps = Pm5TransportCapabilities.FromCapabilities(
            schemaVersion: 13,
            viaUsb: true,
            compiledWithBwm: false,
            compiledWithCep: false,
            bigBufferSize: 1,
            maxCommandDataSize: 512);

        Assert.Equal(Pm5CapabilityState.NotCompiled, caps.Bwm);
        Assert.Equal(Pm5CapabilityState.NotCompiled, caps.Cep);
        Assert.Equal(Pm5CapabilityState.NotCompiled, caps.BleForwarding);
        Assert.Equal(Pm5CapabilityState.NotCompiled, caps.WifiForwarding);
    }
}
