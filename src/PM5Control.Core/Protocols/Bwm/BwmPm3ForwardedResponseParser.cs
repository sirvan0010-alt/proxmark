// PM5 Control Center — BWM -> PM3 NG stream demultiplexer
// VERIFIED outer message type: BWM broadcast 8089 (DATA_FORWARD).
// The PM3 parser is deliberately kept separate from BWM framing.
// This class accepts already-validated BWM frames and feeds only DATA_FORWARD
// payloads into the PM3 NG response stream parser.
namespace PM5Control.Core.Protocols.Bwm;

using PM5Control.Core.Protocols.Pm3;

public sealed class BwmPm3ForwardedResponseParser
{
    private readonly Pm3NgResponseStreamParser _pm3 = new();

    public event Action<Pm3NgResponse>? ResponseReceived;

    public BwmPm3ForwardedResponseParser()
    {
        _pm3.ResponseReceived += response => ResponseReceived?.Invoke(response);
    }

    public void Append(BwmFrame frame)
    {
        if (frame.Kind != BwmFrameKind.Broadcast ||
            frame.CommandId != (ushort)BwmBroadcastType.DataForward)
            return;

        // The PM3 data path behind BWM/FPC is capped at 2048 bytes per
        // forwarded BWM payload. A larger PM3 NG response is therefore
        // expected to arrive fragmented across multiple DATA_FORWARD frames.
        if (!Pm3TransportLimits.Fits(Pm3TransportKind.BwmFpc, frame.Payload.Length))
            return;

        _pm3.Append(frame.Payload);
    }
}
