// PM5 Control Center — BWM stream parser
// PURPOSE: convert arbitrary transport byte chunks into validated BWM frames.
// WHY: USB/TCP/BLE reads are not guaranteed to align with protocol frame boundaries.
// SAFETY: parser is read/validation only; it does not issue device commands.
// See docs/ARCHITECTURE.md.

namespace PM5Control.Core.Protocols.Bwm;

public sealed class BwmStreamParser
{
    private readonly List<byte> _buffer = new();

    public event Action<BwmFrame>? FrameReceived;

    public void Append(ReadOnlySpan<byte> bytes)
    {
        foreach (var b in bytes)
            _buffer.Add(b);

        ParseAvailable();
    }

    private void ParseAvailable()
    {
        while (true)
        {
            if (_buffer.Count < BwmProtocolConstants.HeaderSize)
                return;

            var magic = ReadUInt16(0);
            if (!IsKnownHeader(magic))
            {
                _buffer.RemoveAt(0);
                continue;
            }

            var payloadLength = ReadUInt16(4);
            var frameLength = BwmProtocolConstants.HeaderSize + payloadLength + BwmProtocolConstants.CrcSize;
            if (_buffer.Count < frameLength)
                return;

            var candidate = _buffer.GetRange(0, frameLength).ToArray();
            if (!BwmFrameCodec.TryDecode(candidate, out var frame) || frame is null)
            {
                // Resynchronise one byte at a time. A valid frame may begin inside
                // a malformed candidate, so never discard the whole candidate.
                _buffer.RemoveAt(0);
                continue;
            }

            _buffer.RemoveRange(0, frameLength);
            FrameReceived?.Invoke(frame);
        }
    }

    private static bool IsKnownHeader(ushort magic) =>
        magic == BwmProtocolConstants.RequestMagic ||
        magic == BwmProtocolConstants.ResponseMagic ||
        magic == BwmProtocolConstants.BroadcastMagic;

    private ushort ReadUInt16(int offset) =>
        (ushort)(_buffer[offset] | (_buffer[offset + 1] << 8));
}
