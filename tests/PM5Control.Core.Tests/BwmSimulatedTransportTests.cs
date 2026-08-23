using PM5Control.Core.Diagnostics;
using PM5Control.Core.Protocols.Bwm;
using PM5Control.Simulator;

namespace PM5Control.Core.Tests;

public sealed class BwmSimulatedTransportTests
{
    [Fact]
    public async Task ReadOnlyAdapter_CanRunEndToEndAgainstSimulatedTransport()
    {
        await using var transport = new BwmSimulatedTransport();
        await transport.ConnectAsync();
        var adapter = new BwmReadOnlyAdapter(transport);

        var version = await adapter.GetVersionInfoAsync();
        var heap = await adapter.GetSysFreeHeapAsync();

        Assert.Equal("SIM-BWM-0.1", version.Value);
        Assert.Equal(65536L, heap.Value);
        Assert.Equal(DiagnosticConfidence.Medium, version.Confidence);
        Assert.Equal(DiagnosticConfidence.Medium, heap.Confidence);
    }

    [Fact]
    public async Task SimulatedTransport_RejectsUseBeforeConnect()
    {
        await using var transport = new BwmSimulatedTransport();
        var request = BwmFrameCodec.EncodeRequest((ushort)BwmCommandCode.GetVersionInfo, ReadOnlySpan<byte>.Empty);

        await Assert.ThrowsAsync<InvalidOperationException>(() => transport.SendAsync(request));
    }

    [Fact]
    public async Task SimulatedTransport_ProducesValidFramedResponse()
    {
        await using var transport = new BwmSimulatedTransport();
        await transport.ConnectAsync();
        var request = BwmFrameCodec.EncodeRequest((ushort)BwmCommandCode.GetDeviceModel, ReadOnlySpan<byte>.Empty);

        var response = await transport.SendAsync(request);

        Assert.True(BwmFrameCodec.TryDecode(response, out var frame));
        Assert.NotNull(frame);
        Assert.Equal(BwmFrameKind.Response, frame!.Kind);
        Assert.Equal((ushort)BwmCommandCode.GetDeviceModel, frame.CommandId);
    }
}
