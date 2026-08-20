// PM5 Control Center — simulated BWM transport
// PURPOSE: end-to-end offline transport double for the real Core BWM adapter.
// STATUS: SIMULATED_ONLY. Payload values and response shapes are fixtures,
// not evidence of real PM5/BWM payload layout.
// SAFETY: accepts framed requests and returns only simulated responses; it
// never opens USB/BLE/Wi-Fi and never changes physical device state.

using System.Text;
using PM5Control.Core.Connections;
using PM5Control.Core.Protocols.Bwm;

namespace PM5Control.Simulator;

public sealed class BwmSimulatedTransport : IProxmarkTransport
{
    public string TransportName => "simulated-bwm";
    public bool IsConnected { get; private set; }

    public event Action<ReadOnlyMemory<byte>>? DataReceived;

    public string Version { get; set; } = "SIM-BWM-0.1";
    public string Model { get; set; } = "SIMULATED-PM5-BWM";
    public string CompileDatetime { get; set; } = "SIMULATED-DATE";
    public uint FreeHeap { get; set; } = 65536;
    public byte[] BaseMac { get; set; } = { 0x02, 0x00, 0x00, 0x00, 0x00, 0x01 };

    public Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IsConnected = true;
        return Task.CompletedTask;
    }

    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IsConnected = false;
        return Task.CompletedTask;
    }

    public Task<byte[]> SendAsync(ReadOnlyMemory<byte> request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!IsConnected)
            throw new InvalidOperationException("Simulated BWM transport is not connected.");

        if (!BwmFrameCodec.TryDecode(request.Span, out var frame) || frame is null || frame.Kind != BwmFrameKind.Request)
            return Task.FromResult(Array.Empty<byte>());

        var command = (BwmCommandCode)frame.CommandId;
        byte[] payload = command switch
        {
            BwmCommandCode.GetVersionInfo => Encoding.UTF8.GetBytes(Version + "\0"),
            BwmCommandCode.GetDeviceModel => Encoding.UTF8.GetBytes(Model + "\0"),
            BwmCommandCode.GetAppCompileDatetime => Encoding.UTF8.GetBytes(CompileDatetime + "\0"),
            BwmCommandCode.GetSysFreeHeap => BitConverter.GetBytes(FreeHeap),
            BwmCommandCode.GetSysBaseMacAddr when BaseMac.Length == 6 => BaseMac.ToArray(),
            _ => Array.Empty<byte>()
        };

        var response = BwmFrameCodec.EncodeResponse(frame.CommandId, payload);
        return Task.FromResult(response);
    }

    public ValueTask DisposeAsync()
    {
        IsConnected = false;
        return ValueTask.CompletedTask;
    }
}
