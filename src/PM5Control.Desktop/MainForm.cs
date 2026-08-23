using Microsoft.Win32;
using PM5Control.Core.Connections;
using PM5Control.Core.Protocols.Bwm;
using PM5Control.Core.Protocols.Pm3;

namespace PM5Control.Desktop;

internal sealed class MainForm : Form
{
    private readonly Label _connectionValue = new();
    private readonly Label _portValue = new();
    private readonly Label _hardwareValue = new();
    private readonly Label _firmwareValue = new();
    private readonly Label _fpgaValue = new();
    private readonly Label _bwmValue = new();
    private readonly TextBox _log = new();
    private readonly Button _connectButton = new();
    private readonly Button _refreshButton = new();
    private readonly System.Windows.Forms.Timer _timer = new();

    public MainForm()
    {
        Text = "PM5 Control Center";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(900, 600);
        Size = new Size(1000, 680);
        Font = new Font("Segoe UI", 10F);

        BuildUi();
        RefreshDeviceState();

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
            Padding = new Padding(18),
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 235));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(root);

        var title = new Label
        {
            Text = "PM5 Control Center",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Semibold", 20F),
            TextAlign = ContentAlignment.MiddleLeft,
        };
        root.Controls.Add(title, 0, 0);

        var status = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 6,
            Padding = new Padding(0, 5, 0, 5),
        };
        status.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
        status.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.Controls.Add(status, 0, 1);

        AddStatus(status, 0, "Connection", _connectionValue);
        AddStatus(status, 1, "USB / Serial port", _portValue);
        AddStatus(status, 2, "Hardware", _hardwareValue);
        AddStatus(status, 3, "ARM firmware", _firmwareValue);
        AddStatus(status, 4, "FPGA", _fpgaValue);
        AddStatus(status, 5, "ESP32 / BWM", _bwmValue);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
        _connectButton.Text = "Connect / analyze (read-only)";
        _connectButton.AutoSize = true;
        _connectButton.Click += async (_, _) => await ConnectReadOnlyAsync();
        _refreshButton.Text = "Refresh device";
        _refreshButton.AutoSize = true;
        _refreshButton.Click += (_, _) => RefreshDeviceState();
        buttons.Controls.Add(_connectButton);
        buttons.Controls.Add(_refreshButton);
        root.Controls.Add(buttons, 0, 2);

        _log.Multiline = true;
        _log.ReadOnly = true;
        _log.ScrollBars = ScrollBars.Vertical;
        _log.Dock = DockStyle.Fill;
        _log.Font = new Font("Consolas", 9.5F);
        root.Controls.Add(_log, 0, 3);
    }

    private static void AddStatus(TableLayoutPanel panel, int row, string caption, Label value)
    {
        var label = new Label
        {
            Text = caption,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
        };
        value.Text = "UNKNOWN";
        value.Dock = DockStyle.Fill;
        value.TextAlign = ContentAlignment.MiddleLeft;
        value.Font = new Font("Segoe UI Semibold", 10F);
        panel.Controls.Add(label, 0, row);
        panel.Controls.Add(value, 1, row);
    }

    private void RefreshDeviceState(bool logChanges = true)
    {
        var ports = GetSerialPorts();
        var port = ports.FirstOrDefault(p => p.Equals("COM3", StringComparison.OrdinalIgnoreCase)) ?? ports.FirstOrDefault();

        if (port is null)
        {
            _connectionValue.Text = "Not detected";
            _portValue.Text = "No serial ports detected";
            _connectButton.Enabled = false;
            SetUnknownHardware();
            if (logChanges) Log("No Windows serial port detected.");
            return;
        }

        _connectionValue.Text = "Device transport detected";
        _portValue.Text = port;
        _connectButton.Enabled = true;
        SetUnknownHardware();
        if (logChanges) Log($"Windows serial port detected: {port}");
    }

    private async Task ConnectReadOnlyAsync()
    {
        var portName = _portValue.Text;
        if (string.IsNullOrWhiteSpace(portName) || portName.StartsWith("No ", StringComparison.OrdinalIgnoreCase))
            return;

        _connectButton.Enabled = false;
        _connectionValue.Text = "Analyzing PM3/PM5 read-only protocol...";
        Log($"Selected {portName}.");
        Log("Sending only CMD_VERSION (0x0107) and CMD_CAPABILITIES (0x0112).");
        Log("No firmware write, reset, flash or destructive command is authorized by this path.");

        await using var transport = new Pm3SerialTransport(portName);
        try
        {
            await transport.ConnectAsync();
            Log("Serial endpoint opened successfully; DTR/RTS disabled.");

            var identity = await Pm3ReadOnlyInspector.InspectAsync(transport);
            _connectionValue.Text = "PM3/PM5 NG response received";
            _hardwareValue.Text = identity.Hardware;
            _firmwareValue.Text = identity.ArmFirmware;
            _fpgaValue.Text = identity.FpgaFirmware;
            _bwmValue.Text = "Not queried — PM5 identity checked first";

            Log("Read-only handshake succeeded.");
            Log($"Hardware: {identity.Hardware}");
            Log($"ARM: {identity.ArmFirmware}");
            Log($"FPGA: {identity.FpgaFirmware}");
            Log("CMD_VERSION and CMD_CAPABILITIES completed without a write/reset/flash operation.");
        }
        catch (OperationCanceledException)
        {
            _connectionValue.Text = "Read-only analysis cancelled";
            SetUnknownHardware();
            Log("Read-only analysis cancelled.");
        }
        catch (Exception ex)
        {
            _connectionValue.Text = "PM3/PM5 protocol not verified";
            SetUnknownHardware();
            Log($"Read-only PM3/PM5 query failed: {ex.Message}");
            Log("The serial port itself opened, but no valid PM3 NG response was accepted.");
            Log("No firmware write, reset, flash or destructive command was attempted.");
        }
        finally
        {
            _connectButton.Enabled = true;
        }
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
        if (key is null)
            return result;

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
}
