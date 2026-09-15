using PM5Control.Core.Connections;
using PM5Control.Core.Protocols.Pm3;

namespace PM5Control.Core.Tests;

public sealed class Pm3ReadOnlyClientCancellationTests
{
    [Fact]
    public async Task CancellationRequestsAbortBeforePropagating()
    {
        var transport = new CancelAwareTransport();
        var client = new Pm3ReadOnlyClient(transport, timeoutMs: 5000);
        using var cancellation = new CancellationTokenSource();

        var operation = client.PingAsync(cancellation.Token);
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);
        Assert.Equal(1, transport.AbortCount);
    }

    private sealed class CancelAwareTransport : IPm3ReadOnlyTransport, IProxmarkAbortTransport
    {
        public bool IsConnected => true;
        public int AbortCount { get; private set; }

        public async Task<Pm3NgExchange> SendReadOnlyAsync(ushort command, CancellationToken cancellationToken = default)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            throw new InvalidOperationException("Cancellation should have interrupted the pending transport operation.");
        }

        public Task AbortCurrentOperationAsync(CancellationToken cancellationToken = default)
        {
            AbortCount++;
            return Task.CompletedTask;
        }
    }
}
