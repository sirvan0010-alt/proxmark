using Microsoft.Win32;
using PM5Control.Core.Connections;
using PM5Control.Core.Protocols.Pm3;

namespace PM5Control.Desktop;

internal static class DeveloperUiPatcher
{
    private static bool _developerMode;
    private static IReadOnlyList<string> _enabledButtons = Array.Empty<string>();
    private static readonly AppSettings Settings = AppSettings.Load();

    public static void Install(MainForm form) => ConfigureToolbar(form);

    private static void ConfigureToolbar(Form form)
    {
        var strip = form.Controls.OfType<TableLayoutPanel>().FirstOrDefault()?.Controls.OfType<ToolStrip>().FirstOrDefault();
        if (strip is null || strip.Items.OfType<ToolStripButton>().Any(x => x.Text == "PM5> Console")) return;

        foreach (var item in strip.Items.OfType<ToolStripButton>().Where(x => x.Text.Equals("Developer mode", StringComparison.OrdinalIgnoreCase)).ToArray())
            strip.Items.Remove(item);

        var console = new ToolStripButton("PM5> Console") { DisplayStyle = ToolStripItemDisplayStyle.Text };
        console.Click += (_, _) => ShowConsole(form);
        strip.Items.Insert(Math.Max(0, strip.Items.Count - 1), console);

        var probe = new ToolStripButton("Probe read-only commands") { DisplayStyle = ToolStripItemDisplayStyle.Text };
        probe.Click += async (_, _) => await RunProbeAsync(form);
        strip.Items.Insert(Math.Max(0, strip.Items.Count - 1), probe);

        var settings = new ToolStripButton("Settings") { DisplayStyle = ToolStripItemDisplayStyle.Text };
        settings.Click += (_, _) => ShowSettings(form, strip);
        strip.Items.Add(new ToolStripSeparator());
        strip.Items.Add(settings);
    }

    private static readonly IReadOnlyDictionary<string, string> ButtonCommandInfo = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Connect / analyze"] = "CMD_VERSION (0x0107) + CMD_CAPABILITIES (0x0112)",
        ["hw version"] = "CMD_VERSION (0x0107)",
        ["hw status"] = "CMD_STATUS (0x0108) - raw/unparsed",
        ["hw ping"] = "CMD_PING (0x0109)",
        ["hw capabilities"] = "CMD_CAPABILITIES (0x0112)",
        ["Full diagnostic (hw info)"] = "CMD_VERSION + CMD_CAPABILITIES + CMD_STATUS + CMD_PING",
        ["PM5> Console"] = "interactive UI over the same four-command read-only whitelist",
        ["Probe read-only commands"] = "fixed read-only probe; exact TX/RX frames logged",
        ["help"] = "local only - lists this tool's own commands, does not query the device",
        ["Refresh ports"] = "local only - re-scans Windows COM ports",
    };

    private static void ShowConsole(Form owner)
    {
        using var console = new Pm5ReadOnlyConsoleForm();
        console.ShowDialog(owner);
    }

    private static void ShowSettings(Form owner, ToolStrip strip)
    {
        var names = strip.Items.OfType<ToolStripButton>()
            .Where(x => x.Text is not "Settings" and not "Probe read-only commands" and not "PM5> Console" and not "Refresh ports")
            .Select(x => x.Text).ToArray();
        var enabled = _enabledButtons.Count == 0 ? names : _enabledButtons;
        var commsMode = DescribeCommsMode();
        using var dialog = new DeveloperSettingsForm(_developerMode, names, enabled, ButtonCommandInfo, Settings.EspIpAddress, Settings.EspTcpPort, commsMode);
        if (dialog.ShowDialog(owner) != DialogResult.OK) return;
        _developerMode = dialog.DeveloperMode;
        _enabledButtons = dialog.EnabledButtons();
        Settings.EspIpAddress = dialog.EspIpAddress;
        Settings.EspTcpPort = dialog.EspTcpPort;
        Settings.Save();
        foreach (var button in strip.Items.OfType<ToolStripButton>())
        {
            if (button.Text is "Settings" or "Probe read-only commands" or "PM5> Console" or "Refresh ports") continue;
            button.Visible = !_developerMode || _enabledButtons.Contains(button.Text, StringComparer.OrdinalIgnoreCase);
        }
    }

    private static string DescribeCommsMode()
    {
        var ports = GetSerialPorts();
        var usbLine = ports.Count > 0
            ? $"USB/Serial: detected on {string.Join(", ", ports)}"
            : "USB/Serial: no COM port detected";
        var wifiLine = string.IsNullOrWhiteSpace(Settings.EspIpAddress)
            ? "Wi-Fi/BLE: no ESP32 IP configured - not probed"
            : $"Wi-Fi/BLE: IP configured ({Settings.EspIpAddress}:{Settings.EspTcpPort}) but TCP transport not yet implemented in this build";
        return usbLine + "\n" + wifiLine;
    }

    private static async Task RunProbeAsync(Form owner)
    {
        var ports = GetSerialPorts();
        var port = ports.FirstOrDefault(p => p.Equals("COM3", StringComparison.OrdinalIgnoreCase)) ?? ports.FirstOrDefault();
        if (port is null)
        {
            MessageBox.Show(owner, "No Windows serial port detected.", "Read-only command probe", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var dialog = new ProbeResultForm();
        dialog.Show(owner);
        dialog.Append($"Selected {port}");
        dialog.Append("Only whitelisted read-only commands will be sent.");
        dialog.Append("Each command is a separate transaction; exact TX and RX frames are logged.");

        await using var transport = new Pm3SerialTransport(port);
        try
        {
            await transport.ConnectAsync();
            foreach (var item in ProbeCommands)
            {
                dialog.Append($"=== TRANSACTION {item.Name} (0x{item.Command:X4}) ===");
                try
                {
                    var result = await Pm3ReadOnlyInspector.QueryAsync(transport, item.Command, item.Name);
                    dialog.Append($"  TX frame:      {Convert.ToHexString(result.RequestFrame)}");
                    var match = result.ResponseCommandMatches ? "MATCH" : $"MISMATCH expected 0x{result.ExpectedCommand:X4}, got 0x{result.ResponseCommand:X4}";
                    dialog.Append($"  RESPONSE: {(result.Success ? "OK" : "REJECTED")}, {match}, status={result.Status}, reason={result.Reason}, payload={result.PayloadLength} bytes");
                    dialog.Append($"  RX response:   {Convert.ToHexString(result.RawResponseFrame)}");
                    dialog.Append($"  payload:       {Convert.ToHexString(result.Payload)}");
                    dialog.Append($"  debug frames:  {result.DebugFrames.Count}");
                    foreach (var frame in result.DebugFrames)
                        dialog.Append($"  RX debug 0x{frame.Command:X4}: {Convert.ToHexString(frame.RawFrame)}");
                }
                catch (Exception ex)
                {
                    dialog.Append($"  ERROR — {ex.Message}");
                }
            }
            dialog.Append("Probe complete. No write, reset, flash or simulation command was sent.");
        }
        catch (Exception ex)
        {
            dialog.Append($"Transport error: {ex.Message}");
        }
    }

    private static readonly (ushort Command, string Name)[] ProbeCommands =
    {
        (Pm3CommandCode.Version, "CMD_VERSION"),
        (Pm3CommandCode.Status, "CMD_STATUS"),
        (Pm3CommandCode.Ping, "CMD_PING"),
        (Pm3CommandCode.Capabilities, "CMD_CAPABILITIES"),
        (Pm3CommandCode.GetDebugMode, "CMD_GET_DBGMODE"),
        (Pm3CommandCode.FlashMemInfo, "CMD_FLASHMEM_INFO"),
        (Pm3CommandCode.FlashMemGetSignature, "CMD_FLASHMEM_GET_SIGNATURE"),
        (Pm3CommandCode.FlashMemGetInfo, "CMD_FLASHMEM_GET_INFO"),
        (Pm3CommandCode.LfSamplingGetConfig, "CMD_LF_SAMPLING_GET_CONFIG")
    };

    private static IReadOnlyList<string> GetSerialPorts()
    {
        var result = new List<string>();
        using var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DEVICEMAP\SERIALCOMM");
        if (key is null) return result;
        foreach (var name in key.GetValueNames())
            if (key.GetValue(name) is string port && !string.IsNullOrWhiteSpace(port)) result.Add(port);
        return result.OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private sealed class ProbeResultForm : Form
    {
        private readonly TextBox _log = new();
        public ProbeResultForm()
        {
            Text = "PM5 read-only command probe";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(1100, 720);
            _log.Multiline = true;
            _log.ReadOnly = true;
            _log.ScrollBars = ScrollBars.Both;
            _log.Dock = DockStyle.Fill;
            _log.Font = new Font("Consolas", 9F);
            _log.BackColor = Color.FromArgb(12, 13, 16);
            _log.ForeColor = Color.FromArgb(94, 214, 130);
            Controls.Add(_log);
        }
        public void Append(string text) => _log.AppendText($"[{DateTime.Now:HH:mm:ss}] {text}{Environment.NewLine}");
    }
}
