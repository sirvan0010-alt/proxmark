using System.IO.Ports;
using PM5Control.Core.Protocols.Bwm;

namespace PM5Control.Core.Connections;

/// <summary>
/// Windows USB/UART transport for the PM5 BWM command channel.
/// Opening the port does not assert DTR/RTS. Mutating command authorization
/// remains above this transport layer in the BWM read-only policy.
/// </summary>
public sealed class SerialProxmarkTransport : IProxmarkTransport
{
    private readonly string _portName;
    private readonly int _baudRate;
    private readonly int _readTimeoutMs;
    private SerialPort? _port;

    public SerialProxmarkTransport(string portName, int baudRate = BwmProtocolConstants.DefaultUartBaudRate, int readTimeoutMs = 1500)
    {
        _portName = portName ?? throw new ArgumentNullException(nameof(portName));
        _baudRate = baudRate;
        _readTimeoutMs = readTimeoutMs;
    }

    public string TransportName => $"Serial {_portName} @ {_baudRate}";
    public bool IsConnected => _port?.IsOpen == true;
    public event Action<ReadOnlyMemory<byte>>? DataReceived;

    public Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (IsConnected)
            return Task.CompletedTask;

        var port = new SerialPort(_portName, _baudRate, Parity.None, 8, StopBits.One)
        {
            DtrEnable = false,
            RtsEnable = false,
            Handshake = Handshake.None,
            ReadTimeout = _readTimeoutMs,
            WriteTimeout = _readTimeoutMs,
            ReadBufferSize = 16 * 1024,
            WriteBufferSize = 16 * 1024,
        };

        port.Open();
        _port = port;
        return Task.CompletedTask;
    }

    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _port?.Close();
        _port?.Dispose();
        _port = null;
        return Task.CompletedTask;
    }

    public async Task<byte[]> SendAsync(ReadOnlyMemory<byte> request, CancellationToken cancellationToken = default)
    {
        var port = _port ?? throw new InvalidOperationException("Serial transport is not connected.");
        if (!port.IsOpen)
            throw new InvalidOperationException("Serial port is not open.");

        // Ignore stale input so a previous broadcast/log cannot be mistaken
        // for the response to this request.
        port.DiscardInBuffer();
        await port.BaseStream.WriteAsync(request, cancellationToken).ConfigureAwait(false);
        await port.BaseStream.FlushAsync(cancellationToken).ConfigureAwait(false);

        while (true)
        {
            var header = await ReadExactAsync(port.BaseStream, BwmProtocolConstants.HeaderSize, cancellationToken).ConfigureAwait(false);
            ushort length = (ushort)(header[4] | (header[5] << 8));

            if (length > 8192)
                throw new InvalidDataException($"BWM frame payload is unexpectedly large: {length} bytes.");

            var tail = await ReadExactAsync(port.BaseStream, length + BwmProtocolConstants.CrcSize, cancellationToken).ConfigureAwait(false);
            var frame = new byte[header.Length + tail.Length];
            Buffer.BlockCopy(header, 0, frame, 0, header.Length);
            Buffer.BlockCopy(tail, 0, frame, header.Length, tail.Length);

            if (!BwmFrameCodec.TryDecode(frame, out var decoded) || decoded is null)
                throw new InvalidDataException("Received bytes are not a valid BWM frame (magic/length/CRC mismatch).");

            if (decoded.Kind == BwmFrameKind.Broadcast)
            {
                DataReceived?.Invoke(frame);
                continue;
            }

            return frame;
        }
    }

    private static async Task<byte[]> ReadExactAsync(Stream stream, int count, CancellationToken cancellationToken)
    {
        var buffer = new byte[count];
        var offset = 0;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(2));

        while (offset < count)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(offset, count - offset), timeout.Token).ConfigureAwait(false);
            if (read == 0)
                throw new EndOfStreamException("PM5 serial connection closed while waiting for a BWM response.");
            offset += read;
        }

        return buffer;
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync().ConfigureAwait(false);
    }
}
