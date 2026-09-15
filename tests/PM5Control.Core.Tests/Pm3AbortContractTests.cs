using PM5Control.Core.Protocols.Pm3;

namespace PM5Control.Core.Tests;

public sealed class Pm3AbortContractTests
{
    [Fact]
    public void BreakLoopUsesVerifiedUpstreamCommandId()
    {
        Assert.Equal((ushort)0x0118, Pm3CommandCode.BreakLoop);
        Assert.False(Pm3CommandCode.IsSafeReadOnlyProbe(Pm3CommandCode.BreakLoop));
    }

    [Fact]
    public void BreakLoopEncodesAsEmptyPayloadNgCommand()
    {
        var frame = Pm3NgFrame.EncodeCommand(Pm3CommandCode.BreakLoop);

        Assert.Equal(Convert.FromHexString("504D3361008018013361"), frame);
    }
}
