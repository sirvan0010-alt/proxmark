using PM5Control.Core.Protocols.Bwm;
using PM5Control.Simulator;

namespace PM5Control.Core.Tests;

public sealed class BwmSimulatedFaultTests
{
    [Theory]
    [InlineData(SimulationFault.MalformedResponse)]
    [InlineData(SimulationFault.WrongCommandId)]
    [InlineData(SimulationFault.BroadcastInsteadOfResponse)]
    [InlineData(SimulationFault.UnsupportedCommand)]
    public async Task ReadOnlyAdapter_RejectsInvalidOrUnsupportedResponses(SimulationFault fault)
    {
        await using var transport = new BwmSimulatedTransport { Fault = fault };
        await transport.ConnectAsync();
        var adapter = new BwmReadOnlyAdapter(transport);

        var result = await adapter.GetVersionInfoAsync();

        Assert.Equal(DiagnosticSourceState.Unknown, result.SourceState);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task ReadOnlyAdapter_PropagatesCancellation()
    {
        await using var transport = new BwmSimulatedTransport();
        await transport.ConnectAsync();
        var adapter = new BwmReadOnlyAdapter(transport);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => adapter.GetVersionInfoAsync(cts.Token));
    }

    [Fact]
    public async Task ReadOnlyAdapter_HandlesSimulatedTimeoutAsUnknown()
    {
        await using var transport = new BwmSimulatedTransport { Fault = SimulationFault.Timeout };
        await transport.ConnectAsync();
        var adapter = new BwmReadOnlyAdapter(transport);

        var result = await adapter.GetVersionInfoAsync();

        Assert.Equal(DiagnosticSourceState.Unknown, result.SourceState);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task ReadOnlyAdapter_HandlesSimulatedDisconnectAsUnknown()
    {
        await using var transport = new BwmSimulatedTransport { Fault = SimulationFault.DisconnectBeforeSend };
        await transport.ConnectAsync();
        var adapter = new BwmReadOnlyAdapter(transport);

        var result = await adapter.GetVersionInfoAsync();

        Assert.Equal(DiagnosticSourceState.Unknown, result.SourceState);
        Assert.Null(result.Value);
        Assert.False(transport.IsConnected);
    }
}
