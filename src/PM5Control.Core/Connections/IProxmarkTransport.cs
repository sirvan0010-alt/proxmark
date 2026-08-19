/*
 * PM5 Control Center
 *
 * PURPOSE: Defines the minimal transport boundary used by protocol adapters.
 * WHY: USB, BLE and Wi-Fi/TCP must be interchangeable at the application level.
 * RULE: Do not put PM5 command knowledge into a transport implementation.
 * SEE: docs/ARCHITECTURE.md
 */

namespace PM5Control.Core.Connections;

public interface IProxmarkTransport : IAsyncDisposable
{
    string TransportName { get; }
    bool IsConnected { get; }

    Task ConnectAsync(CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);
    Task<byte[]> SendAsync(ReadOnlyMemory<byte> request, CancellationToken cancellationToken = default);
}
