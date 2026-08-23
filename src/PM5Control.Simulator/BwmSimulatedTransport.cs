// PM5 Control Center — simulated BWM transport
// PURPOSE: end-to-end offline transport double for the real Core BWM adapter.
// STATUS: SIMULATED_ONLY. Payload values are deterministic fixtures derived
// from the source-documented response shapes; they are not hardware evidence.
// SAFETY: accepts framed requests and returns only simulated responses; it
// never opens USB/BLE/Wi-Fi and never changes physical device state.

using System.Buffers.Binary;
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
    public ushort ModelId { get; set; } = 0xDA10;
    public string CompileDatetime { get; set; } = "SIMULATED-DATE";
    public string TimeZone { get; set; } = "UTC";
    public uint FreeHeap { get; set; } = 65536;
    public uint SysTimestamp { get; set; } = 0;
    public uint UartBaudRate { get; set; } = 115200;
    public uint UartMaxBaudRate { get; set; } = 2000000;
    public byte[] BaseMac { get; set; } = new byte[] { 0x02, 0x00, 0x00, 0x00, 0x00, 0x01 };
    public byte[] NvsStats { get; set; } = new byte[20];
    public bool LogUartForwardEnable { get; set; }
    public byte LogLevel { get; set; }
    public bool Ready { get; set; } = true;

    public SimulationFault Fault { get; set; }

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

        if (Fault == SimulationFault.DisconnectBeforeSend)
        {
            IsConnected = false;
            throw new InvalidOperationException("Simulated connection loss before send.");
        }

        if (Fault == SimulationFault.Timeout)
            throw new TimeoutException("Simulated BWM timeout.");

        if (!BwmFrameCodec.TryDecode(request.Span, out var frame) || frame is null || frame.Kind != BwmFrameKind.Request)
            return Task.FromResult(Array.Empty<byte>());

        var command = (BwmCommandCode)frame.CommandId;
        if (Fault == SimulationFault.UnsupportedCommand)
            return Task.FromResult(Array.Empty<byte>());

        byte[] payload = command switch
        {
            BwmCommandCode.GetVersionInfo => Utf8String(Version),
            BwmCommandCode.GetDeviceModel => EncodeUInt16LittleEndian(ModelId),
            BwmCommandCode.GetSysFreeHeap => EncodeUInt32LittleEndian(FreeHeap),
            BwmCommandCode.GetSysTimestamp => EncodeUInt32LittleEndian(SysTimestamp),
            BwmCommandCode.GetAppCompileDatetime => Utf8String(CompileDatetime),
            BwmCommandCode.GetSysTimeZone => Utf8String(TimeZone),
            BwmCommandCode.GetSysBaseMacAddr when BaseMac.Length == 6 => BaseMac.ToArray(),
            BwmCommandCode.GetSysUartCmdBaudRate => EncodeUInt32LittleEndian(UartBaudRate),
            BwmCommandCode.GetSysUartCmdMaxBaudRate => EncodeUInt32LittleEndian(UartMaxBaudRate),
            BwmCommandCode.GetSysNvsStats when NvsStats.Length == 20 => NvsStats.ToArray(),
            BwmCommandCode.GetLogUartForwardEnable => new[] { (byte)(LogUartForwardEnable ? 1 : 0) },
            BwmCommandCode.GetLogLevel => new[] { LogLevel },
            BwmCommandCode.GetSysReadyStatus => new[] { (byte)(Ready ? 1 : 0) },
            _ => Array.Empty<byte>()
        };

        if (Fault == SimulationFault.MalformedResponse)
            return Task.FromResult(new byte[] { 0x00, 0xFF, 0x00 });

        ushort responseCommand = frame.CommandId;
        if (Fault == SimulationFault.WrongCommandId)
            responseCommand = unchecked((ushort)(responseCommand + 1));

        byte[] response = Fault == SimulationFault.BroadcastInsteadOfResponse
            ? BwmFrameCodec.EncodeBroadcast(responseCommand, payload)
            : BwmFrameCodec.EncodeResponse(responseCommand, payload);

        return Task.FromResult(response);
    }

    private static byte[] Utf8String(string value) => Encoding.UTF8.GetBytes(value + "\0");

    private static byte[] EncodeUInt16LittleEndian(ushort value)
    {
        var bytes = new byte[sizeof(ushort)];
        BinaryPrimitives.WriteUInt16LittleEndian(bytes, value);
        return bytes;
    }

    private static byte[] EncodeUInt32LittleEndian(uint value)
    {
        var bytes = new byte[sizeof(uint)];
        BinaryPrimitives.WriteUInt32LittleEndian(bytes, value);
        return bytes;
    }

    public ValueTask DisposeAsync()
    {
        IsConnected = false;
        return ValueTask.CompletedTask;
    }
}
