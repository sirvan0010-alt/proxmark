/*
 * PM5 Control Center
 *
 * PURPOSE: Defines the minimal transport boundary used by protocol adapters.
 * WHY: USB, BLE and Wi-Fi/TCP must be interchangeable at the application level.
 * RULE: Do not put PM5 command knowledge into a transport implementation.
 * RULE: A transport may deliver unsolicited data; protocol layers must not
 *       assume every incoming packet is a direct response to SendAsync.
 * SEE: docs/ARCHITECTURE.md
 */

namespace PM5Control.Core.Connections;

public interface IProxmarkTransport : IAsyncDisposable
{
    string TransportName { get; }
    bool IsConnected { get; }

    /// <summary>
    /// Raised when raw bytes arrive without being synchronously returned by
    /// the request operation. This is required for BWM broadcasts and other
    /// asynchronous device events.
    /// </summary>
    event Action<ReadOnlyMemory<byte>>? DataReceived;

    Task ConnectAsync(CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);
    Task<byte[]> SendAsync(ReadOnlyMemory<byte> request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Optional capability for transports that can deliver the PM3 NG abort
/// command independently of the currently awaited response.
///
/// This is deliberately separate from IProxmarkTransport: BWM control framing
/// and transparent PM3 forwarding are distinct protocols and must not be
/// conflated until their exact wire path is verified for a transport.
/// </summary>
public interface IProxmarkAbortTransport
{
    Task AbortCurrentOperationAsync(CancellationToken cancellationToken = default);
}
