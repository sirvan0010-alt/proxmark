using PM5Control.Core.Diagnostics;
using PM5Control.Core.Protocols.Bwm;

namespace PM5Control.Core.Tests;

public sealed class BwmReadOnlyAdapterV2Tests
{
    [Fact]
    public void Policy_ContainsOnlyFirstSessionSystemInspectorCommands()
    {
        Assert.Contains(BwmCommandCode.GetVersionInfo, BwmReadOnlyAdapter.AllowedCommands);
        Assert.Contains(BwmCommandCode.GetSysNvsStats, BwmReadOnlyAdapter.AllowedCommands);
        Assert.Contains(BwmCommandCode.GetSysReadyStatus, BwmReadOnlyAdapter.AllowedCommands);

        Assert.DoesNotContain(BwmCommandCode.SetSysTimestamp, BwmReadOnlyAdapter.AllowedCommands);
        Assert.DoesNotContain(BwmCommandCode.Reboot, BwmReadOnlyAdapter.AllowedCommands);
        Assert.DoesNotContain(BwmCommandCode.GetWifiConnectCfgPassword, BwmReadOnlyAdapter.AllowedCommands);
        Assert.DoesNotContain(BwmCommandCode.GetMqttClientPassword, BwmReadOnlyAdapter.AllowedCommands);
        Assert.DoesNotContain(BwmCommandCode.GetMqttClientCckey, BwmReadOnlyAdapter.AllowedCommands);
    }

    [Fact]
    public async Task GetDeviceModelIdAsync_DecodesLittleEndianUInt16()
    {
        var transport = new FakeTransport
        {
            OnSend = _ => BwmFrameCodec.EncodeResponse(
                (ushort)BwmCommandCode.GetDeviceModel,
                new byte[] { 0x10, 0xDA })
        };
        var adapter = new BwmReadOnlyAdapter(transport);

        var result = await adapter.GetDeviceModelIdAsync();

        Assert.Equal((ushort)0xDA10, result.Value);
        Assert.Equal(DiagnosticSourceState.Reported, result.SourceState);
    }

    [Fact]
    public async Task GetSysTimestampAsync_RejectsWrongPayloadLength()
    {
        var transport = new FakeTransport
        {
            OnSend = _ => BwmFrameCodec.EncodeResponse(
                (ushort)BwmCommandCode.GetSysTimestamp,
                new byte[] { 0x01, 0x02 })
        };
        var adapter = new BwmReadOnlyAdapter(transport);

        var result = await adapter.GetSysTimestampAsync();

        Assert.Equal(DiagnosticSourceState.Unknown, result.SourceState);
    }

    [Fact]
    public async Task GetSysNvsStatsAsync_PreservesDocumentedTwentyBytePayloadRaw()
    {
        var payload = Enumerable.Range(0, 20).Select(i => (byte)i).ToArray();
        var transport = new FakeTransport
        {
            OnSend = _ => BwmFrameCodec.EncodeResponse(
                (ushort)BwmCommandCode.GetSysNvsStats,
                payload)
        };
        var adapter = new BwmReadOnlyAdapter(transport);

        var result = await adapter.GetSysNvsStatsAsync();

        Assert.Equal(payload, result.Value);
        Assert.Equal(DiagnosticSourceState.Reported, result.SourceState);
    }

    [Fact]
    public async Task GetSysReadyStatusAsync_DecodesOneByteStatus()
    {
        var transport = new FakeTransport
        {
            OnSend = _ => BwmFrameCodec.EncodeResponse(
                (ushort)BwmCommandCode.GetSysReadyStatus,
                new byte[] { 1 })
        };
        var adapter = new BwmReadOnlyAdapter(transport);

        var result = await adapter.GetSysReadyStatusAsync();

        Assert.True(result.Value);
        Assert.Equal(DiagnosticSourceState.Reported, result.SourceState);
    }

    [Fact]
    public async Task QueryPayloadAsync_ReturnsValidatedPayloadWithoutSemanticGuessing()
    {
        var payload = new byte[] { 0xAA, 0xBB, 0xCC };
        var transport = new FakeTransport
        {
            OnSend = _ => BwmFrameCodec.EncodeResponse(
                (ushort)BwmCommandCode.GetLogLevel,
                payload)
        };
        var adapter = new BwmReadOnlyAdapter(transport);

        var result = await adapter.QueryPayloadAsync(BwmCommandCode.GetLogLevel);

        Assert.Equal(payload, result.Value);
        Assert.Equal(DiagnosticConfidence.Medium, result.Confidence);
    }

    [Fact]
    public async Task QueryAsync_RejectsReadOnlyButOutOfScopeNetworkGetter()
    {
        var sendCount = 0;
        var transport = new FakeTransport
        {
            OnSend = _ =>
            {
                sendCount++;
                return BwmFrameCodec.EncodeResponse((ushort)BwmCommandCode.GetWifiCfgCountry, Array.Empty<byte>());
            }
        };
        var adapter = new BwmReadOnlyAdapter(transport);

        var frame = await adapter.QueryAsync(BwmCommandCode.GetWifiCfgCountry);

        Assert.Null(frame);
        Assert.Equal(0, sendCount);
    }
}
