using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PM5Control.Core.WirelessLab;

namespace PM5Control.Core.WirelessLab.WiFi;

public sealed class WiFiCapabilityAgent : IDisposable
{
    private readonly SerialPort _port;
    private readonly object _sync = new();
    private CancellationTokenSource? _connectionCts;
    private Task? _reader;
    private bool _disposed;
    public WiFiCapabilityMatrix CapabilityMatrix { get; } = new();
    public bool IsConnected => _port.IsOpen;
    public string? ActiveSessionId { get; private set; }
    public void BindValidationSession(string sessionId) { if (string.IsNullOrWhiteSpace(sessionId)) throw new ArgumentException("Session ID is required.", nameof(sessionId)); ActiveSessionId = sessionId; }
    public void ClearValidationSession() => ActiveSessionId = null;
    public event EventHandler<WiFiCapabilityEventArgs>? CapabilityTested;
    public event EventHandler<WiFiScanResultEventArgs>? ScanResultReceived;
    public event EventHandler<WirelessErrorEventArgs>? ErrorReceived;
    public event EventHandler<WirelessStatusEventArgs>? StatusReceived;

    public WiFiCapabilityAgent(string portName, int baudRate = 115200) { _port = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One) { ReadTimeout = 100, WriteTimeout = 1000, ReadBufferSize = 4096 }; }

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();
        lock (_sync) { if (_port.IsOpen) return; _connectionCts = new CancellationTokenSource(); _port.Open(); _reader = Task.Run(() => ReaderLoop(_connectionCts.Token), _connectionCts.Token); }
        await Task.Delay(100, ct).ConfigureAwait(false);
    }
    public async Task DisconnectAsync()
    {
        CancellationTokenSource? cts; Task? reader;
        lock (_sync) { cts = _connectionCts; reader = _reader; _connectionCts = null; _reader = null; }
        cts?.Cancel();
        if (reader != null) { try { await reader.WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false); } catch { } }
        lock (_sync) { if (_port.IsOpen) _port.Close(); }
        cts?.Dispose();
        ActiveSessionId = null;
    }
    public Task RunCapabilityDiscoveryAsync(CancellationToken ct = default) => SendAsync(WirelessProtocol.CmdGetCapabilities, ReadOnlyMemory<byte>.Empty, ct);
    public Task RunSingleTestAsync(byte capId, CancellationToken ct = default) => SendAsync(WirelessProtocol.CmdRunTest, new byte[] { capId }, ct);
    public Task StartScanAsync(CancellationToken ct = default) => SendAsync(WirelessProtocol.CmdStartScan, ReadOnlyMemory<byte>.Empty, ct);
    public Task StartSnifferAsync(byte channel, CancellationToken ct = default) => SendAsync(WirelessProtocol.CmdStartSniffer, new byte[] { channel }, ct);
    public Task StopSnifferAsync(CancellationToken ct = default) => SendAsync(WirelessProtocol.CmdStopSniffer, ReadOnlyMemory<byte>.Empty, ct);
    public Task GetStatusAsync(CancellationToken ct = default) => SendAsync(WirelessProtocol.CmdGetStatus, ReadOnlyMemory<byte>.Empty, ct);
    public async Task StartSoftApAsync(string ssid, byte channel, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(ssid)) throw new ArgumentException("SSID is required.", nameof(ssid));
        var payload = new byte[33]; int count = Math.Min(ssid.Length, 32); Encoding.ASCII.GetBytes(ssid.AsSpan(0, count), payload.AsSpan(0, count)); payload[32] = channel;
        await SendAsync(WirelessProtocol.CmdStartSoftAp, payload, ct).ConfigureAwait(false);
    }
    public Task StopSoftApAsync(CancellationToken ct = default) => SendAsync(WirelessProtocol.CmdStopSoftAp, ReadOnlyMemory<byte>.Empty, ct);

    private Task SendAsync(byte command, ReadOnlyMemory<byte> payload, CancellationToken ct)
    {
        ThrowIfDisposed(); ct.ThrowIfCancellationRequested(); var frame = WirelessProtocol.BuildFrame(command, payload.Span);
        lock (_sync) { if (!_port.IsOpen) throw new InvalidOperationException("Serial port is not connected."); ct.ThrowIfCancellationRequested(); _port.Write(frame, 0, frame.Length); }
        return Task.CompletedTask;
    }
    private void ReaderLoop(CancellationToken ct)
    {
        var buffer = new List<byte>(512);
        while (!ct.IsCancellationRequested)
        {
            try
            {
                int available = _port.BytesToRead;
                if (available > 0) { var chunk = new byte[Math.Min(available, 256)]; int read = _port.Read(chunk, 0, chunk.Length); for (int i=0;i<read;i++) buffer.Add(chunk[i]); }
                else Thread.Sleep(5);
                while (buffer.Count > 0)
                {
                    var span = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(buffer);
                    if (WirelessProtocol.TryParseFrame(span, out var frame, out var consumed)) { buffer.RemoveRange(0, consumed); if (frame != null) ProcessFrame(frame); }
                    else if (consumed > 0) buffer.RemoveRange(0, consumed); else break;
                }
                if (buffer.Count > WirelessProtocol.MaxFrameLength * 4) buffer.RemoveRange(0, buffer.Count - WirelessProtocol.MaxFrameLength);
            }
            catch (TimeoutException) { }
            catch (InvalidOperationException) { break; }
            catch (Exception ex) { ErrorReceived?.Invoke(this, new WirelessErrorEventArgs(ex.Message)); }
        }
    }
    private void ProcessFrame(ParsedFrame frame)
    {
        switch (frame.Command)
        {
            case WirelessProtocol.EvtCapabilityResult when frame.Payload.Length >= 3:
            case WirelessProtocol.EvtTestResult when frame.Payload.Length >= 3:
                var capId = frame.Payload[0]; var result = frame.Payload[1] switch { WirelessProtocol.ResultPass => CapabilityTestResult.Pass, WirelessProtocol.ResultFail => CapabilityTestResult.Fail, WirelessProtocol.ResultError => CapabilityTestResult.Error, WirelessProtocol.ResultTimeout => CapabilityTestResult.Timeout, _ => CapabilityTestResult.NotRun };
                byte? error = result == CapabilityTestResult.Error ? frame.Payload[2] : null;
                CapabilityMatrix.RecordTestResult(capId, result, ActiveSessionId ?? "UNBOUND", error, Convert.ToHexString(frame.Payload));
                CapabilityTested?.Invoke(this, new WiFiCapabilityEventArgs(capId, result, error)); break;
            case WirelessProtocol.EvtScanResult when frame.Payload.Length >= 43:
                var ssid = Encoding.ASCII.GetString(frame.Payload, 1, 32).TrimEnd('\0'); var bssid = new byte[6]; Array.Copy(frame.Payload, 33, bssid, 0, 6);
                ScanResultReceived?.Invoke(this, new WiFiScanResultEventArgs(ssid, BitConverter.ToString(bssid).Replace('-', ':'), frame.Payload[39], (sbyte)frame.Payload[40], frame.Payload[41], frame.Payload[42])); break;
            case WirelessProtocol.EvtStatus when frame.Payload.Length >= 2: StatusReceived?.Invoke(this, new WirelessStatusEventArgs(frame.Payload[0] != 0, frame.Payload[1] != 0)); break;
            case WirelessProtocol.EvtError when frame.Payload.Length > 0: ErrorReceived?.Invoke(this, new WirelessErrorEventArgs($"BWM Error: 0x{frame.Payload[0]:X2}")); break;
        }
    }
    private void ThrowIfDisposed(){if(_disposed)throw new ObjectDisposedException(nameof(WiFiCapabilityAgent));}
    public void Dispose(){if(_disposed)return;_disposed=true;DisconnectAsync().GetAwaiter().GetResult();_port.Dispose();}
}

public sealed class WiFiCapabilityEventArgs : EventArgs { public byte CapabilityId{get;} public CapabilityTestResult Result{get;} public byte? ErrorCode{get;} public WiFiCapabilityEventArgs(byte id,CapabilityTestResult result,byte? error){CapabilityId=id;Result=result;ErrorCode=error;} }
public sealed class WiFiScanResultEventArgs : EventArgs { public string SSID{get;} public string BSSID{get;} public byte Channel{get;} public sbyte RSSI{get;} public byte Authentication{get;} public byte PMF{get;} public WiFiScanResultEventArgs(string ssid,string bssid,byte channel,sbyte rssi,byte auth,byte pmf){SSID=ssid;BSSID=bssid;Channel=channel;RSSI=rssi;Authentication=auth;PMF=pmf;} }
public sealed class WirelessErrorEventArgs : EventArgs { public string Message{get;} public WirelessErrorEventArgs(string message){Message=message;} }
public sealed class WirelessStatusEventArgs : EventArgs { public bool SoftAPRunning{get;} public bool SnifferRunning{get;} public WirelessStatusEventArgs(bool softAp,bool sniffer){SoftAPRunning=softAp;SnifferRunning=sniffer;} }
