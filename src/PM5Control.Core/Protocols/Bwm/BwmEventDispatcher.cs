// PM5 Control Center — BWM event dispatcher
// PURPOSE: separate unsolicited BWM broadcasts from normal request/response traffic.
// WHY: BWM can emit events independently of the command that is currently pending.
// SAFETY: dispatch only; no device mutation.

namespace PM5Control.Core.Protocols.Bwm;

public sealed class BwmEventDispatcher
{
    public event Action<BwmFrame>? BroadcastReceived;

    public void Dispatch(BwmFrame frame)
    {
        if (frame.IsBroadcast)
            BroadcastReceived?.Invoke(frame);
    }
}
