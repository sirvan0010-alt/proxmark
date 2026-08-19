using PM5Control.Core.Protocols.Bwm;

namespace PM5Control.Core.Tests;

public sealed class BwmEventDispatcherTests
{
    [Fact]
    public void BroadcastFrameRaisesBroadcastEvent()
    {
        var dispatcher = new BwmEventDispatcher();
        BwmFrame? received = null;
        dispatcher.BroadcastReceived += frame => received = frame;

        dispatcher.Dispatch(new BwmFrame(BwmFrameKind.Broadcast, 1, new byte[] { 2 }));

        Assert.NotNull(received);
        Assert.Equal((ushort)1, received!.CommandId);
    }

    [Fact]
    public void RequestFrameDoesNotRaiseBroadcastEvent()
    {
        var dispatcher = new BwmEventDispatcher();
        var count = 0;
        dispatcher.BroadcastReceived += _ => count++;

        dispatcher.Dispatch(new BwmFrame(BwmFrameKind.Request, 1, Array.Empty<byte>()));

        Assert.Equal(0, count);
    }

    [Fact]
    public void ResponseFrameDoesNotRaiseBroadcastEvent()
    {
        var dispatcher = new BwmEventDispatcher();
        var count = 0;
        dispatcher.BroadcastReceived += _ => count++;

        dispatcher.Dispatch(new BwmFrame(BwmFrameKind.Response, 1, Array.Empty<byte>()));

        Assert.Equal(0, count);
    }
}
