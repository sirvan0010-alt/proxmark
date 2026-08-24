using System.Drawing;

namespace PM5Control.Desktop;

internal sealed class DeveloperSettingsForm : Form
{
    private readonly CheckBox _developerMode = new();
    private readonly CheckedListBox _buttons = new();
    private readonly Label _detail = new();
    private readonly TextBox _espIp = new();
    private readonly NumericUpDown _espPort = new();
    private readonly Label _commsMode = new();
    private readonly Dictionary<string, string> _displayToName = new(StringComparer.OrdinalIgnoreCase);
    private readonly IReadOnlyDictionary<string, string> _commandInfo;

    public bool DeveloperMode => _developerMode.Checked;
    public string EspIpAddress => _espIp.Text.Trim();
    public int EspTcpPort => (int)_espPort.Value;

    public DeveloperSettingsForm(bool developerMode, IEnumerable<string> buttonNames, IEnumerable<string> enabledNames,
        IReadOnlyDictionary<string, string> commandInfo, string espIp, int espPort, string commsModeText)
    {
        _commandInfo = commandInfo;
        Text = "Settings";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(620, 620);
        BackColor = Color.FromArgb(18, 20, 24);
        ForeColor = Color.FromArgb(226, 230, 236);
        Font = new Font("Segoe UI", 9.5F);

        var espHeader = new Label
        {
            Text = "BWM / ESP32 wireless endpoint",
            AutoSize = true,
            Location = new Point(20, 16),
            Font = new Font("Segoe UI Semibold", 10F),
            ForeColor = Color.FromArgb(255, 122, 24),
        };
        Controls.Add(espHeader);

        _commsMode.Text = commsModeText;
        _commsMode.AutoSize = true;
        _commsMode.Location = new Point(20, 40);
        _commsMode.ForeColor = Color.FromArgb(142, 150, 160);
        _commsMode.Font = new Font("Consolas", 9F);
        Controls.Add(_commsMode);

        var espLabel = new Label { Text = "ESP32 IP address:", AutoSize = true, Location = new Point(20, 68) };
        Controls.Add(espLabel);

        _espIp.Text = espIp;
        _espIp.Location = new Point(160, 65);
        _espIp.Width = 160;
        _espIp.BackColor = Color.FromArgb(26, 29, 35);
        _espIp.ForeColor = Color.FromArgb(226, 230, 236);
        _espIp.PlaceholderText = "e.g. 192.168.1.42";
        Controls.Add(_espIp);

        var portLabel = new Label { Text = "TCP port:", AutoSize = true, Location = new Point(332, 68) };
        Controls.Add(portLabel);

        _espPort.Minimum = 1;
        _espPort.Maximum = 65535;
        _espPort.Value = espPort;
        _espPort.Location = new Point(400, 65);
        _espPort.Width = 80;
        _espPort.BackColor = Color.FromArgb(26, 29, 35);
        _espPort.ForeColor = Color.FromArgb(226, 230, 236);
        Controls.Add(_espPort);

        var espNote = new Label
        {
            Text = "Default 7891 matches the BWM firmware's built-in TCP forwarding server\n" +
                   "(see app_tcp_server.c). Saved here only - no Wi-Fi transport is wired up\n" +
                   "yet in this build; USB/COM3 remains the only active connection path.",
            AutoSize = true,
            Location = new Point(20, 98),
            ForeColor = Color.FromArgb(142, 150, 160),
            Font = new Font("Consolas", 8.5F),
        };
        Controls.Add(espNote);

        var sep = new Label { BorderStyle = BorderStyle.Fixed3D, Location = new Point(20, 168), Size = new Size(580, 2) };
        Controls.Add(sep);

        _developerMode.Text = "Developer mode";
        _developerMode.Checked = developerMode;
        _developerMode.AutoSize = true;
        _developerMode.Location = new Point(20, 184);
        Controls.Add(_developerMode);

        var note = new Label
        {
            Text = "Each button below shows exactly which device command it sends - select a row to see details.",
            AutoSize = true,
            Location = new Point(20, 210),
            ForeColor = Color.FromArgb(142, 150, 160)
        };
        Controls.Add(note);

        var label = new Label
        {
            Text = "Toolbar buttons enabled in developer mode:",
            AutoSize = true,
            Location = new Point(20, 238)
        };
        Controls.Add(label);

        _buttons.Location = new Point(20, 264);
        _buttons.Size = new Size(560, 220);
        _buttons.BackColor = Color.FromArgb(26, 29, 35);
        _buttons.ForeColor = Color.FromArgb(226, 230, 236);
        _buttons.Font = new Font("Consolas", 9F);
        foreach (var name in buttonNames)
        {
            var info = commandInfo.TryGetValue(name, out var text) ? text : "local only - no device command";
            var display = $"{name,-28} {info}";
            _displayToName[display] = name;
            _buttons.Items.Add(display, enabledNames.Contains(name, StringComparer.OrdinalIgnoreCase));
        }
        _buttons.SelectedIndexChanged += (_, _) => ShowDetail();
        Controls.Add(_buttons);

        _detail.AutoSize = false;
        _detail.Location = new Point(20, 494);
        _detail.Size = new Size(560, 40);
        _detail.ForeColor = Color.FromArgb(255, 122, 24);
        _detail.Font = new Font("Consolas", 9F);
        Controls.Add(_detail);

        var ok = new Button { Text = "Apply", DialogResult = DialogResult.OK, Location = new Point(410, 560), Width = 80 };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(500, 560), Width = 80 };
        Controls.Add(ok);
        Controls.Add(cancel);
        AcceptButton = ok;
        CancelButton = cancel;
    }

    private void ShowDetail()
    {
        if (_buttons.SelectedItem is not string display || !_displayToName.TryGetValue(display, out var name))
        {
            _detail.Text = "";
            return;
        }
        var info = _commandInfo.TryGetValue(name, out var text) ? text : "local only - no device command";
        _detail.Text = $"{name}: {info}";
    }

    public IReadOnlyList<string> EnabledButtons()
        => _buttons.CheckedItems.Cast<string>()
            .Select(display => _displayToName.TryGetValue(display, out var name) ? name : display)
            .ToArray();
}
