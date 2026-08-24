using System.Drawing;

namespace PM5Control.Desktop;

internal sealed class DeveloperSettingsForm : Form
{
    private readonly CheckBox _developerMode = new();
    private readonly CheckedListBox _buttons = new();

    public bool DeveloperMode => _developerMode.Checked;

    public DeveloperSettingsForm(bool developerMode, IEnumerable<string> buttonNames, IEnumerable<string> enabledNames)
    {
        Text = "Developer settings";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(520, 430);
        BackColor = Color.FromArgb(18, 20, 24);
        ForeColor = Color.FromArgb(226, 230, 236);
        Font = new Font("Segoe UI", 9.5F);

        _developerMode.Text = "Developer mode";
        _developerMode.Checked = developerMode;
        _developerMode.AutoSize = true;
        _developerMode.Location = new Point(20, 18);
        Controls.Add(_developerMode);

        var note = new Label
        {
            Text = "Developer mode is configured here, not on the main device card.\nIt exposes raw diagnostic data and per-button visibility settings.",
            AutoSize = true,
            Location = new Point(20, 48),
            ForeColor = Color.FromArgb(142, 150, 160)
        };
        Controls.Add(note);

        var label = new Label
        {
            Text = "Toolbar buttons enabled in developer mode:",
            AutoSize = true,
            Location = new Point(20, 105)
        };
        Controls.Add(label);

        _buttons.Location = new Point(20, 132);
        _buttons.Size = new Size(460, 220);
        _buttons.BackColor = Color.FromArgb(26, 29, 35);
        _buttons.ForeColor = Color.FromArgb(226, 230, 236);
        foreach (var name in buttonNames)
        {
            var index = _buttons.Items.Add(name, enabledNames.Contains(name, StringComparer.OrdinalIgnoreCase));
        }
        Controls.Add(_buttons);

        var ok = new Button { Text = "Apply", DialogResult = DialogResult.OK, Location = new Point(310, 375), Width = 80 };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(400, 375), Width = 80 };
        Controls.Add(ok);
        Controls.Add(cancel);
        AcceptButton = ok;
        CancelButton = cancel;
    }

    public IReadOnlyList<string> EnabledButtons()
        => _buttons.CheckedItems.Cast<string>().ToArray();
}
