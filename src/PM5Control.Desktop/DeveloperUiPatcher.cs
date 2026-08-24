using Microsoft.Win32;
using PM5Control.Core.Connections;
using PM5Control.Core.Protocols.Pm3;

namespace PM5Control.Desktop;

internal static class DeveloperUiPatcher
{
    private static bool _developerMode;
    private static IReadOnlyList<string> _enabledButtons = Array.Empty<string>();

    public static void Install(MainForm form) => ConfigureToolbar(form);

    private static void ConfigureToolbar(Form form)
    {
        var strip = form.Controls.OfType<TableLayoutPanel>().FirstOrDefault()?.Controls.OfType<ToolStrip>().FirstOrDefault();
        if (strip is null || strip.Items.OfType<ToolStripButton>().Any(x => x.Text == "Probe read-only commands")) return;

        foreach (var item in strip.Items.OfType<ToolStripButton>().Where(x => x.Text.Equals("Developer mode", StringComparison.OrdinalIgnoreCase)).ToArray())
            strip.Items.Remove(item);

        var probe = new ToolStripButton("Probe read-only commands") { DisplayStyle = ToolStripItemDisplayStyle.Text };
        probe.Click += async (_, _) => await RunProbeAsync(form);
        strip.Items.Insert(Math.Max(0, strip.Items.Count - 1), probe);

        var settings = new ToolStripButton("Settings") { DisplayStyle = ToolStripItemDisplayStyle.Text };
        settings.Click += (_, _) => ShowSettings(form, strip);
        strip.Items.Add(new ToolStripSeparator());
        strip.Items.Add(settings);
    }

    private static void ShowSettings(Form owner, ToolStrip strip)
    {
        var names = strip.Items.OfType<ToolStripButton>()
            .Where(x => x.Text is not "Settings" and not "Probe read-only commands" and not "Refresh ports")
            .Select(x => x.Text).ToArray();
        var enabled = _enabledButtons.Count == 0 ? names : _enabledButtons;
        using var dialog = new DeveloperSettingsForm(_developerMode, names, enabled);
        if (dialog.ShowDialog(owner) != DialogResult.OK) return;
        _developerMode = dialog.DeveloperMode;
        _enabledButtons = dialog.EnabledButtons();
        foreach (var button in strip.Items.OfType<ToolStripButton>())
        {
            if (button.Text is "Settings" or "Probe read-only commands" or "Refresh ports") continue;
            button.Visible = !_developerMode || _enabledButtons.Contains(button.Text, StringComparer.OrdinalIgnoreCase);
        }
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

        await using var transport = new Pm3SerialTransport(port);
        try
        {
            await transport.ConnectAsync();
            foreach (var item in ProbeCommands)
            {
                try
                {
                    var result = await Pm3ReadOnlyInspector.QueryAsync(transport, item.Command, item.Name);
                    dialog.Append($"{item.Name} (0x{item.Command:X4}): {(result.Success ? "OK" : "REJECTED")}, status={result.Status}, reason={result.Reason}, payload={result.PayloadLength} bytes");
                    dialog.Append($"  raw: {Convert.ToHexString(result.Payload)}");
                    foreach (var frame in result.DebugFrames)
                        dialog.Append($"  debug 0x{frame.Command:X4}: {Convert.ToHexString(frame.Payload)}");
                }
                catch (Exception ex)
                {
                    dialog.Append($"{item.Name} (0x{item.Command:X4}): ERROR — {ex.Message}");
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
            Size = new Size(900, 620);
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
