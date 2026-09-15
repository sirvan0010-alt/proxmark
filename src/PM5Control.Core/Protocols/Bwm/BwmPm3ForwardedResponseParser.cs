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

        _pm3.Append(frame.Payload);
    }
}
