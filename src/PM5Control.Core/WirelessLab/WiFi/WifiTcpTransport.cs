using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using PM5Control.Core.WirelessLab;

namespace PM5Control.Core.WirelessLab.WiFi;

public sealed class WifiTcpTransport : IAsyncDisposable
{
    private readonly string _host;
    private readonly int _port;
    private readonly TimeSpan _connectTimeout;
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly SemaphoreSlim _receiveLock = new(1, 1);
    private readonly List<byte> _rxBuffer = new(1024);
    private TcpClient? _client;
    private NetworkStream? _stream;
    private bool _disposed;

    public WifiTcpTransport(string host = "192.168.4.1", int port = 7901, int connectTimeoutMs = 3000)
    {
        if (string.IsNullOrWhiteSpace(host)) throw new ArgumentException("Host is required.", nameof(host));
        if (port is < 1 or > 65535) throw new ArgumentOutOfRangeException(nameof(port));
        if (connectTimeoutMs <= 0) throw new ArgumentOutOfRangeException(nameof(connectTimeoutMs));
        _host = host; _port = port; _connectTimeout = TimeSpan.FromMilliseconds(connectTimeoutMs);
    }

    public string Host => _host;
    public int Port => _port;
    public bool IsConnected => _client?.Connected == true && _stream != null;

    public async Task<bool> OpenAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed(); if (IsConnected) return true; CloseInternal();
        var client = new TcpClient();
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(_connectTimeout);
        try {
            await client.ConnectAsync(_host, _port, timeoutCts.Token).ConfigureAwait(false);
            _client = client; _stream = client.GetStream(); _rxBuffer.Clear(); return true;
        } catch (OperationCanceledException) when (!ct.IsCancellationRequested) { client.Dispose(); return false; }
          catch (SocketException) { client.Dispose(); return false; }
          catch { client.Dispose(); throw; }
    }

    public Task CloseAsync(CancellationToken ct = default) { ct.ThrowIfCancellationRequested(); CloseInternal(); return Task.CompletedTask; }

    public async Task SendAsync(byte command, ReadOnlyMemory<byte> payload, CancellationToken ct = default)
    {
        ThrowIfDisposed(); EnsureConnected();
        var frame = WirelessProtocol.BuildFrame(command, payload.Span);
        await _sendLock.WaitAsync(ct).ConfigureAwait(false);
        try { EnsureConnected(); await _stream!.WriteAsync(frame, ct).ConfigureAwait(false); await _stream.FlushAsync(ct).ConfigureAwait(false); }
        finally { _sendLock.Release(); }
    }

    public async Task<ParsedFrame> ReadFrameAsync(TimeSpan timeout, CancellationToken ct = default)
    {
        ThrowIfDisposed(); EnsureConnected(); if (timeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeout));
        await _receiveLock.WaitAsync(ct).ConfigureAwait(false);
        try {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct); timeoutCts.CancelAfter(timeout);
            var chunk = new byte[512];
            while (true) {
                if (TryTakeFrame(out var frame)) return frame;
                int read;
                try { read = await _stream!.ReadAsync(chunk, timeoutCts.Token).ConfigureAwait(false); }
                catch (OperationCanceledException) when (!ct.IsCancellationRequested) { throw new TimeoutException($"No complete BWM frame received within {timeout.TotalMilliseconds:0} ms."); }
                if (read == 0) { CloseInternal(); throw new IOException("BWM TCP endpoint closed the connection."); }
                for (var i = 0; i < read; i++) _rxBuffer.Add(chunk[i]);
                if (_rxBuffer.Count > WirelessProtocol.MaxFrameLength * 4) _rxBuffer.RemoveRange(0, _rxBuffer.Count - WirelessProtocol.MaxFrameLength);
            }
        } finally { _receiveLock.Release(); }
    }

    private bool TryTakeFrame(out ParsedFrame frame)
    {
        frame = null!; if (_rxBuffer.Count == 0) return false;
        var span = CollectionsMarshal.AsSpan(_rxBuffer);
        if (!WirelessProtocol.TryParseFrame(span, out var parsed, out var consumed)) { if (consumed > 0) _rxBuffer.RemoveRange(0, consumed); return false; }
        _rxBuffer.RemoveRange(0, consumed); frame = parsed!; return true;
    }

    private void EnsureConnected() { if (!IsConnected) throw new InvalidOperationException("Wi-Fi TCP transport is not connected."); }
    private void CloseInternal() { try { _stream?.Dispose(); } catch { } _stream = null; try { _client?.Dispose(); } catch { } _client = null; _rxBuffer.Clear(); }
    private void ThrowIfDisposed() { if (_disposed) throw new ObjectDisposedException(nameof(WifiTcpTransport)); }
    public ValueTask DisposeAsync() { if (_disposed) return ValueTask.CompletedTask; _disposed = true; CloseInternal(); _sendLock.Dispose(); _receiveLock.Dispose(); return ValueTask.CompletedTask; }
}