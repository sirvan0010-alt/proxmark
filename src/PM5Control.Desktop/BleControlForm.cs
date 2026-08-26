using System.Text;
using PM5Control.Core.Protocols.Pm3;

namespace PM5Control.Desktop;

internal sealed class BleControlForm : Form
{
    private readonly ComboBox _devices = new() { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList, Height = 30 };
    private readonly Button _refresh = new() { Text = "Refresh BLE", AutoSize = true };
    private readonly Button _connect = new() { Text = "Connect / analyze", AutoSize = true };
    private readonly Button _disconnect = new() { Text = "Disconnect", AutoSize = true, Enabled = false };
    private readonly Button _status = new() { Text = "STATUS", AutoSize = true, Enabled = false };
    private readonly Button _version = new() { Text = "VERSION", AutoSize = true, Enabled = false };
    private readonly Button _capabilities = new() { Text = "CAPABILITIES", AutoSize = true, Enabled = false };
    private readonly Button _ping = new() { Text = "PING", AutoSize = true, Enabled = false };
    private readonly TextBox _log = new() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Both, Dock = DockStyle.Fill, Font = new Font("Consolas", 9.5F) };
    private WindowsBleProxmarkTransport? _transport;

    public BleControlForm()
    {
        Text = "PM5 Control Center · Bluetooth LE";
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(1000, 680);
        BackColor = Color.FromArgb(18, 20, 24);
        ForeColor = Color.White;

        var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 88, Padding = new Padding(12), BackColor = Color.FromArgb(18, 20, 24) };
        top.Controls.Add(_devices);
        _devices.Width = 420;
        top.Controls.Add(_refresh);
        top.Controls.Add(_connect);
        top.Controls.Add(_disconnect);
        top.Controls.Add(_version);
        top.Controls.Add(_capabilities);
        top.Controls.Add(_status);
        top.Controls.Add(_ping);
        Controls.Add(_log);
        Controls.Add(top);

        _refresh.Click += async (_, _) => await RefreshAsync();
        _connect.Click += async (_, _) => await ConnectAsync();
        _disconnect.Click += async (_, _) => await DisconnectAsync();
        _version.Click += async (_, _) => await ProbeAsync(Pm3CommandCode.Version, "CMD_VERSION");
        _capabilities.Click += async (_, _) => await ProbeAsync(Pm3CommandCode.Capabilities, "CMD_CAPABILITIES");
        _status.Click += async (_, _) => await ProbeAsync(Pm3CommandCode.Status, "CMD_STATUS");
        _ping.Click += async (_, _) => await ProbeAsync(Pm3CommandCode.Ping, "CMD_PING");
        Shown += async (_, _) => await RefreshAsync();
        FormClosed += async (_, _) => await DisconnectAsync();
    }

    private async Task RefreshAsync()
    {
        try
        {
            _devices.Items.Clear();
            var candidates = await WindowsBleProxmarkTransport.DiscoverAsync();
            foreach (var candidate in candidates) _devices.Items.Add(candidate);
            if (_devices.Items.Count > 0) _devices.SelectedIndex = 0;
            Log($"BLE discovery: {candidates.Count} PM5-like device(s) found.");
            foreach (var candidate in candidates) Log($"  {candidate}");
            if (candidates.Count == 0) Log("No cached/discovered device named Proxmark/PM5. Windows Bluetooth discovery may need to run first.");
        }
        catch (Exception ex) { Log($"BLE discovery failed: {ex.Message}"); }
    }

    private async Task ConnectAsync()
    {
        if (_devices.SelectedItem is not BleDeviceCandidate candidate)
        {
            Log("Select a PM5 BLE device first.");
            return;
        }
        await DisconnectAsync();
        try
        {
            _transport = new WindowsBleProxmarkTransport(candidate.BluetoothAddress);
            await _transport.ConnectAsync();
            _version.Enabled = _capabilities.Enabled = _status.Enabled = _ping.Enabled = true;
            _connect.Enabled = false;
            _disconnect.Enabled = true;
            Log($"BLE connected: {candidate.Name} / 0x{candidate.BluetoothAddress:X12}");
            await ProbeAsync(Pm3CommandCode.Version, "CMD_VERSION");
            await ProbeAsync(Pm3CommandCode.Capabilities, "CMD_CAPABILITIES");
        }
        catch (Exception ex)
        {
            Log($"BLE connect failed: {ex.Message}");
            await DisconnectAsync();
        }
    }

    private async Task ProbeAsync(ushort command, string name)
    {
        var transport = _transport;
        if (transport is null || !transport.IsConnected) return;
        try
        {
            var exchange = await transport.SendReadOnlyAsync(command);
            var response = exchange.Response;
            Log($"{name}: status={response.Status}, reason={response.Reason}, payload={response.Payload.Length} bytes");
            Log($"  raw: {Convert.ToHexString(response.RawFrame)}");
            foreach (var debug in exchange.DebugFrames)
                Log($"  debug 0x{debug.Command:X4}: {Convert.ToHexString(debug.Payload)}");

            if (command == Pm3CommandCode.Version)
            {
                var text = Encoding.UTF8.GetString(response.Payload).Replace("\0", " ").Trim();
                Log($"  version text: {text}");
            }
            else if (command == Pm3CommandCode.Capabilities)
            {
                var c = Pm3ReadOnlyInspector.DecodeCapabilities(response.Payload);
                Log($"  capabilities schema={c.SchemaVersion}; known={c.IsKnownSchema}; USB={c.ViaUsb}; FPC={c.ViaFpc}; BigBuf={c.BigBufferSize}; features={string.Join(", ", c.EnabledFeatures)}");
            }
            else if (command == Pm3CommandCode.Status)
            {
                foreach (var frame in exchange.DebugFrames.Where(x => x.Command == Pm3CommandCode.DebugPrintString))
                {
                    var text = Encoding.UTF8.GetString(frame.Payload).Replace("\0", " ").Trim();
                    if (!string.IsNullOrWhiteSpace(text)) Log($"  hw status: {text}");
                }
            }
        }
        catch (Exception ex) { Log($"{name} failed: {ex.Message}"); }
    }

    private async Task DisconnectAsync()
    {
        if (_transport is not null)
        {
            try { await _transport.DisposeAsync(); } catch { }
            _transport = null;
        }
        _version.Enabled = _capabilities.Enabled = _status.Enabled = _ping.Enabled = false;
        _connect.Enabled = true;
        _disconnect.Enabled = false;
    }

    private void Log(string text) => _log.AppendText($"[{DateTime.Now:HH:mm:ss}] {text}{Environment.NewLine}");
}
