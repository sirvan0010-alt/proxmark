using System.IO.Ports;
using PM5Control.Core.Protocols.Pm3;

namespace PM5Control.Core.Connections;

public sealed class Pm3SerialTransport : IAsyncDisposable
{
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

    public async Task<Pm3NgResponse> SendReadOnlyAsync(ushort command, CancellationToken cancellationToken = default)
    {
        if (!IsConnected) throw new InvalidOperationException("PM3 serial transport is not connected.");
        var port = _port!;
        port.DiscardInBuffer();
        var request = Pm3NgFrame.EncodeCommand(command);
        await port.BaseStream.WriteAsync(request, cancellationToken).ConfigureAwait(false);
        await port.BaseStream.FlushAsync(cancellationToken).ConfigureAwait(false);

        var header = await ReadExactAsync(port.BaseStream, Pm3NgFrame.ResponseHeaderSize, cancellationToken).ConfigureAwait(false);
        if (!Pm3NgFrame.TryGetResponseLength(header, out var totalLength))
            throw new InvalidDataException("PM3 endpoint did not return a valid NG response header.");

        var tail = await ReadExactAsync(port.BaseStream, totalLength - header.Length, cancellationToken).ConfigureAwait(false);
        var frame = new byte[totalLength];
        Buffer.BlockCopy(header, 0, frame, 0, header.Length);
        Buffer.BlockCopy(tail, 0, frame, header.Length, tail.Length);
        if (!Pm3NgFrame.TryDecodeResponse(frame, out var response) || response is null)
            throw new InvalidDataException("PM3 endpoint returned an invalid NG response frame.");
        if (response.Command != command)
            throw new InvalidDataException($"PM3 response command mismatch: expected 0x{command:X4}, got 0x{response.Command:X4}.");
        return response;
    }

    private async Task<byte[]> ReadExactAsync(Stream stream, int count, CancellationToken cancellationToken)
    {
        var buffer = new byte[count];
        var offset = 0;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_timeoutMs);
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
