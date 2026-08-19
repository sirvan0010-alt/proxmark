using PM5Control.Core.Protocols.Bwm;

namespace PM5Control.Core.Tests;

public class BwmFrameCodecTests
{
    [Fact]
    public void RequestRoundTripPreservesCommandAndPayload()
    {
        var payload = new byte[] { 0x01, 0x02, 0xAA, 0x55 };
        var encoded = BwmFrameCodec.EncodeRequest(0x1234, payload);

        Assert.True(BwmFrameCodec.TryDecode(encoded, out var decoded));
        Assert.NotNull(decoded);
        Assert.Equal(BwmFrameKind.Request, decoded!.Kind);
        Assert.Equal((ushort)0x1234, decoded.CommandId);
        Assert.Equal(payload, decoded.Payload);
    }

    [Fact]
    public void CorruptedCrcIsRejected()
    {
        var encoded = BwmFrameCodec.EncodeRequest(0x0001, new byte[] { 0x10, 0x20 });
        encoded[^1] ^= 0x01;

        Assert.False(BwmFrameCodec.TryDecode(encoded, out _));
    }

    [Fact]
    public void WrongLengthIsRejected()
    {
        var encoded = BwmFrameCodec.EncodeRequest(0x0001, new byte[] { 0x10, 0x20 });
        encoded[4] = 0x03;

        Assert.False(BwmFrameCodec.TryDecode(encoded, out _));
    }

    [Fact]
    public void BroadcastIsRecognized()
    {
        var encoded = BwmFrameCodec.EncodeBroadcast(0x0002, new byte[] { 0x42 });

        Assert.True(BwmFrameCodec.TryDecode(encoded, out var decoded));
        Assert.Equal(BwmFrameKind.Broadcast, decoded!.Kind);
    }
}
