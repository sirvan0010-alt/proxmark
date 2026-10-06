using Microsoft.Win32;
using PM5Control.Core.Connections;
using PM5Control.Core.Protocols.Bwm;
using PM5Control.Core.Protocols.Pm3;

namespace PM5Control.Desktop;

/// <summary>
/// Proxmark5 Control Center main window.
/// Visual theme: dark case / orange accent, matching the physical PM5 hardware colourway.
/// All toolbar actions call only whitelisted read-only PM3 NG commands
/// (CMD_VERSION, CMD_CAPABILITIES, CMD_STATUS, CMD_PING) via Pm3ReadOnlyInspector.
/// "Help" is a local, offline command reference and never touches the device.
/// </summary>
internal sealed class MainForm : Form
{
    private static readonly Color BgPanel = Color.FromArgb(18, 20, 24);
    private static readonly Color BgControl = Color.FromArgb(26, 29, 35);
    private static readonly Color BgLog = Color.FromArgb(12, 13, 16);
    private static readonly Color FgText = Color.FromArgb(226, 230, 236);
    private static readonly Color FgMuted = Color.FromArgb(142, 150, 160);
    private static readonly Color AccentOrange = Color.FromArgb(255, 122, 24);
    private static readonly Color AccentOrangeDim = Color.FromArgb(120, 62, 20);
    private static readonly Color AccentGreen = Color.FromArgb(94, 214, 130);
    private static readonly Color AccentRed = Color.FromArgb(230, 90, 80);

    private static readonly Font FontTitle = new("Segoe UI Black", 19F, FontStyle.Regular);
    private static readonly Font FontSubtitle = new("Consolas", 9.5F, FontStyle.Regular);
    private static readonly Font FontLabel = new("Segoe UI Semibold", 9.5F);
    private static readonly Font FontValue = new("Consolas", 10F, FontStyle.Bold);
    private static readonly Font FontLog = new("Consolas", 9.5F);
    private static readonly Font FontToolbar = new("Segoe UI Semibold", 9.5F);

    private readonly Label _connectionValue = new();
    private readonly Label _portValue = new();
    private readonly Label _hardwareValue = new();
    private readonly Label _firmwareValue = new();
    private readonly Label _fpgaValue = new();
    private readonly Label _bwmValue = new();
    private readonly Label _statusPill = new();
    private readonly TextBox _log = new();
    private readonly System.Windows.Forms.Timer _timer = new();
    private readonly ToolStripComboBox _portSelector = new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        Width = 90
    };
    private readonly AppSettings _settings = AppSettings.Load();

    private ToolStripButton _btnConnect = null!;
    private ToolStripButton _btnVersion = null!;
    private ToolStripButton _btnStatus = null!;
    private ToolStripButton _btnPing = null!;
    private ToolStripButton _btnCapabilities = null!;
    private ToolStripButton _btnFullDiag = null!;
    private ToolStripButton _btnHelp = null!;
    private ToolStripButton _btnRefreshPorts = null!;
    private ToolStripButton _btnDeveloper = null!;
    private ToolStripButton _btnExport = null!;
    private bool _developerMode;
    private bool _operationInProgress;
    private bool _suppressPortSelectionEvent;

    public MainForm()
    {
        Text = "Proxmark5 Control Center";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(980, 660);
        Size = new Size(1080, 720);
        Font = new Font("Segoe UI", 10F);
        BackColor = BgPanel;
        ForeColor = FgText;

        BuildUi();
        RefreshDeviceState();
        SetPill("READ-ONLY MODE", AccentGreen);
        Log("PM5 Control Center started. No write, reset or flash command is ever issued from this UI.");

        _timer.Interval = 2000;
        _timer.Tick += (_, _) => RefreshDeviceState(false);
        _timer.Start();
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = BgPanel,
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 210));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(root);

        root.Controls.Add(BuildToolbar(), 0, 0);
        root.Controls.Add(BuildHeader(), 0, 1);
        root.Controls.Add(BuildStatusGrid(), 0, 2);
        root.Controls.Add(BuildLogPanel(), 0, 3);
    }

    private ToolStrip BuildToolbar()
    {
        var strip = new ToolStrip
        {
            Dock = DockStyle.Top,
            GripStyle = ToolStripGripStyle.Hidden,
            RenderMode = ToolStripRenderMode.System,
            BackColor = BgControl,
            ForeColor = FgText,
            Font = FontToolbar,
            Padding = new Padding(6, 4, 6, 4),
            ImageScalingSize = new Size(1, 1),
        };
        strip.Renderer = new Pm5ToolbarRenderer();

        _btnConnect = MakeToolButton("Connect / analyze", async () => await ConnectReadOnlyAsync());
        _btnVersion = MakeToolButton("hw version", async () => await RunSingleCommandAsync("hw version", RunVersionAsync));
        _btnStatus = MakeToolButton("hw status", async () => await RunSingleCommandAsync("hw status", RunStatusAsync));
        _btnPing = MakeToolButton("hw ping", async () => await RunSingleCommandAsync("hw ping", RunPingAsync));
        _btnCapabilities = MakeToolButton("hw capabilities", async () => await RunSingleCommandAsync("hw capabilities", RunCapabilitiesAsync));
        _btnFullDiag = MakeToolButton("Full diagnostic (hw info)", async () => await RunFullDiagnosticAsync());
        _btnHelp = MakeToolButton("help", ShowHelp);
        _btnDeveloper = new ToolStripButton("Developer mode") { CheckOnClick = true, DisplayStyle = ToolStripItemDisplayStyle.Text };
        _btnDeveloper.CheckedChanged += (_, _) =>
        {
            _developerMode = _btnDeveloper.Checked;
            Log(_developerMode ? "Developer mode enabled: raw read-only responses will be logged." : "Developer mode disabled.");
        };
        _btnExport = MakeToolButton("Export report", ExportReport);
        _btnRefreshPorts = MakeToolButton("Refresh ports", () => RefreshDeviceState());

        _portSelector.ForeColor = Color.Black;
        _portSelector.Font = FontToolbar;
        _portSelector.SelectedIndexChanged += (_, _) => OnPortSelectorChanged();

        strip.Items.Add(_btnConnect);
        strip.Items.Add(new ToolStripLabel("Port:") { ForeColor = FgText, Font = FontToolbar, Margin = new Padding(8, 2, 2, 2) });
        strip.Items.Add(_portSelector);
        strip.Items.Add(new ToolStripSeparator());
        strip.Items.Add(_btnVersion);
        strip.Items.Add(_btnStatus);
        strip.Items.Add(_btnPing);
        strip.Items.Add(_btnCapabilities);
        strip.Items.Add(new ToolStripSeparator());
        strip.Items.Add(_btnFullDiag);
        strip.Items.Add(_btnHelp);
        strip.Items.Add(_btnDeveloper);
        strip.Items.Add(_btnExport);
        strip.Items.Add(new ToolStripSeparator());
        strip.Items.Add(_btnRefreshPorts);
        return strip;
    }

    private ToolStripButton MakeToolButton(string text, Action onClick)
        => MakeToolButton(text, () => { onClick(); return Task.CompletedTask; });

    private ToolStripButton MakeToolButton(string text, Func<Task> onClick)
    {
        var button = new ToolStripButton(text)
        {
            DisplayStyle = ToolStripItemDisplayStyle.Text,
            ForeColor = FgText,
            Font = FontToolbar,
            Margin = new Padding(2, 2, 2, 2),
            AutoSize = true,
        };
        button.Click += async (_, _) =>
        {
            try { await onClick(); }
            catch (Exception ex) { Log($"UI error: {ex.Message}"); }
        };
        return button;
    }

    private Control BuildHeader()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(18, 14, 18, 6),
            BackColor = BgPanel,
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var titleBox = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, BackColor = BgPanel };
        var title = new Label { Text = "PROXMARK5", AutoSize = true, Font = FontTitle, ForeColor = AccentOrange };
        var subtitle = new Label
        {
            Text = "CONTROL CENTER  ·  read-only BWM / ESP32 diagnostic inspector",
            AutoSize = true,
            Font = FontSubtitle,
            ForeColor = FgMuted,
        };
        titleBox.Controls.Add(title);
        titleBox.Controls.Add(subtitle);

        _statusPill.AutoSize = true;
        _statusPill.Font = new Font("Segoe UI Semibold", 9.5F);
        _statusPill.Padding = new Padding(10, 4, 10, 4);
        _statusPill.Anchor = AnchorStyles.Right;
        _statusPill.BackColor = AccentGreen;
        _statusPill.ForeColor = Color.Black;

        panel.Controls.Add(titleBox, 0, 0);
        panel.Controls.Add(_statusPill, 1, 0);
        return panel;
    }

    private Control BuildStatusGrid()
    {
        var wrapper = new Panel { Dock = DockStyle.Fill, Padding = new Padding(18, 6, 18, 6), BackColor = BgPanel };
        var status = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 6,
            BackColor = BgControl,
            Padding = new Padding(14),
        };
        status.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
        status.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        wrapper.Controls.Add(status);

        AddStatus(status, 0, "Connection", _connectionValue);
        AddStatus(status, 1, "USB / Serial port", _portValue);
        AddStatus(status, 2, "Hardware", _hardwareValue);
        AddStatus(status, 3, "ARM firmware", _firmwareValue);
        AddStatus(status, 4, "FPGA", _fpgaValue);
        AddStatus(status, 5, "ESP32 / BWM", _bwmValue);
        return wrapper;
    }

    private Control BuildLogPanel()
    {
        var wrapper = new Panel { Dock = DockStyle.Fill, Padding = new Padding(18, 6, 18, 14), BackColor = BgPanel };
        _log.Multiline = true;
        _log.ReadOnly = true;
        _log.ScrollBars = ScrollBars.Vertical;
        _log.Dock = DockStyle.Fill;
        _log.Font = FontLog;
        _log.BackColor = BgLog;
        _log.ForeColor = AccentGreen;
        _log.BorderStyle = BorderStyle.FixedSingle;
        wrapper.Controls.Add(_log);
        return wrapper;
    }

    private void AddStatus(TableLayoutPanel panel, int row, string caption, Label value)
    {
        var label = new Label
        {
            Text = caption,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = FontLabel,
            ForeColor = FgMuted,
        };
        value.Text = "UNKNOWN";
        value.Dock = DockStyle.Fill;
        value.TextAlign = ContentAlignment.MiddleLeft;
        value.Font = FontValue;
        value.ForeColor = FgText;
        panel.Controls.Add(label, 0, row);
        panel.Controls.Add(value, 1, row);
    }

    private void SetPill(string text, Color color)
    {
        _statusPill.Text = text;
        _statusPill.BackColor = color;
    }

    private void RefreshDeviceState(bool logChanges = true)
    {
        var selection = SerialPortSelection.Choose(GetSerialPorts(), _settings.LastSerialPort);

        _suppressPortSelectionEvent = true;
        try
        {
            _portSelector.Items.Clear();
            foreach (var candidate in selection.Candidates)
                _portSelector.Items.Add(candidate);

            if (selection.DefaultPort is not null)
                _portSelector.SelectedItem = selection.DefaultPort;
        }
        finally
        {
            _suppressPortSelectionEvent = false;
        }

        if (selection.DefaultPort is null)
        {
            _connectionValue.Text = "Not detected";
            _portValue.Text = "No serial ports detected";
            SetToolbarDeviceButtonsEnabled(false);
            SetUnknownHardware();
            if (logChanges) Log(selection.Reason);
            return;
        }

        _connectionValue.Text = "Device transport detected";
        _portValue.Text = selection.DefaultPort;
        SetToolbarDeviceButtonsEnabled(!_operationInProgress);
        if (logChanges) Log(selection.Reason);
    }

    private void OnPortSelectorChanged()
    {
        if (_suppressPortSelectionEvent) return;
        if (_portSelector.SelectedItem is not string port || string.IsNullOrWhiteSpace(port)) return;

        _portValue.Text = port;
        _settings.LastSerialPort = port;
        _settings.Save();
        Log($"Port manually selected: {port}.");
        SetToolbarDeviceButtonsEnabled(!_operationInProgress);
    }

    private void SetToolbarDeviceButtonsEnabled(bool enabled)
    {
        _btnConnect.Enabled = enabled;
        _btnVersion.Enabled = enabled;
        _btnStatus.Enabled = enabled;
        _btnPing.Enabled = enabled;
        _btnCapabilities.Enabled = enabled;
        _btnFullDiag.Enabled = enabled;
        _portSelector.Enabled = enabled && _portSelector.Items.Count > 0;
    }

    private async Task ConnectReadOnlyAsync()
    {
        var portName = _portValue.Text;
        if (string.IsNullOrWhiteSpace(portName) || portName.StartsWith("No ", StringComparison.OrdinalIgnoreCase)) return;

        _operationInProgress = true;
        SetToolbarDeviceButtonsEnabled(false);
        _connectionValue.Text = "Analyzing PM3/PM5 read-only protocol...";
        SetPill("QUERYING…", AccentOrange);
        Log($"Selected {portName}.");
        Log("Sending only CMD_VERSION (0x0107) and CMD_CAPABILITIES (0x0112).");
        Log("No firmware write, reset, flash or destructive command is authorized by this path.");

        await using var transport = new Pm3SerialTransport(portName);
        try
        {
            await transport.ConnectAsync();
            Log("Serial endpoint opened successfully; DTR/RTS disabled.");
            var inspection = await Pm3ReadOnlyInspector.InspectAsync(transport);
            var identity = inspection.Identity;
            _connectionValue.Text = "PM3/PM5 NG response received";
            _hardwareValue.Text = identity.Hardware;
            _firmwareValue.Text = identity.ArmFirmware;
            _fpgaValue.Text = identity.FpgaFirmware;
            _bwmValue.Text = "Not queried — PM5 identity checked first";

            Log("Read-only handshake succeeded.");
            Log($"Hardware: {identity.Hardware}");
            Log($"ARM: {identity.ArmFirmware}");
            Log($"FPGA: {identity.FpgaFirmware}");
            LogCapabilities(inspection.Capabilities);
            Log("CMD_VERSION and CMD_CAPABILITIES completed without a write/reset/flash operation.");
            SetPill("DEVICE OK", AccentGreen);
        }
        catch (OperationCanceledException)
        {
            _connectionValue.Text = "Read-only analysis cancelled";
            SetUnknownHardware();
            Log("Read-only analysis cancelled.");
            SetPill("CANCELLED", AccentRed);
        }
        catch (Exception ex)
        {
            _connectionValue.Text = "PM3/PM5 protocol not verified";
            SetUnknownHardware();
            Log($"Read-only PM3/PM5 query failed: {ex.Message}");
            Log("The serial port itself opened, but no valid PM3 NG response was accepted.");
            Log("No firmware write, reset, flash or destructive command was attempted.");
            SetPill("NO RESPONSE", AccentRed);
        }
        finally
        {
            _operationInProgress = false;
            SetToolbarDeviceButtonsEnabled(true);
        }
    }

    private async Task RunSingleCommandAsync(string label, Func<Pm3SerialTransport, Task> action)
    {
        var portName = _portValue.Text;
        if (string.IsNullOrWhiteSpace(portName) || portName.StartsWith("No ", StringComparison.OrdinalIgnoreCase))
        {
            Log($"{label}: no serial port available.");
            return;
        }

        _operationInProgress = true;
        SetToolbarDeviceButtonsEnabled(false);
        Log($"--- {label} ---");
        await using var transport = new Pm3SerialTransport(portName);
        try
        {
            await transport.ConnectAsync();
            await action(transport);
        }
        catch (Exception ex)
        {
            Log($"{label} failed: {ex.Message}");
        }
        finally
        {
            _operationInProgress = false;
            SetToolbarDeviceButtonsEnabled(true);
        }
    }

    private async Task RunVersionAsync(Pm3SerialTransport transport)
    {
        var identity = await Pm3ReadOnlyInspector.QueryVersionAsync(transport);
        _hardwareValue.Text = identity.Hardware;
        _firmwareValue.Text = identity.ArmFirmware;
        _fpgaValue.Text = identity.FpgaFirmware;
        Log($"ARM: {identity.ArmFirmware}");
        Log($"FPGA: {identity.FpgaFirmware}");
        Log($"Hardware: {identity.Hardware}");
    }

    private async Task RunCapabilitiesAsync(Pm3SerialTransport transport)
    {
        var capabilities = await Pm3ReadOnlyInspector.QueryCapabilitiesAsync(transport);
        LogCapabilities(capabilities);
    }

    private async Task RunStatusAsync(Pm3SerialTransport transport)
    {
        var diag = await Pm3ReadOnlyInspector.QueryStatusAsync(transport);
        Log(FormatDiagnostic(diag));
        Log("Note: CMD_STATUS payload is not decoded (format not yet hardware-verified for PM5) — raw acknowledgement only.");
        LogRawDiagnostic(diag);
    }

    private async Task RunPingAsync(Pm3SerialTransport transport)
    {
        var diag = await Pm3ReadOnlyInspector.PingAsync(transport);
        Log(FormatDiagnostic(diag));
        LogRawDiagnostic(diag);
    }

    private async Task RunFullDiagnosticAsync()
    {
        var portName = _portValue.Text;
        if (string.IsNullOrWhiteSpace(portName) || portName.StartsWith("No ", StringComparison.OrdinalIgnoreCase))
        {
            Log("Full diagnostic: no serial port available.");
            return;
        }

        _operationInProgress = true;
        SetToolbarDeviceButtonsEnabled(false);
        SetPill("FULL DIAGNOSTIC…", AccentOrange);
        Log("=== Full diagnostic (hw version + hw capabilities + hw status + hw ping) ===");

        await using var transport = new Pm3SerialTransport(portName);
        try
        {
            await transport.ConnectAsync();
            await RunVersionAsync(transport);
            await RunCapabilitiesAsync(transport);
            await RunStatusAsync(transport);
            await RunPingAsync(transport);
            Log("=== Full diagnostic complete. No write/reset/flash command was sent. ===");
            SetPill("DEVICE OK", AccentGreen);
        }
        catch (Exception ex)
        {
            Log($"Full diagnostic aborted: {ex.Message}");
            SetPill("NO RESPONSE", AccentRed);
        }
        finally
        {
            _operationInProgress = false;
            SetToolbarDeviceButtonsEnabled(true);
        }
    }

    private static string FormatDiagnostic(Pm3RawDiagnostic diag)
        => $"{diag.CommandName}: {(diag.Success ? "OK" : "FAILED")} (status={diag.Status}, reason={diag.Reason}, payload={diag.PayloadLength} bytes)";

    private void LogCapabilities(Pm3CapabilitiesReport capabilities)
    {
        if (!capabilities.IsKnownSchema)
        {
            Log($"CMD_CAPABILITIES returned schema v{capabilities.SchemaVersion}; raw data retained, decoding intentionally skipped.");
            if (_developerMode) Log($"Capabilities raw: {Convert.ToHexString(capabilities.RawPayload)}");
            return;
        }

        _hardwareValue.Text = capabilities.IsKnownSchema
            ? $"RDV4 hw flag: {(capabilities.IsRdv4 ? "yes" : "no")} - CMD_CAPABILITIES has no PM5-specific bit"
            : $"Unknown CMD_CAPABILITIES schema (v{capabilities.SchemaVersion})";
        Log($"Capabilities schema v{capabilities.SchemaVersion}; RDV4={capabilities.IsRdv4}; enabled: {string.Join(", ", capabilities.EnabledFeatures.DefaultIfEmpty("none"))}.");
        if (_developerMode) Log($"Capabilities raw: {Convert.ToHexString(capabilities.RawPayload)}");
    }

    private void LogRawDiagnostic(Pm3RawDiagnostic diag)
    {
        if (!_developerMode) return;
        Log($"{diag.CommandName} raw: {Convert.ToHexString(diag.Payload)}");
        foreach (var frame in diag.DebugFrames)
            Log($"Interleaved 0x{frame.Command:X4}: status={frame.Status}, reason={frame.Reason}, raw={Convert.ToHexString(frame.Payload)}");
    }

    private void ExportReport()
    {
        using var dialog = new SaveFileDialog { Filter = "Text report (*.txt)|*.txt", FileName = $"pm5-diagnostic-{DateTime.Now:yyyyMMdd-HHmmss}.txt" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        File.WriteAllText(dialog.FileName, $"PM5 Control Center diagnostic report{Environment.NewLine}Exported: {DateTimeOffset.Now:O}{Environment.NewLine}{Environment.NewLine}{_log.Text}");
        Log($"Diagnostic report exported: {dialog.FileName}");
    }

    private void ShowHelp()
    {
        const string helpText =
            "PM5 Control Center — offline command reference\n" +
            "(this list is local; it does not query the device)\n\n" +
            "  Connect / analyze   Opens the serial port and runs CMD_VERSION + CMD_CAPABILITIES.\n" +
            "  hw version          Query firmware/ARM/FPGA version string (CMD_VERSION, 0x0107).\n" +
            "  hw status           Runtime status acknowledgement (CMD_STATUS, 0x0108, raw/unparsed).\n" +
            "  hw ping             Liveness check (CMD_PING, 0x0109).\n" +
            "  hw capabilities     Compiled-in feature flags; no PM5-specific bit exists (CMD_CAPABILITIES, 0x0112).\n" +
            "  Full diagnostic     Runs all four commands above in sequence and logs the results.\n" +
            "  Developer mode      Shows raw responses and interleaved diagnostic frames; it never enables custom commands.\n" +
            "  Export report       Saves the current local diagnostic log as a text report.\n" +
            "  Refresh ports       Re-scans Windows COM ports without touching the device.\n\n" +
            "Not implemented on purpose (destructive or FPGA/antenna-energizing):\n" +
            "  hw tune, hw fpga config, hw ant_pm5, hw bootloader, flash/reset commands.\n" +
            "  These require the physical-hardware phase in AI_CONTEXT.md and are out of scope\n" +
            "  for this read-only inspector until explicitly enabled.";

        MessageBox.Show(helpText, "help", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void SetUnknownHardware()
    {
        _hardwareValue.Text = "UNKNOWN — PM3/PM5 protocol not verified";
        _firmwareValue.Text = "UNKNOWN";
        _fpgaValue.Text = "UNKNOWN";
        _bwmValue.Text = "UNKNOWN";
    }

    private void Log(string message)
    {
        _log.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
    }

    private static IReadOnlyList<string> GetSerialPorts()
    {
        var result = new List<string>();
        using var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DEVICEMAP\SERIALCOMM");
        if (key is null) return result;
        foreach (var valueName in key.GetValueNames())
        {
            if (key.GetValue(valueName) is string port && !string.IsNullOrWhiteSpace(port))
                result.Add(port);
        }
        return result.OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer.Stop();
            _timer.Dispose();
        }
        base.Dispose(disposing);
    }

    private sealed class Pm5ToolbarRenderer : ToolStripProfessionalRenderer
    {
        public Pm5ToolbarRenderer() : base(new Pm5ColorTable()) { }

        protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
        {
            var button = e.Item as ToolStripButton;
            var bounds = new Rectangle(Point.Empty, e.Item.Size);
            if (button is { Pressed: true })
                e.Graphics.FillRectangle(new SolidBrush(AccentOrangeDim), bounds);
            else if (button is { Selected: true })
                e.Graphics.FillRectangle(new SolidBrush(BgControl), bounds);
            else
                e.Graphics.FillRectangle(new SolidBrush(BgControl), bounds);

            using var pen = new Pen(button is { Selected: true } or { Pressed: true } ? AccentOrange : Color.FromArgb(48, 52, 58));
            e.Graphics.DrawRectangle(pen, 0, 0, bounds.Width - 1, bounds.Height - 1);
        }

        private sealed class Pm5ColorTable : ProfessionalColorTable
        {
            public override Color ToolStripGradientBegin => BgControl;
            public override Color ToolStripGradientMiddle => BgControl;
            public override Color ToolStripGradientEnd => BgControl;
            public override Color ImageMarginGradientBegin => BgControl;
            public override Color ImageMarginGradientMiddle => BgControl;
            public override Color ImageMarginGradientEnd => BgControl;
            public override Color SeparatorDark => Color.FromArgb(60, 64, 70);
            public override Color SeparatorLight => Color.FromArgb(60, 64, 70);
        }
    }
}
