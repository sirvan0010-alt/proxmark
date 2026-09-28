using PM5Control.Core.Protocols.Pm3;

namespace PM5Control.Core.Connections;

/// <summary>
/// Generic PM3-NG command transport. Unlike IPm3ReadOnlyTransport this is
/// explicitly a control-capable boundary and may be used for firmware/BWM
/// management operations after the caller has applied its own safety policy.
/// </summary>
public interface IPm3CommandTransport
{
    bool IsConnected { get; }
    Task<Pm3NgExchange> SendCommandAsync(ushort command, ReadOnlyMemory<byte> payload = default, CancellationToken cancellationToken = default);
}
