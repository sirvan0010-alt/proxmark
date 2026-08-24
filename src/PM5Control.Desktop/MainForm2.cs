using Microsoft.Win32;
using PM5Control.Core.Connections;
using PM5Control.Core.Protocols.Pm3;

namespace PM5Control.Desktop;

internal sealed class MainForm2 : Form
{
    private const string ClientVersion = "0.5.0";
    private const string BuildCommit = "pending CI";

    private static readonly Color Bg = Color.FromArgb(18, 20, 24);
    private static readonly Color Panel = Color.FromArgb(26, 29, 35);
    private static readonly Color LogBg = Color.FromArgb(10, 12, 15);
    private static readonly Color TextColor = Color.FromArgb(226, 230, 236);
    private static readonly Color Muted = Color.FromArgb(145, 153, 164);
    private static readonly Color Orange = Color.FromArgb(255, 122, 24);
    private static readonly Color Green = Color.FromArgb(94, 214, 130);
    private static readonly Color Red = Color.FromArgb(230, 90, 80);

    private readonly Label _transport = ValueLabel();
    private readonly Label _usb = ValueLabel();
    private readonly Label _ble = ValueLabel();
    private readonly Label _wifi = ValueLabel();
    private readonly Label _device = ValueLabel();
    private readonly Label _arm = ValueLabel();
    private readonly Label _fpga = ValueLabel();
    private readonly Label _bwm = ValueLabel();
    private readonly Label _build = ValueLabel();
    private readonly TextBox _consoleLog = new();
    private readonly TextBox _diagLog = new();
    private readonly ComboBox _command = new();
    private readonly Button _execute = new();
    private readonly Button _analyze = new();
    private string? _port;
    private bool _busy;

    public MainForm2()
    {
        Text = "Proxmark5 Control Center";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(980, 680);
        Size = new Size(1120, 760);
        BackColor = Bg;
        ForeColor = TextColor;
        Font = new Font("Segoe UI", 10F);
        BuildUi();
        RefreshPorts();
        Log(_diagLog, $"PM5 Control Center v{ClientVersion} started. Read-only UI: no write, reset or flash command is available.");
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = Bg };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(root);

        var header = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, BackColor = Bg, Padding = new Padding(18, 10, 18, 8) };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        var title = new Label { Text = "PROXMARK5  CONTROL CENTER", AutoSize = true, Font = new Font("Segoe UI Black", 18F), ForeColor = Orange };
        var sub = new Label { Text = $"Read-only diagnostic client  ·  v{ClientVersion}  ·  {BuildCommit}", AutoSize = true, ForeColor = Muted, Font = new Font("Consolas", 9F), Location = new Point(20, 45) };
        var left = new Panel { Dock = DockStyle.Fill };
        left.Controls.Add(title); left.Controls.Add(sub);
        var status = new Label { Text = "READ-ONLY", AutoSize = true, BackColor = Green, ForeColor = Color.Black, Font = new Font("Segoe UI Semibold", 9F), Padding = new Padding(10, 5, 10, 5), Anchor = AnchorStyles.Right };
        header.Controls.Add(left, 0, 0); header.Controls.Add(status, 1, 0);
        root.Controls.Add(header, 0, 0);

        var tabs = new TabControl { Dock = DockStyle.Fill, BackColor = Bg, ForeColor = TextColor, Padding = new Point(14, 6) };
        tabs.TabPages.Add(BuildDeviceTab());
        tabs.TabPages.Add(BuildConsoleTab());
        tabs.TabPages.Add(BuildDiagnosticsTab());
        root.Controls.Add(tabs, 0, 1);
    }

    private TabPage BuildDeviceTab()
    {
        var page = Page("DEVICE");
        var grid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 9, Padding = new Padding(20), BackColor = Bg };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 230));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        AddRow(grid, 0, "Transport", _transport);
        AddRow(grid, 1, "USB / Serial", _usb);
        AddRow(grid, 2, "Bluetooth / BLE", _ble);
        AddRow(grid, 3, "Wi-Fi", _wifi);
        AddRow(grid, 4, "Device family", _device);
        AddRow(grid, 5, "ARM firmware", _arm);
        AddRow(grid, 6, "FPGA", _fpga);
        AddRow(grid, 7, "ESP32 / BWM", _bwm);
        AddRow(grid, 8, "Client build", _build);
        _build.Text = $"v{ClientVersion} · {BuildCommit}";
        page.Controls.Add(grid);
        return page;
    }

    private TabPage BuildConsoleTab()
    {
        var page = Page("CONSOLE");
        var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 62, Padding = new Padding(18, 14, 18, 8), BackColor = Bg };
        _command.DropDownStyle = ComboBoxStyle.DropDownList;
        _command.Width = 260;
        _command.Items.AddRange(new object[] { "hw version", "hw status", "hw capabilities", "hw ping" });
        _command.SelectedIndex = 0;
        _execute.Text = "Execute";
        _execute.AutoSize = true;
        _execute.Click += async (_, _) => await ExecuteSelectedAsync();
        _analyze.Text = "Connect / analyze";
        _analyze.AutoSize = true;
        _analyze.Click += async (_, _) => await AnalyzeAsync();
        top.Controls.Add(_command); top.Controls.Add(_execute); top.Controls.Add(_analyze);
        page.Controls.Add(top);
        ConfigureLog(_consoleLog);
        page.Controls.Add(_consoleLog);
        return page;
    }

    private TabPage BuildDiagnosticsTab()
    {
        var page = Page("DIAGNOSTICS");
        var info = new Label { Dock = DockStyle.Top, Height = 42, Text = "Developer view: raw read-only responses, interleaved frames and transport diagnostics. No custom/destructive commands.", Padding = new Padding(18, 12, 18, 4), ForeColor = Muted };
        page.Controls.Add(info);
        ConfigureLog(_diagLog);
        page.Controls.Add(_diagLog);
        return page;
    }

    private static TabPage Page(string text) => new(text) { BackColor = Bg, ForeColor = TextColor };

    private static Label ValueLabel() => new() { Dock = DockStyle.Fill, Text = "UNKNOWN", TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Consolas", 10F, FontStyle.Bold), ForeColor = TextColor };

    private static void AddRow(TableLayoutPanel grid, int row, string name, Label value)
    {
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100F / 9F));
        grid.Controls.Add(new Label { Text = name, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Muted, Font = new Font("Segoe UI Semibold", 10F) }, 0, row);
        grid.Controls.Add(value, 1, row);
    }

    private static void ConfigureLog(TextBox box)
    {
        box.Multiline = true; box.ReadOnly = true; box.ScrollBars = ScrollBars.Vertical; box.Dock = DockStyle.Fill;
        box.BackColor = LogBg; box.ForeColor = Green; box.Font = new Font("Consolas", 9.5F); box.BorderStyle = BorderStyle.FixedSingle;
    }

    private async Task AnalyzeAsync()
    {
        if (!TryGetPort()) return;
        await RunAsync("Connect / analyze", async transport =>
        {
            var inspection = await Pm3ReadOnlyInspector.InspectAsync(transport);
            _transport.Text = "PM3 NG response received";
            _usb.Text = $"Connected · {_port}";
            _device.Text = inspection.Identity.Hardware;
            _arm.Text = inspection.Identity.ArmFirmware;
            _fpga.Text = inspection.Identity.FpgaFirmware;
            _bwm.Text = "Not yet queried";
            _ble.Text = "Unknown — no BLE telemetry on current USB protocol";
            _wifi.Text = "Unknown — no Wi-Fi telemetry on current USB protocol";
            Log(_consoleLog, "Read-only handshake succeeded.");
            Log(_consoleLog, $"ARM: {inspection.Identity.ArmFirmware}");
            Log(_consoleLog, $"FPGA: {inspection.Identity.FpgaFirmware}");
            Log(_consoleLog, $"Hardware: {inspection.Identity.Hardware}");
            Log(_consoleLog, $"Capabilities schema v{inspection.Capabilities.SchemaVersion}; known={inspection.Capabilities.IsKnownSchema}.");
        });
    }

    private async Task ExecuteSelectedAsync()
    {
        if (!TryGetPort()) return;
        var command = _command.SelectedItem?.ToString() ?? "hw version";
        await RunAsync(command, async transport =>
        {
            switch (command)
            {
                case "hw version":
                    var id = await Pm3ReadOnlyInspector.QueryVersionAsync(transport);
                    _device.Text = id.Hardware; _arm.Text = id.ArmFirmware; _fpga.Text = id.FpgaFirmware;
                    Log(_consoleLog, $"ARM: {id.ArmFirmware}"); Log(_consoleLog, $"FPGA: {id.FpgaFirmware}"); Log(_consoleLog, $"Hardware: {id.Hardware}");
                    break;
                case "hw capabilities":
                    var cap = await Pm3ReadOnlyInspector.QueryCapabilitiesAsync(transport);
                    Log(_consoleLog, $"CMD_CAPABILITIES schema v{cap.SchemaVersion}; known={cap.IsKnownSchema}; raw bytes={cap.RawPayload.Length}.");
                    break;
                case "hw status":
                    var status = await Pm3ReadOnlyInspector.QueryStatusAsync(transport);
                    Log(_consoleLog, $"CMD_STATUS: {(status.Success ? "OK" : "FAILED")} (status={status.Status}, reason={status.Reason}, payload={status.PayloadLength} bytes)");
                    foreach (var frame in status.DebugFrames)
                        Log(_diagLog, $"Interleaved 0x{frame.Command:X4}: status={frame.Status}, reason={frame.Reason}, payload={frame.Payload.Length} bytes, raw={Convert.ToHexString(frame.Payload)}");
                    break;
                case "hw ping":
                    var ping = await Pm3ReadOnlyInspector.PingAsync(transport);
                    Log(_consoleLog, $"CMD_PING: {(ping.Success ? "OK" : "FAILED")} (status={ping.Status}, reason={ping.Reason}, payload={ping.PayloadLength} bytes)");
                    break;
            }
        });
    }

    private async Task RunAsync(string label, Func<Pm3SerialTransport, Task> action)
    {
        if (_busy || string.IsNullOrWhiteSpace(_port)) return;
        _busy = true; _execute.Enabled = false; _analyze.Enabled = false;
        Log(_consoleLog, $"--- {label} ---");
        await using var transport = new Pm3SerialTransport(_port);
        try
        {
            await transport.ConnectAsync();
            await action(transport);
            _transport.Text = "USB/Serial transaction OK";
        }
        catch (Exception ex)
        {
            _transport.Text = "Transaction failed";
            Log(_consoleLog, $"{label} failed: {ex.Message}");
        }
        finally { _busy = false; _execute.Enabled = true; _analyze.Enabled = true; }
    }

    private bool TryGetPort()
    {
        RefreshPorts();
        if (!string.IsNullOrWhiteSpace(_port)) return true;
        Log(_consoleLog, "No Windows serial port detected.");
        return false;
    }

    private void RefreshPorts()
    {
        var ports = GetSerialPorts();
        _port = ports.FirstOrDefault(p => p.Equals("COM3", StringComparison.OrdinalIgnoreCase)) ?? ports.FirstOrDefault();
        _usb.Text = _port is null ? "Not detected" : $"Detected · {_port}";
        _transport.Text = _port is null ? "No USB serial transport" : "USB serial endpoint detected";
        _ble.Text = "Unknown — not exposed by current USB protocol";
        _wifi.Text = "Unknown — not exposed by current USB protocol";
    }

    private static IReadOnlyList<string> GetSerialPorts()
    {
        var result = new List<string>();
        using var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DEVICEMAP\SERIALCOMM");
        if (key is null) return result;
        foreach (var name in key.GetValueNames())
            if (key.GetValue(name) is string port && !string.IsNullOrWhiteSpace(port)) result.Add(port);
        return result.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static void Log(TextBox box, string message) => box.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
}
