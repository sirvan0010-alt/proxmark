using PM5Control.Core.Diagnostics;
using PM5Control.Core.Protocols.Bwm;

namespace PM5Control.Core.Tests;

public sealed class BwmReadOnlyAdapterV2Tests
{
    [Fact]
    public void Policy_ContainsExactlyTheReviewedFirstSessionCommands()
    {
        var expected = new[]
        {
            BwmCommandCode.GetVersionInfo, BwmCommandCode.GetDeviceModel, BwmCommandCode.GetSysFreeHeap,
            BwmCommandCode.GetSysTimestamp, BwmCommandCode.GetAppCompileDatetime, BwmCommandCode.GetSysTimeZone,
            BwmCommandCode.GetSysBaseMacAddr, BwmCommandCode.GetSysUartCmdBaudRate, BwmCommandCode.GetSysUartCmdMaxBaudRate,
            BwmCommandCode.GetSysNvsStats, BwmCommandCode.GetLogUartForwardEnable, BwmCommandCode.GetLogLevel,
            BwmCommandCode.GetSysReadyStatus,
        };

        Assert.Equal(expected.Length, BwmReadOnlyAdapter.AllowedCommands.Count);
        Assert.All(expected, command => Assert.Contains(command, BwmReadOnlyAdapter.AllowedCommands));
        Assert.DoesNotContain(BwmCommandCode.SetSysTimestamp, BwmReadOnlyAdapter.AllowedCommands);
        Assert.DoesNotContain(BwmCommandCode.Reboot, BwmReadOnlyAdapter.AllowedCommands);
        Assert.DoesNotContain(BwmCommandCode.GetWifiConnectCfgPassword, BwmReadOnlyAdapter.AllowedCommands);
        Assert.DoesNotContain(BwmCommandCode.GetMqttClientPassword, BwmReadOnlyAdapter.AllowedCommands);
        Assert.DoesNotContain(BwmCommandCode.GetMqttClientCckey, BwmReadOnlyAdapter.AllowedCommands);
    }

    [Theory]
    [InlineData(BwmCommandCode.GetVersionInfo, 1)]
    [InlineData(BwmCommandCode.GetDeviceModel, 2)]
    [InlineData(BwmCommandCode.GetSysFreeHeap, 4)]
    [InlineData(BwmCommandCode.GetSysTimestamp, 4)]
    [InlineData(BwmCommandCode.GetAppCompileDatetime, 1)]
    [InlineData(BwmCommandCode.GetSysTimeZone, 1)]
    [InlineData(BwmCommandCode.GetSysBaseMacAddr, 6)]
    [InlineData(BwmCommandCode.GetSysUartCmdBaudRate, 4)]
    [InlineData(BwmCommandCode.GetSysUartCmdMaxBaudRate, 4)]
    [InlineData(BwmCommandCode.GetSysNvsStats, 20)]
    [InlineData(BwmCommandCode.GetLogUartForwardEnable, 1)]
    [InlineData(BwmCommandCode.GetLogLevel, 1)]
    [InlineData(BwmCommandCode.GetSysReadyStatus, 1)]
    public async Task QueryPayloadAsync_CoversAll13SourceDocumentedResponseShapes(BwmCommandCode command, int expectedLength)
    {
        var payload = new byte[expectedLength];
        var transport = new FakeTransport { OnSend = _ => BwmFrameCodec.EncodeResponse((ushort)command, payload) };
        var adapter = new BwmReadOnlyAdapter(transport);

        var result = await adapter.QueryPayloadAsync(command);

        Assert.Equal(payload, result.Value);
        Assert.Equal(DiagnosticSourceState.Reported, result.SourceState);
        Assert.Equal(DiagnosticConfidence.Medium, result.Confidence);
    }

    [Fact]
    public async Task GetDeviceModelIdAsync_DecodesLittleEndianUInt16()
    {
        var transport = new FakeTransport
        {
            OnSend = _ => BwmFrameCodec.EncodeResponse((ushort)BwmCommandCode.GetDeviceModel, new byte[] { 0x10, 0xDA })
        };
        var adapter = new BwmReadOnlyAdapter(transport);
        var result = await adapter.GetDeviceModelIdAsync();
        Assert.Equal((ushort)0xDA10, result.Value);
        Assert.Equal(DiagnosticSourceState.Reported, result.SourceState);
    }

    [Fact]
    public async Task QueryAsync_RejectsBadCrc()
    {
        var valid = BwmFrameCodec.EncodeResponse((ushort)BwmCommandCode.GetVersionInfo, new byte[] { 1 });
        valid[^1] ^= 0xFF;
        var adapter = new BwmReadOnlyAdapter(new FakeTransport { OnSend = _ => valid });
        Assert.Null(await adapter.QueryAsync(BwmCommandCode.GetVersionInfo));
    }

    [Fact]
    public async Task QueryAsync_RejectsWrongResponseCommand()
    {
        var transport = new FakeTransport
        {
            OnSend = _ => BwmFrameCodec.EncodeResponse((ushort)BwmCommandCode.GetSysTimestamp, new byte[4])
        };
        var adapter = new BwmReadOnlyAdapter(transport);
        Assert.Null(await adapter.QueryAsync(BwmCommandCode.GetVersionInfo));
    }

    [Fact]
    public async Task QueryAsync_RejectsBroadcastInsteadOfResponse()
    {
        var transport = new FakeTransport
        {
            OnSend = _ => BwmFrameCodec.EncodeBroadcast((ushort)BwmCommandCode.GetVersionInfo, new byte[] { 1 })
        };
        var adapter = new BwmReadOnlyAdapter(transport);
        Assert.Null(await adapter.QueryAsync(BwmCommandCode.GetVersionInfo));
    }

    [Fact]
    public async Task QueryAsync_RejectsOutOfScopeNetworkGetterBeforeTransport()
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
        Assert.Null(await adapter.QueryAsync(BwmCommandCode.GetWifiCfgCountry));
        Assert.Equal(0, sendCount);
    }

    [Fact]
    public async Task GetSysNvsStatsAsync_RejectsWrongPayloadLength()
    {
        var transport = new FakeTransport
        {
            OnSend = _ => BwmFrameCodec.EncodeResponse((ushort)BwmCommandCode.GetSysNvsStats, new byte[19])
        };
        var adapter = new BwmReadOnlyAdapter(transport);
        var result = await adapter.GetSysNvsStatsAsync();
        Assert.Equal(DiagnosticSourceState.Unknown, result.SourceState);
    }

    [Fact]
    public async Task AssessIdentityAsync_DoesNotConfuseBwmProtocolWithPhysicalPm5()
    {
        var transport = new FakeTransport
        {
            OnSend = request =>
            {
                Assert.True(BwmFrameCodec.TryDecode(request, out var frame));
                return frame!.CommandId switch
                {
                    (ushort)BwmCommandCode.GetDeviceModel => BwmFrameCodec.EncodeResponse(frame.CommandId, new byte[] { 0x10, 0xDA }),
                    (ushort)BwmCommandCode.GetVersionInfo => BwmFrameCodec.EncodeResponse(frame.CommandId, new byte[] { (byte)'B', (byte)'W', (byte)'M', 0 }),
                    _ => Array.Empty<byte>()
                };
            }
        };
        var adapter = new BwmReadOnlyAdapter(transport);
        var result = await adapter.AssessIdentityAsync();
        Assert.Equal("UNKNOWN", result.Family.Value);
        Assert.Equal("DO_NOT_RECOMMEND_FIRMWARE", result.FirmwareRecommendation.Value);
        Assert.Contains("PM5", result.HumanSummary.Value);
        Assert.Contains("PM3", result.HumanSummary.Value);
    }
}
