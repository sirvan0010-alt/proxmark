using PM5Control.Core.Protocols.Pm3;

namespace PM5Control.Core.Connections;

/// <summary>
/// Shared transport contract for the PM3-NG read-only probe. USB/serial, BLE
/// and future Wi-Fi transports all carry the same framed command stream.
/// </summary>
public interface IPm3ReadOnlyTransport
{
    bool IsConnected { get; }
    Task<Pm3NgExchange> SendReadOnlyAsync(ushort command, CancellationToken cancellationToken = default);
}
