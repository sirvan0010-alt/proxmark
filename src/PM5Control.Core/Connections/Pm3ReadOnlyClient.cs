using PM5Control.Core.Protocols.Pm3;

namespace PM5Control.Core.Connections;

/// <summary>
/// Transport-independent PM3-NG read-only transaction facade.
///
/// Response framing/correlation belongs to IPm3ReadOnlyTransport. The client
/// deliberately does not attempt to decode the first byte returned by a
/// generic transport or assume that one SendAsync call maps to one frame.
///
/// The transport implementation is responsible for consuming unsolicited
/// debug/broadcast frames, correlating the expected command and enforcing its
/// unmatched-response limit. This keeps the read-only client safe to use with
/// the existing PM3 serial implementation without inventing a second framing
/// protocol.
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
    ///
    /// Serialization is intentional: a physical PM3 command channel is a
    /// single ordered stream and concurrent requests can otherwise interleave.
    /// The timeout token is local to this operation, so cancellation by the
    /// caller remains distinguishable from a transport-level timeout.
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
                // IPm3ReadOnlyTransport already owns the complete framed
                // exchange, including debug/unmatched response handling.
                return await _transport.SendReadOnlyAsync(command, timeout.Token)
                    .ConfigureAwait(false);
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
