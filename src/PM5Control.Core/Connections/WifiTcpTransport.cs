using System.Diagnostics;
using PM5Control.Core.Protocols.Pm3;

namespace PM5Control.Core.Connections;

/// <summary>
/// Native PM5 BWM Wi-Fi/TCP transport.
///
/// The BWM terminates the Wi-Fi side and exposes the PM3-NG command stream
/// directly on TCP (default upstream port 7777). No BWM frame is wrapped around
/// the PM3 frame on this link.
/// </summary>
public sealed class WifiTcpTransport : IProxmarkTransport, IPm3ReadOnlyTransport, IPm3CommandTransport, IProxmarkAbortTransport
{
    private readonly string _host;
    private readonly int _port;
    private readonly int _timeoutMs;
    private TcpClient? _client;
    private NetworkStream? _stream;
    private readonly SemaphoreSlim _ioGate = new(1, 1);

    public WifiTcpTransport(string host, int port = 7777, int timeoutMs = 5000)
    {
        if (string.IsNullOrWhiteSpace(host)) throw new ArgumentException("Host is required.", nameof(host));
        if (port is < 1 or > 65535) throw new ArgumentOutOfRangeException(nameof(port));
        if (timeoutMs <= 0) throw new ArgumentOutOfRangeException(nameof(timeoutMs));
        _host = host;
        _port = port;
        _timeoutMs = timeoutMs;
    }

    public string TransportName => $"Wi-Fi / TCP {_host}:{_port}";
    public bool IsConnected => _client?.Connected == true && _stream is not null;
    public event Action<ReadOnlyMemory<byte>>? DataReceived;

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (IsConnected) return;
        var client = new TcpClient { NoDelay = true };
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_timeoutMs);
        await client.ConnectAsync(_host, _port, timeout.Token).ConfigureAwait(false);
        _client = client;
        _stream = client.GetStream();
    }

    public async Task<Pm3NgExchange> SendReadOnlyAsync(ushort command, CancellationToken cancellationToken = default)
    {
        if (!Pm3CommandCode.IsSafeReadOnlyProbe(command))
            throw new InvalidOperationException($"Command 0x{command:X4} is outside the read-only Wi-Fi probe policy.");
        return await SendCommandAsync(command, ReadOnlyMemory<byte>.Empty, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Pm3NgExchange> SendCommandAsync(ushort command, ReadOnlyMemory<byte> payload = default, CancellationToken cancellationToken = default)
    {
        if (!IsConnected) throw new InvalidOperationException("PM5 Wi-Fi/TCP transport is not connected.");
        var request = Pm3NgFrame.EncodeCommand(command, payload.Span);
        await _ioGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var stream = _stream ?? throw new InvalidOperationException("TCP stream is unavailable.");
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(_timeoutMs);
            await stream.WriteAsync(request, timeout.Token).ConfigureAwait(false);
            await stream.FlushAsync(timeout.Token).ConfigureAwait(false);

            var deadline = Stopwatch.GetTimestamp() + Stopwatch.Frequency * _timeoutMs / 1000;
            var debug = new List<Pm3NgResponse>();
            var unmatched = new List<Pm3NgResponse>();
            while (true)
            {
                timeout.Token.ThrowIfCancellationRequested();
                var remaining = RemainingMilliseconds(deadline);
                if (remaining <= 0) throw new TimeoutException($"PM5 Wi-Fi/TCP transaction timed out waiting for CMD 0x{command:X4}; unmatched={unmatched.Count}; debug={debug.Count}.");
                var response = await ReadResponseAsync(stream, timeout.Token, remaining).ConfigureAwait(false);
                if (response is null) throw new EndOfStreamException("PM5 Wi-Fi/TCP connection closed while waiting for a response.");
                DataReceived?.Invoke(response.RawFrame);
                if (Pm3CommandCode.IsDebugResponse(response.Command)) { debug.Add(response); continue; }
                if (response.Command == command)
                    return new Pm3NgExchange(request, response, debug, unmatched);
                unmatched.Add(response);
                if (unmatched.Count >= 32)
                    throw new TimeoutException($"PM5 Wi-Fi/TCP response storm while waiting for CMD 0x{command:X4}; last=0x{response.Command:X4}.");
            }
        }
        finally { _ioGate.Release(); }
    }

    public Task<byte[]> SendAsync(ReadOnlyMemory<byte> request, CancellationToken cancellationToken = default)
    {
        if (request.Length < Pm3NgFrame.CommandHeaderSize + Pm3NgFrame.PostambleSize)
            throw new InvalidDataException("PM5 Wi-Fi/TCP transport received an undersized PM3 NG frame.");
        var command = BitConverter.ToUInt16(request.Span.Slice(6, 2));
        var payloadLength = (BitConverter.ToUInt16(request.Span.Slice(4, 2)) & 0x7FFF);
        var payload = request.Span.Slice(Pm3NgFrame.CommandHeaderSize, payloadLength).ToArray();
        return SendCommandAsync(command, payload, cancellationToken).ContinueWith(t => t.Result.Response.RawFrame, cancellationToken, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }

    public async Task AbortCurrentOperationAsync(CancellationToken cancellationToken = default)
    {
        _ = await SendCommandAsync(Pm3CommandCode.BreakLoop, ReadOnlyMemory<byte>.Empty, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<Pm3NgResponse?> ReadResponseAsync(Stream stream, CancellationToken cancellationToken, int timeoutMs)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(timeoutMs);
        var header = await ReadExactAsync(stream, Pm3NgFrame.ResponseHeaderSize, timeout.Token).ConfigureAwait(false);
        if (!Pm3NgFrame.TryGetResponseLength(header, out var totalLength))
            throw new InvalidDataException($"Invalid PM3 NG response header: {Convert.ToHexString(header)}");
        var tail = await ReadExactAsync(stream, totalLength - header.Length, timeout.Token).ConfigureAwait(false);
        var frame = new byte[totalLength];
        Buffer.BlockCopy(header, 0, frame, 0, header.Length);
        Buffer.BlockCopy(tail, 0, frame, header.Length, tail.Length);
        return Pm3NgFrame.TryDecodeResponse(frame, out var response) ? response : throw new InvalidDataException($"Invalid PM3 NG response frame: {Convert.ToHexString(frame)}");
    }

    private static async Task<byte[]> ReadExactAsync(Stream stream, int count, CancellationToken cancellationToken)
    {
        var buffer = new byte[count];
        var offset = 0;
        while (offset < count)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(offset, count - offset), cancellationToken).ConfigureAwait(false);
            if (read == 0) return Array.Empty<byte>();
            offset += read;
        }
        return buffer;
    }

    private static int RemainingMilliseconds(long deadline)
    {
        var ticks = deadline - Stopwatch.GetTimestamp();
        if (ticks <= 0) return 0;
        return (int)Math.Min(int.MaxValue, Math.Ceiling(ticks * 1000.0 / Stopwatch.Frequency));
    }

    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _stream?.Dispose();
        _client?.Dispose();
        _stream = null;
        _client = null;
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync().ConfigureAwait(false);
        _ioGate.Dispose();
    }
}
