using System.Buffers.Binary;
using PM5Control.Core.Protocols.Bwm;
using PM5Control.Core.Protocols.Pm3;

namespace PM5Control.Core.Tests;

public sealed class BwmPm3ForwardedResponseParserTests
{
    [Fact]
    public void DataForwardPayloadIsDecodedAsPm3Response()
    {
        var pm3 = MakeResponse(Pm3CommandCode.Version, new byte[] { 1, 2 });
        var bwm = new BwmFrame(BwmFrameKind.Broadcast, (ushort)BwmBroadcastType.DataForward, pm3);
        var parser = new BwmPm3ForwardedResponseParser();
        Pm3NgResponse? received = null;
        parser.ResponseReceived += response => received = response;

        parser.Append(bwm);

        Assert.NotNull(received);
        Assert.Equal(Pm3CommandCode.Version, received!.Command);
    }

    [Fact]
    public void SplittingPm3AcrossTwoBwmFramesIsSupported()
    {
        var pm3 = MakeResponse(Pm3CommandCode.Status, new byte[] { 3, 4, 5, 6 });
        var split = 7;
        var parser = new BwmPm3ForwardedResponseParser();
        var count = 0;
        parser.ResponseReceived += _ => count++;

        parser.Append(new BwmFrame(BwmFrameKind.Broadcast, (ushort)BwmBroadcastType.DataForward, pm3[..split]));
        Assert.Equal(0, count);
        parser.Append(new BwmFrame(BwmFrameKind.Broadcast, (ushort)BwmBroadcastType.DataForward, pm3[split..]));

        Assert.Equal(1, count);
    }

    [Fact]
    public void MultiplePm3ResponsesInOneBwmPayloadAreDecoded()
    {
        var first = MakeResponse(Pm3CommandCode.Version, new byte[] { 1 });
        var second = MakeResponse(Pm3CommandCode.Ping, new byte[] { 2 });
        var payload = first.Concat(second).ToArray();
        var parser = new BwmPm3ForwardedResponseParser();
        var commands = new List<ushort>();
        parser.ResponseReceived += response => commands.Add(response.Command);

        parser.Append(new BwmFrame(BwmFrameKind.Broadcast, (ushort)BwmBroadcastType.DataForward, payload));

        Assert.Equal(new[] { Pm3CommandCode.Version, Pm3CommandCode.Ping }, commands);
    }

    [Fact]
    public void BwmAckAndOtherBroadcastsDoNotEnterPm3Parser()
    {
        var pm3 = MakeResponse(Pm3CommandCode.Version, new byte[] { 9 });
        var parser = new BwmPm3ForwardedResponseParser();
        var count = 0;
        parser.ResponseReceived += _ => count++;

        parser.Append(new BwmFrame(BwmFrameKind.Response, (ushort)BwmCommandCode.SendForwardData, pm3));
        parser.Append(new BwmFrame(BwmFrameKind.Broadcast, (ushort)BwmBroadcastType.SysLogMessage, pm3));

        Assert.Equal(0, count);
    }

    private static byte[] MakeResponse(ushort command, byte[] payload)
    {
        var frame = new byte[Pm3NgFrame.ResponseHeaderSize + payload.Length + Pm3NgFrame.PostambleSize];
        BinaryPrimitives.WriteUInt32LittleEndian(frame.AsSpan(0, 4), Pm3NgFrame.ResponseMagic);
        BinaryPrimitives.WriteUInt16LittleEndian(frame.AsSpan(4, 2), (ushort)(0x8000 | payload.Length));
        frame[6] = 0;
        frame[7] = 0;
        BinaryPrimitives.WriteUInt16LittleEndian(frame.AsSpan(8, 2), command);
        payload.CopyTo(frame.AsSpan(Pm3NgFrame.ResponseHeaderSize));
        BinaryPrimitives.WriteUInt16LittleEndian(frame.AsSpan(frame.Length - 2), Pm3NgFrame.ResponsePostambleMagic);
        return frame;
    }
}
