using PM5Control.Core.Connections;
using PM5Control.Core.Diagnostics;
using PM5Control.Core.Protocols.Bwm;

namespace PM5Control.Core.Tests;

/// <summary>
/// Minimal in-memory IProxmarkTransport test double. Not the eventual
/// mock BWM device (that is a separate, more complete piece of work per
/// the project plan) — just enough to exercise BwmReadOnlyAdapter's
/// request/response and error handling without a real transport.
/// </summary>
internal sealed class FakeTransport : IProxmarkTransport
{
    public string TransportName => "fake";
    public bool IsConnected { get; private set; } = true;

    public event Action<ReadOnlyMemory<byte>>? DataReceived;

    public Func<byte[], byte[]>? OnSend { get; set; }
    public Exception? ThrowOnSend { get; set; }

    public Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        IsConnected = true;
        return Task.CompletedTask;
    }

    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        IsConnected = false;
        return Task.CompletedTask;
    }

    public Task<byte[]> SendAsync(ReadOnlyMemory<byte> request, CancellationToken cancellationToken = default)
    {
        if (ThrowOnSend is not null)
            throw ThrowOnSend;

        var response = OnSend?.Invoke(request.ToArray()) ?? Array.Empty<byte>();
        return Task.FromResult(response);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

public class BwmReadOnlyAdapterTests
{
    [Fact]
    public async Task QueryAsync_ReturnsDecodedFrame_WhenResponseIsWellFormed()
    {
        var transport = new FakeTransport
        {
            OnSend = _ => BwmFrameCodec.EncodeResponse(
                (ushort)BwmCommandCode.GetSysFreeHeap,
                new byte[] { 0x01, 0x02, 0x03, 0x04 })
        };
        var adapter = new BwmReadOnlyAdapter(transport);

        var frame = await adapter.QueryAsync(BwmCommandCode.GetSysFreeHeap);

        Assert.NotNull(frame);
        Assert.Equal(BwmFrameKind.Response, frame!.Kind);
        Assert.Equal((ushort)BwmCommandCode.GetSysFreeHeap, frame.CommandId);
    }

    [Fact]
    public async Task QueryAsync_RejectsMutatingCommand_BeforeTransportCall()
    {
        var sendCount = 0;
        var transport = new FakeTransport
        {
            OnSend = _ =>
            {
                sendCount++;
                return BwmFrameCodec.EncodeResponse((ushort)BwmCommandCode.SetSysTimestamp, Array.Empty<byte>());
            }
        };
        var adapter = new BwmReadOnlyAdapter(transport);

        var frame = await adapter.QueryAsync(BwmCommandCode.SetSysTimestamp);

        Assert.Null(frame);
        Assert.Equal(0, sendCount);
    }

    [Fact]
    public async Task QueryAsync_PropagatesCancellation()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var transport = new FakeTransport { ThrowOnSend = new OperationCanceledException(cts.Token) };
        var adapter = new BwmReadOnlyAdapter(transport);

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => adapter.QueryAsync(BwmCommandCode.GetVersionInfo, cancellationToken: cts.Token));
    }

    [Fact]
    public async Task QueryAsync_ReturnsNull_WhenTransportThrows()
    {
        var transport = new FakeTransport { ThrowOnSend = new TimeoutException("simulated timeout") };
        var adapter = new BwmReadOnlyAdapter(transport);

        var frame = await adapter.QueryAsync(BwmCommandCode.GetVersionInfo);

        Assert.Null(frame);
    }

    [Fact]
    public async Task QueryAsync_ReturnsNull_WhenResponseIsMalformed()
    {
        var transport = new FakeTransport { OnSend = _ => new byte[] { 0x01, 0x02, 0x03 } };
        var adapter = new BwmReadOnlyAdapter(transport);

        var frame = await adapter.QueryAsync(BwmCommandCode.GetVersionInfo);

        Assert.Null(frame);
    }

    [Fact]
    public async Task QueryAsync_ReturnsNull_WhenResponseCommandIdDoesNotMatchRequest()
    {
        var transport = new FakeTransport
        {
            OnSend = _ => BwmFrameCodec.EncodeResponse((ushort)BwmCommandCode.GetDeviceModel, Array.Empty<byte>())
        };
        var adapter = new BwmReadOnlyAdapter(transport);

        // Requested GetVersionInfo but firmware (in this test) answered with a
        // different command id — adapter must not silently accept a mismatched reply.
        var frame = await adapter.QueryAsync(BwmCommandCode.GetVersionInfo);

        Assert.Null(frame);
    }

    [Fact]
    public async Task QueryAsync_ReturnsNull_WhenResponseIsActuallyABroadcastFrame()
    {
        var transport = new FakeTransport
        {
            OnSend = _ => BwmFrameCodec.EncodeBroadcast((ushort)BwmCommandCode.GetVersionInfo, Array.Empty<byte>())
        };
        var adapter = new BwmReadOnlyAdapter(transport);

        var frame = await adapter.QueryAsync(BwmCommandCode.GetVersionInfo);

        Assert.Null(frame);
    }

    [Fact]
    public async Task GetVersionInfoAsync_DecodesUtf8Payload_WithMediumConfidence()
    {
        var transport = new FakeTransport
        {
            OnSend = _ => BwmFrameCodec.EncodeResponse(
                (ushort)BwmCommandCode.GetVersionInfo,
                System.Text.Encoding.UTF8.GetBytes("v1.2.3"))
        };
        var adapter = new BwmReadOnlyAdapter(transport);

        var result = await adapter.GetVersionInfoAsync();

        Assert.Equal("v1.2.3", result.Value);
        Assert.Equal(DiagnosticSourceState.Reported, result.SourceState);
        Assert.Equal(DiagnosticConfidence.Medium, result.Confidence);
    }

    [Fact]
    public async Task GetVersionInfoAsync_ReturnsUnknown_WhenNoResponse()
    {
        var transport = new FakeTransport { OnSend = _ => Array.Empty<byte>() };
        var adapter = new BwmReadOnlyAdapter(transport);

        var result = await adapter.GetVersionInfoAsync();

        Assert.Null(result.Value);
        Assert.Equal(DiagnosticSourceState.Unknown, result.SourceState);
        Assert.Equal(DiagnosticConfidence.Unknown, result.Confidence);
    }

    [Fact]
    public async Task GetSysFreeHeapAsync_DecodesLittleEndianUInt32()
    {
        var transport = new FakeTransport
        {
            OnSend = _ => BwmFrameCodec.EncodeResponse(
                (ushort)BwmCommandCode.GetSysFreeHeap,
                new byte[] { 0x00, 0x10, 0x00, 0x00 }) // 0x1000 = 4096, little-endian
        };
        var adapter = new BwmReadOnlyAdapter(transport);

        var result = await adapter.GetSysFreeHeapAsync();

        Assert.Equal(4096L, result.Value);
        Assert.Equal(DiagnosticConfidence.Medium, result.Confidence);
    }

    [Fact]
    public async Task GetSysBaseMacAddrAsync_FormatsSixByteMacAsColonHex()
    {
        var transport = new FakeTransport
        {
            OnSend = _ => BwmFrameCodec.EncodeResponse(
                (ushort)BwmCommandCode.GetSysBaseMacAddr,
                new byte[] { 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF })
        };
        var adapter = new BwmReadOnlyAdapter(transport);

        var result = await adapter.GetSysBaseMacAddrAsync();

        Assert.Equal("AA:BB:CC:DD:EE:FF", result.Value);
    }

    [Fact]
    public async Task GetSysBaseMacAddrAsync_ReturnsUnknown_WhenPayloadLengthIsWrong()
    {
        var transport = new FakeTransport
        {
            OnSend = _ => BwmFrameCodec.EncodeResponse(
                (ushort)BwmCommandCode.GetSysBaseMacAddr,
                new byte[] { 0xAA, 0xBB, 0xCC }) // wrong length, not 6 bytes
        };
        var adapter = new BwmReadOnlyAdapter(transport);

        var result = await adapter.GetSysBaseMacAddrAsync();

        Assert.Null(result.Value);
        Assert.Equal(DiagnosticSourceState.Unknown, result.SourceState);
    }

    [Fact]
    public async Task ReadDeviceInfoAsync_LeavesNonBwmFieldsUnknown_AndFillsOnlyBwmScopedFields()
    {
        var transport = new FakeTransport
        {
            OnSend = request =>
            {
                // Decode which command was requested and answer accordingly,
                // so both GetDeviceModel and GetVersionInfo calls resolve.
                BwmFrameCodec.TryDecode(request, out var requested);
                var cmd = requested!.CommandId;
                if (cmd == (ushort)BwmCommandCode.GetDeviceModel)
                    return BwmFrameCodec.EncodeResponse(cmd, System.Text.Encoding.UTF8.GetBytes("ESP32-C2"));
                if (cmd == (ushort)BwmCommandCode.GetVersionInfo)
                    return BwmFrameCodec.EncodeResponse(cmd, System.Text.Encoding.UTF8.GetBytes("1.0.0"));
                return Array.Empty<byte>();
            }
        };
        var adapter = new BwmReadOnlyAdapter(transport);

        var info = await adapter.ReadDeviceInfoAsync();

        Assert.Equal("ESP32-C2", info.Esp32Model.Value);
        Assert.Equal("1.0.0", info.BwmFirmware.Value);

        // Fields outside BWM's scope (main PM5 ARM/FPGA subsystem) must stay
        // Unknown, not guessed.
        Assert.Equal(DiagnosticSourceState.Unknown, info.Model.SourceState);
        Assert.Equal(DiagnosticSourceState.Unknown, info.HardwareRevision.SourceState);
        Assert.Equal(DiagnosticSourceState.Unknown, info.ArmFirmware.SourceState);
        Assert.Equal(DiagnosticSourceState.Unknown, info.FpgaFirmware.SourceState);
    }
}
