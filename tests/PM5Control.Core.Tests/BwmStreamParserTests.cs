using PM5Control.Core.Protocols.Bwm;

namespace PM5Control.Core.Tests;

public sealed class BwmStreamParserTests
{
    [Fact]
    public void CompleteFrameInOneAppendIsReceived()
    {
        var parser = new BwmStreamParser();
        BwmFrame? received = null;
        parser.FrameReceived += frame => received = frame;
        var expected = BwmFrameCodec.EncodeRequest(0x1234, new byte[] { 1, 2, 3 });

        parser.Append(expected);

        Assert.NotNull(received);
        Assert.Equal(BwmFrameKind.Request, received!.Kind);
        Assert.Equal((ushort)0x1234, received.CommandId);
        Assert.Equal(new byte[] { 1, 2, 3 }, received.Payload);
    }

    [Fact]
    public void FrameSplitIntoTwoChunksIsReceivedAfterSecondChunk()
    {
        var parser = new BwmStreamParser();
        var count = 0;
        parser.FrameReceived += _ => count++;
        var frame = BwmFrameCodec.EncodeResponse(1, new byte[] { 9, 8, 7, 6 });

        parser.Append(frame.AsSpan(0, 3));
        Assert.Equal(0, count);

        parser.Append(frame.AsSpan(3));
        Assert.Equal(1, count);
    }

    [Fact]
    public void ByteByByteFragmentationIsReceived()
    {
        var parser = new BwmStreamParser();
        var count = 0;
        parser.FrameReceived += _ => count++;
        var frame = BwmFrameCodec.EncodeBroadcast(2, new byte[] { 0xAA, 0xBB });

        foreach (var value in frame)
            parser.Append(new[] { value });

        Assert.Equal(1, count);
    }

    [Fact]
    public void TwoFramesInOneAppendAreBothReceived()
    {
        var parser = new BwmStreamParser();
        var frames = new List<BwmFrame>();
        parser.FrameReceived += frame => frames.Add(frame);
        var first = BwmFrameCodec.EncodeRequest(10, new byte[] { 1 });
        var second = BwmFrameCodec.EncodeResponse(11, new byte[] { 2, 3 });

        parser.Append(first.Concat(second).ToArray());

        Assert.Equal(2, frames.Count);
        Assert.Equal((ushort)10, frames[0].CommandId);
        Assert.Equal((ushort)11, frames[1].CommandId);
    }

    [Fact]
    public void RequestFrameKindIsDecoded()
    {
        var frame = ParseSingle(BwmFrameCodec.EncodeRequest(3, new byte[] { 4 }));
        Assert.Equal(BwmFrameKind.Request, frame.Kind);
    }

    [Fact]
    public void ResponseFrameKindIsDecoded()
    {
        var frame = ParseSingle(BwmFrameCodec.EncodeResponse(4, new byte[] { 5 }));
        Assert.Equal(BwmFrameKind.Response, frame.Kind);
    }

    [Fact]
    public void BroadcastFrameKindIsDecoded()
    {
        var frame = ParseSingle(BwmFrameCodec.EncodeBroadcast(5, new byte[] { 6 }));
        Assert.Equal(BwmFrameKind.Broadcast, frame.Kind);
    }

    [Fact]
    public void GarbageBeforeValidFrameIsDiscarded()
    {
        var parser = new BwmStreamParser();
        var count = 0;
        parser.FrameReceived += _ => count++;
        var valid = BwmFrameCodec.EncodeRequest(6, new byte[] { 7 });

        parser.Append(new byte[] { 0x00, 0xFF, 0x55, 0xAA }.Concat(valid).ToArray());

        Assert.Equal(1, count);
    }

    [Fact]
    public void GarbageBadFrameThenValidFrameResynchronizes()
    {
        var parser = new BwmStreamParser();
        var frames = new List<BwmFrame>();
        parser.FrameReceived += frame => frames.Add(frame);
        var bad = BwmFrameCodec.EncodeRequest(7, new byte[] { 0x10, 0x20 });
        bad[^1] ^= 0xFF;
        var valid = BwmFrameCodec.EncodeResponse(8, new byte[] { 0x30, 0x40 });

        parser.Append(new byte[] { 0x99, 0x88 }.Concat(bad).Concat(valid).ToArray());

        Assert.Single(frames);
        Assert.Equal((ushort)8, frames[0].CommandId);
    }

    [Fact]
    public void BadCrcDoesNotProduceFrame()
    {
        var parser = new BwmStreamParser();
        var count = 0;
        parser.FrameReceived += _ => count++;
        var frame = BwmFrameCodec.EncodeRequest(9, new byte[] { 1, 2 });
        frame[^1] ^= 0x01;

        parser.Append(frame);

        Assert.Equal(0, count);
    }

    [Fact]
    public void InvalidLengthDoesNotHangOrCrash()
    {
        var parser = new BwmStreamParser();
        var count = 0;
        parser.FrameReceived += _ => count++;
        var frame = new byte[BwmProtocolConstants.HeaderSize];
        frame[0] = (byte)BwmProtocolConstants.RequestMagic;
        frame[1] = (byte)(BwmProtocolConstants.RequestMagic >> 8);
        frame[4] = 0xFF;
        frame[5] = 0xFF;

        parser.Append(frame);

        Assert.Equal(0, count);
    }

    [Fact]
    public void UnknownMagicIsDiscardedByteByByte()
    {
        var parser = new BwmStreamParser();
        var count = 0;
        parser.FrameReceived += _ => count++;
        var bytes = Enumerable.Repeat((byte)0x7E, 32).ToArray();

        parser.Append(bytes);

        Assert.Equal(0, count);
    }

    [Fact]
    public void EmptyInputDoesNothing()
    {
        var parser = new BwmStreamParser();
        var count = 0;
        parser.FrameReceived += _ => count++;

        parser.Append(ReadOnlySpan<byte>.Empty);

        Assert.Equal(0, count);
    }

    [Fact]
    public void MaximumPayloadRoundTrips()
    {
        var parser = new BwmStreamParser();
        BwmFrame? received = null;
        parser.FrameReceived += frame => received = frame;
        var payload = Enumerable.Repeat((byte)0xA5, ushort.MaxValue).ToArray();
        var frame = BwmFrameCodec.EncodeRequest(12, payload);

        parser.Append(frame);

        Assert.NotNull(received);
        Assert.Equal(ushort.MaxValue, received!.Payload.Length);
        Assert.Equal(payload, received.Payload);
    }

    [Fact]
    public void IncompleteFrameIsCompletedByLaterAppend()
    {
        var parser = new BwmStreamParser();
        var count = 0;
        parser.FrameReceived += _ => count++;
        var frame = BwmFrameCodec.EncodeResponse(13, new byte[] { 0x01, 0x02, 0x03 });

        parser.Append(frame.AsSpan(0, frame.Length - 1));
        Assert.Equal(0, count);

        parser.Append(frame.AsSpan(frame.Length - 1));
        Assert.Equal(1, count);
    }

    private static BwmFrame ParseSingle(byte[] encoded)
    {
        var parser = new BwmStreamParser();
        BwmFrame? received = null;
        parser.FrameReceived += frame => received = frame;
        parser.Append(encoded);
        return Assert.NotNull(received);
    }
}
