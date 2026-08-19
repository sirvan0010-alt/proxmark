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
            if (_buffer.Count < 6)
                return;

            var header = ReadUInt16(0);
            if (!IsKnownHeader(header))
            {
                _buffer.RemoveAt(0);
                continue;
            }

            var payloadLength = ReadUInt16(4);
            var frameLength = 2 + 2 + 2 + payloadLength + 2;
            if (_buffer.Count < frameLength)
                return;

            var candidate = _buffer.Take(frameLength).ToArray();
            if (!TryDecode(candidate, out var frame))
            {
                _buffer.RemoveAt(0);
                continue;
            }

            _buffer.RemoveRange(0, frameLength);
            FrameReceived?.Invoke(frame);
        }
    }

    private bool TryDecode(byte[] bytes, out BwmFrame frame)
    {
        frame = default!;
        var header = BitConverter.ToUInt16(bytes, 0);
        var command = BitConverter.ToUInt16(bytes, 2);
        var length = BitConverter.ToUInt16(bytes, 4);
        var payload = bytes.AsSpan(6, length);
        var expectedCrc = BitConverter.ToUInt16(bytes, 6 + length);
        var actualCrc = BwmCrc16.Compute(bytes.AsSpan(0, 6 + length));

        if (expectedCrc != actualCrc)
            return false;

        frame = new BwmFrame(header, command, payload.ToArray());
        return true;
    }

    private static bool IsKnownHeader(ushort header) =>
        header == BwmProtocolConstants.RequestHeader ||
        header == BwmProtocolConstants.ResponseHeader ||
        header == BwmProtocolConstants.BroadcastHeader;

    private ushort ReadUInt16(int offset) =>
        (ushort)(_buffer[offset] | (_buffer[offset + 1] << 8));
}
