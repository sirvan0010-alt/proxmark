// PM5 Control Center — PM3 NG response stream parser
// PURPOSE: recover PM3 NG response frames from an arbitrary byte stream.
// BWM 8089 DATA_FORWARD is an outer envelope; its payload may fragment or
// coalesce PM3 NG frames. This parser deliberately knows nothing about BWM.
// SCOPE: PM3 NG responses only. It does not decode realtime LF sample streams.
namespace PM5Control.Core.Protocols.Pm3;

public sealed class Pm3NgResponseStreamParser
{
    private readonly List<byte> _buffer = new();

    public event Action<Pm3NgResponse>? ResponseReceived;

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
            if (_buffer.Count < Pm3NgFrame.ResponseHeaderSize)
                return;

            var header = _buffer.GetRange(0, Pm3NgFrame.ResponseHeaderSize).ToArray();
            if (!Pm3NgFrame.TryGetResponseLength(header, out var frameLength))
            {
                _buffer.RemoveAt(0);
                continue;
            }

            if (_buffer.Count < frameLength)
                return;

            var candidate = _buffer.GetRange(0, frameLength).ToArray();
            if (!Pm3NgFrame.TryDecodeResponse(candidate, out var response) || response is null)
            {
                _buffer.RemoveAt(0);
                continue;
            }

            _buffer.RemoveRange(0, frameLength);
            ResponseReceived?.Invoke(response);
        }
    }
}
