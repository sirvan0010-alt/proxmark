using PM5Control.Core.Protocols.Pm3;

namespace PM5Control.Core.Connections;

/// <summary>
/// Transport-independent PM3-NG read-only transaction facade.
///
/// Response framing/correlation belongs to IPm3ReadOnlyTransport. The client
/// deliberately does not attempt to decode the first byte returned by a
/// generic transport or assume that one SendAsync call maps to one frame.
/// </summary>
public sealed class Pm3ReadOnlyClient
{
    private const int DefaultTimeoutMs = 3000;
    private readonly IPm3ReadOnlyTransport _transport;
    private readonly SemaphoreSlim _transactionGate = new(1, 1);
    private readonly int _timeoutMs;

    public Pm3ReadOnlyClient(IPm3ReadOnlyTransport transport, int timeoutMs = DefaultTimeoutMs)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        if (timeoutMs <= 0)
            throw new ArgumentOutOfRangeException(nameof(timeoutMs));
        _timeoutMs = timeoutMs;
    }

    /// <summary>
    /// Executes one allow-listed PM3-NG read-only command.
    /// If the caller cancels while the device is busy, an abort-capable
    /// transport is given an opportunity to deliver CMD_BREAK_LOOP before the
    /// cancellation is propagated. This mirrors the upstream PM5/BWM contract.
    /// </summary>
    public async Task<Pm3NgExchange> ExecuteAsync(
        ushort command,
        CancellationToken cancellationToken = default)
    {
        if (!Pm3CommandCode.IsSafeReadOnlyProbe(command))
            throw new InvalidOperationException(
                $"Command 0x{command:X4} is outside the read-only probe policy.");

        if (!_transport.IsConnected)
            throw new InvalidOperationException("PM3 read-only transport is not connected.");

        await _transactionGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(_timeoutMs);

            try
            {
                return await _transport.SendReadOnlyAsync(command, timeout.Token)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                if (_transport is IProxmarkAbortTransport abortTransport)
                {
                    try
                    {
                        // Do not reuse the cancelled token: the abort itself
                        // must have a chance to reach the device.
                        using var abortTimeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(750));
                        await abortTransport.AbortCurrentOperationAsync(abortTimeout.Token).ConfigureAwait(false);
                    }
                    catch
                    {
                        // Cancellation remains authoritative. The transport
                        // can report abort failure through its own diagnostics.
                    }
                }

                throw;
            }
            catch (OperationCanceledException) when (
                !cancellationToken.IsCancellationRequested && timeout.IsCancellationRequested)
            {
                throw new TimeoutException(
                    $"PM3 read-only transaction timed out waiting for CMD 0x{command:X4} after {_timeoutMs} ms.");
            }
        }
        finally
        {
            _transactionGate.Release();
        }
    }

    public Task<Pm3NgExchange> VersionAsync(CancellationToken cancellationToken = default) =>
        ExecuteAsync(Pm3CommandCode.Version, cancellationToken);

    public Task<Pm3NgExchange> CapabilitiesAsync(CancellationToken cancellationToken = default) =>
        ExecuteAsync(Pm3CommandCode.Capabilities, cancellationToken);

    public Task<Pm3NgExchange> StatusAsync(CancellationToken cancellationToken = default) =>
        ExecuteAsync(Pm3CommandCode.Status, cancellationToken);

    public Task<Pm3NgExchange> PingAsync(CancellationToken cancellationToken = default) =>
        ExecuteAsync(Pm3CommandCode.Ping, cancellationToken);
}
