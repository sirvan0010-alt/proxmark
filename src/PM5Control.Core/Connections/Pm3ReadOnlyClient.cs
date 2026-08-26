using System.Diagnostics;
using PM5Control.Core.Protocols.Pm3;

namespace PM5Control.Core.Connections;

/// <summary>
/// Transport-independent PM3-NG read-only transaction engine.
/// USB/serial, BLE and future TCP transports all use the same framing,
/// response correlation and response-storm protection here.
/// </summary>
public sealed class Pm3ReadOnlyClient
{
    private const int DefaultTimeoutMs = 3000;
    private const int MaxUnmatchedResponses = 32;
    private readonly IProxmarkTransport _transport;
    private readonly int _timeoutMs;

    public Pm3ReadOnlyClient(IProxmarkTransport transport, int timeoutMs = DefaultTimeoutMs)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        if (timeoutMs <= 0) throw new ArgumentOutOfRangeException(nameof(timeoutMs));
        _timeoutMs = timeoutMs;
    }

    public async Task<Pm3NgExchange> ExecuteAsync(ushort command, CancellationToken cancellationToken = default)
    {
        if (!Pm3CommandCode.IsSafeReadOnlyProbe(command))
            throw new InvalidOperationException($"Command 0x{command:X4} is outside the read-only probe policy.");
        if (!_transport.IsConnected)
            throw new InvalidOperationException($"Transport '{_transport.TransportName}' is not connected.");

        var request = Pm3NgFrame.EncodeCommand(command);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_timeoutMs);
        var raw = await _transport.SendAsync(request, timeout.Token).ConfigureAwait(false);
        if (!Pm3NgFrame.TryDecodeResponse(raw, out var response) || response is null)
            throw new InvalidDataException($"Transport '{_transport.TransportName}' returned an invalid PM3-NG response for CMD 0x{command:X4}; RX={Convert.ToHexString(raw)}");

        if (response.Command != command)
            throw new InvalidDataException($"Transport '{_transport.TransportName}' returned CMD 0x{response.Command:X4} while waiting for CMD 0x{command:X4}.");

        return new Pm3NgExchange(request, response, Array.Empty<Pm3NgResponse>(), Array.Empty<Pm3NgResponse>());
    }

    public Task<Pm3NgExchange> VersionAsync(CancellationToken cancellationToken = default) => ExecuteAsync(Pm3CommandCode.Version, cancellationToken);
    public Task<Pm3NgExchange> CapabilitiesAsync(CancellationToken cancellationToken = default) => ExecuteAsync(Pm3CommandCode.Capabilities, cancellationToken);
    public Task<Pm3NgExchange> StatusAsync(CancellationToken cancellationToken = default) => ExecuteAsync(Pm3CommandCode.Status, cancellationToken);
    public Task<Pm3NgExchange> PingAsync(CancellationToken cancellationToken = default) => ExecuteAsync(Pm3CommandCode.Ping, cancellationToken);
}
