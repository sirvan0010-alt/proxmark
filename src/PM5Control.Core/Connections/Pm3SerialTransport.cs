using System.Diagnostics;
using System.IO.Ports;
using PM5Control.Core.Protocols.Pm3;

namespace PM5Control.Core.Connections;

public sealed class Pm3SerialTransport : IAsyncDisposable
{
    private const int MaxUnmatchedResponses = 32;

    private readonly string _portName;
    private readonly int _baudRate;
    private readonly int _timeoutMs;
    private SerialPort? _port;

    public Pm3SerialTransport(string portName, int baudRate = 460800, int timeoutMs = 2000)
    {
        _portName = portName ?? throw new ArgumentNullException(nameof(portName));
        _baudRate = baudRate;
        _timeoutMs = timeoutMs;
    }

    public bool IsConnected => _port?.IsOpen == true;

    public Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (IsConnected) return Task.CompletedTask;
        var port = new SerialPort(_portName, _baudRate, Parity.None, 8, StopBits.One)
        {
            DtrEnable = false,
            RtsEnable = false,
            Handshake = Handshake.None,
            ReadTimeout = _timeoutMs,
            WriteTimeout = _timeoutMs,
            ReadBufferSize = 16384,
            WriteBufferSize = 16384
        };
        port.Open();
        _port = port;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Sends one whitelisted command and correlates the response by command ID.
    /// Debug-print frames are retained separately. Non-debug responses for another
    /// command are retained as unmatched frames and do not terminate the transaction.
    /// The transaction has both a hard wall-clock deadline and a maximum unmatched
    /// frame budget so a response storm can never trap the caller in an unbounded loop.
    /// No command is retransmitted automatically.
    ///
    /// When a response storm occurs, the exception includes the exact request frame
    /// and the last unmatched response frame. This is intentionally read-only diagnostic
    /// data so PM5 protocol framing can be compared against an upstream client.
    /// </summary>
    public async Task<Pm3NgExchange> SendReadOnlyAsync(ushort command, CancellationToken cancellationToken = default)
    {
        if (!IsConnected) throw new InvalidOperationException("PM3 serial transport is not connected.");

        var port = _port!;
        port.DiscardInBuffer();
        var request = Pm3NgFrame.EncodeCommand(command);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_timeoutMs);

        await port.BaseStream.WriteAsync(request, timeout.Token).ConfigureAwait(false);
        await port.BaseStream.FlushAsync(timeout.Token).ConfigureAwait(false);

        var deadline = Stopwatch.GetTimestamp() + Stopwatch.Frequency * _timeoutMs / 1000;
        var debugFrames = new List<Pm3NgResponse>();
        var unmatchedResponses = new List<Pm3NgResponse>();

        while (true)
        {
            timeout.Token.ThrowIfCancellationRequested();

            var remainingMs = GetRemainingMilliseconds(deadline);
            if (remainingMs <= 0)
                throw new TimeoutException($"PM3 transaction timed out waiting for CMD 0x{command:X4}; unmatched={unmatchedResponses.Count}, debug={debugFrames.Count}; TX={Hex(request)}");

            var header = await ReadExactAsync(port.BaseStream, Pm3NgFrame.ResponseHeaderSize, timeout.Token, remainingMs).ConfigureAwait(false);
            if (!Pm3NgFrame.TryGetResponseLength(header, out var totalLength))
                throw new InvalidDataException($"PM3 endpoint did not return a valid NG response header; TX={Hex(request)}; RX_HEADER={Hex(header)}");

            remainingMs = GetRemainingMilliseconds(deadline);
            if (remainingMs <= 0)
                throw new TimeoutException($"PM3 transaction timed out while reading CMD 0x{command:X4}; unmatched={unmatchedResponses.Count}, debug={debugFrames.Count}; TX={Hex(request)}");

            var tail = await ReadExactAsync(port.BaseStream, totalLength - header.Length, timeout.Token, remainingMs).ConfigureAwait(false);
            var frame = new byte[totalLength];
            Buffer.BlockCopy(header, 0, frame, 0, header.Length);
            Buffer.BlockCopy(tail, 0, frame, header.Length, tail.Length);
            if (!Pm3NgFrame.TryDecodeResponse(frame, out var response) || response is null)
                throw new InvalidDataException($"PM3 endpoint returned an invalid NG response frame; TX={Hex(request)}; RX={Hex(frame)}");

            if (Pm3CommandCode.IsDebugResponse(response.Command))
            {
                debugFrames.Add(response);
                continue;
            }

            if (response.Command == command)
                return new Pm3NgExchange(request, response, debugFrames, unmatchedResponses);

            unmatchedResponses.Add(response);
            if (unmatchedResponses.Count >= MaxUnmatchedResponses)
            {
                throw new TimeoutException(
                    $"PM3 response storm detected while waiting for CMD 0x{command:X4}; " +
                    $"received {unmatchedResponses.Count} unmatched frames, last=0x{response.Command:X4}; " +
                    $"TX={Hex(request)}; RX_LAST={Hex(response.RawFrame)}");
            }
        }
    }

    private static int GetRemainingMilliseconds(long deadline)
    {
        var remainingTicks = deadline - Stopwatch.GetTimestamp();
        if (remainingTicks <= 0) return 0;
        var remainingMs = (long)Math.Ceiling(remainingTicks * 1000.0 / Stopwatch.Frequency);
        return remainingMs > int.MaxValue ? int.MaxValue : (int)remainingMs;
    }

    private static string Hex(byte[] bytes) => Convert.ToHexString(bytes);

    private static async Task<byte[]> ReadExactAsync(Stream stream, int count, CancellationToken cancellationToken, int timeoutMs)
    {
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
        var buffer = new byte[count];
        var offset = 0;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(timeoutMs);
        while (offset < count)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(offset, count - offset), timeout.Token).ConfigureAwait(false);
            if (read == 0) throw new EndOfStreamException("PM3 serial connection closed while waiting for a response.");
            offset += read;
        }
        return buffer;
    }

    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _port?.Close();
        _port?.Dispose();
        _port = null;
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync() => await DisconnectAsync().ConfigureAwait(false);
}
