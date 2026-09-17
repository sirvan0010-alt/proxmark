using PM5Control.Core.Protocols.Bwm;
using PM5Control.Core.Protocols.Pm3;

namespace PM5Control.Core.Tests;

public sealed class BwmWirelessAbortEncodingTests
{
    [Fact]
    public void ForwardCommandUsesVerifiedUpstreamId()
    {
        Assert.Equal((ushort)5000, (ushort)BwmCommandCode.SendForwardData);
    }

    [Fact]
    public void BreakLoopIsWrappedAsBwmTransparentForwardPayload()
    {
        var pm3BreakLoop = Pm3NgFrame.EncodeCommand(Pm3CommandCode.BreakLoop);
        var bwmFrame = BwmFrameCodec.EncodeRequest(
            (ushort)BwmCommandCode.SendForwardData,
            pm3BreakLoop);

        Assert.Equal(
            Convert.FromHexString("7CC788130A00504D3361008018016133D0A9"),
            bwmFrame);

        Assert.True(BwmFrameCodec.TryDecode(bwmFrame, out var decoded));
        Assert.NotNull(decoded);
        Assert.Equal(BwmFrameKind.Request, decoded!.Kind);
        Assert.Equal((ushort)5000, decoded.CommandId);
        Assert.Equal(pm3BreakLoop, decoded.Payload);
    }
}
