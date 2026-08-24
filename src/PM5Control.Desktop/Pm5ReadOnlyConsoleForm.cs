using Microsoft.Win32;
using PM5Control.Core.Connections;
using PM5Control.Core.Protocols.Pm3;

namespace PM5Control.Desktop;

/// <summary>
/// Developer console and persistent custom-button panel. All device traffic is
/// constrained to the same verified read-only whitelist; custom buttons cannot
/// bypass that policy.
/// </summary>
internal sealed class Pm5ReadOnlyConsoleForm : Form
{
    private static readonly (string Name, ushort Command)[] Whitelist =
    {
        ("hw version", Pm3CommandCode.Version),
        ("hw status", Pm3CommandCode.Status),
        ("hw ping", Pm3CommandCode.Ping),
        ("hw capabilities", Pm3CommandCode.Capabilities),
    };

    private readonly AppSettings _settings = AppSettings.Load();
    private readonly TextBox _output = new();
    private readonly TextBox _input = new();
    private readonly Button _send = new();
    private readonly Button[] _customButtons = new Button[10];
    private readonly TextBox[] _nameEditors = new TextBox[10];
    private readonly TextBox[] _commandEditors = new TextBox[10];
    private readonly CheckBox[] _enabledEditors = new CheckBox[10];
    private readonly Button _saveCustom = new();
    private readonly Button _testCustom = new();

    public Pm5ReadOnlyConsoleForm()
    {
        Text = "PM5 Developer Console — READ-ONLY";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(980, 650);
        Size = new Size(1180, 760);
        BackColor = Color.FromArgb(18, 20, 24);
        ForeColor = Color.FromArgb(226, 230, 236);

        var tabs = new TabControl { Dock = DockStyle.Fill };
        var consolePage = new TabPage("PM5> Console") { BackColor = BackColor, ForeColor = ForeColor };
        var customPage = new TabPage("Custom Commands") { BackColor = BackColor, ForeColor = ForeColor };
        tabs.TabPages.Add(consolePage);
        tabs.TabPages.Add(customPage);
        Controls.Add(tabs);

        BuildConsolePage(consolePage);
        BuildCustomPage(customPage);
    }

    private void BuildConsolePage(TabPage page)
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(12) };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        page.Controls.Add(root);

        var policy = new Label
        {
            Text = "READ-ONLY POLICY  ·  hw version / hw status / hw ping / hw capabilities  ·  writes, reset and flash blocked",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 9F),
            ForeColor = Color.FromArgb(94, 214, 130)
        };
        root.Controls.Add(policy, 0, 0);

        _output.Multiline = true;
        _output.ReadOnly = true;
        _output.ScrollBars = ScrollBars.Both;
        _output.WordWrap = false;
        _output.Dock = DockStyle.Fill;
        _output.Font = new Font("Consolas", 9.5F);
        _output.BackColor = Color.FromArgb(12, 13, 16);
        _output.ForeColor = Color.FromArgb(94, 214, 130);
        root.Controls.Add(_output, 0, 1);

        var commandRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = new Padding(0, 10, 0, 0) };
        commandRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        commandRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _input.Dock = DockStyle.Fill;
        _input.Font = new Font("Consolas", 10F);
        _input.BackColor = Color.FromArgb(26, 29, 35);
        _input.ForeColor = Color.FromArgb(226, 230, 236);
        _input.Text = "hw capabilities";
        _input.KeyDown += async (_, e) =>
        {
            if (e.KeyCode != Keys.Enter || e.Shift || e.Control || e.Alt) return;
            e.SuppressKeyPress = true;
            await ExecuteCommandAsync(_input.Text.Trim());
        };
        commandRow.Controls.Add(_input, 0, 0);
        _send.Text = "Send read-only";
        _send.AutoSize = true;
        _send.Click += async (_, _) => await ExecuteCommandAsync(_input.Text.Trim());
        commandRow.Controls.Add(_send, 1, 0);
        root.Controls.Add(commandRow, 0, 2);

        Append("PM5> Read-only console ready.");
        Append("PM5> Type 'help' for the whitelist. Unknown commands are rejected before transport access.");
        Append("PM5> Exact TX/RX frames are logged for every accepted transaction.");
        Append(string.Empty);
    }

    private void BuildCustomPage(TabPage page)
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(12) };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        page.Controls.Add(root);

        var header = new Label
        {
            Text = "CUSTOM COMMANDS  ·  10 persistent debug buttons  ·  only whitelisted read-only commands can execute",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 9F),
            ForeColor = Color.FromArgb(255, 170, 80)
        };
        root.Controls.Add(header, 0, 0);

        var grid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 11, AutoScroll = true, Padding = new Padding(0, 8, 0, 0) };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
        grid.Controls.Add(MakeHeader("Button"), 0, 0);
        grid.Controls.Add(MakeHeader("Name"), 1, 0);
        grid.Controls.Add(MakeHeader("Command"), 2, 0);
        grid.Controls.Add(MakeHeader("Enabled"), 3, 0);

        for (var i = 0; i < 10; i++)
        {
            var index = i;
            _customButtons[i] = new Button
            {
                Text = _settings.CustomButtonNames[i],
                Dock = DockStyle.Fill,
                Margin = new Padding(2),
                Tag = index,
                BackColor = Color.FromArgb(26, 29, 35),
                ForeColor = Color.FromArgb(226, 230, 236)
            };
            _customButtons[i].Click += async (_, _) => await ExecuteCustomAsync(index);
            grid.Controls.Add(_customButtons[i], 0, i + 1);

            _nameEditors[i] = MakeEditor(_settings.CustomButtonNames[i]);
            _commandEditors[i] = MakeEditor(_settings.CustomButtonCommands[i]);
            _enabledEditors[i] = new CheckBox { Checked = _settings.CustomButtonEnabled[i], Dock = DockStyle.None, Anchor = AnchorStyles.None, AutoSize = true };
            grid.Controls.Add(_nameEditors[i], 1, i + 1);
            grid.Controls.Add(_commandEditors[i], 2, i + 1);
            grid.Controls.Add(_enabledEditors[i], 3, i + 1);
        }
        root.Controls.Add(grid, 0, 1);

        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
        _saveCustom.Text = "SAVE BUTTONS";
        _saveCustom.AutoSize = true;
        _saveCustom.Click += (_, _) => SaveCustomButtons();
        _testCustom.Text = "TEST SELECTED";
        _testCustom.AutoSize = true;
        _testCustom.Click += async (_, _) => await ExecuteCustomAsync(SelectedEditorIndex());
        actions.Controls.Add(_saveCustom);
        actions.Controls.Add(_testCustom);
        actions.Controls.Add(new Label { Text = "  Tip: define a name + one of the four allowed commands, then Save.", AutoSize = true, Padding = new Padding(8, 7, 0, 0), ForeColor = Color.FromArgb(142, 150, 160) });
        root.Controls.Add(actions, 0, 2);
    }

    private static Label MakeHeader(string text) => new() { Text = text, AutoSize = true, Font = new Font("Segoe UI Semibold", 9F), ForeColor = Color.FromArgb(142, 150, 160), Padding = new Padding(4) };

    private static TextBox MakeEditor(string text) => new() { Text = text, Dock = DockStyle.Fill, Font = new Font("Consolas", 9F), BackColor = Color.FromArgb(26, 29, 35), ForeColor = Color.FromArgb(226, 230, 236) };

    private int SelectedEditorIndex()
    {
        var focused = Array.FindIndex(_commandEditors, x => x.Focused);
        if (focused >= 0) return focused;
        focused = Array.FindIndex(_nameEditors, x => x.Focused);
        return focused >= 0 ? focused : 0;
    }

    private void SaveCustomButtons()
    {
        for (var i = 0; i < 10; i++)
        {
            _settings.CustomButtonNames[i] = string.IsNullOrWhiteSpace(_nameEditors[i].Text) ? $"Button {i + 1}" : _nameEditors[i].Text.Trim();
            _settings.CustomButtonCommands[i] = _commandEditors[i].Text.Trim();
            _settings.CustomButtonEnabled[i] = _enabledEditors[i].Checked;
            _customButtons[i].Text = _settings.CustomButtonNames[i];
            _customButtons[i].Enabled = _settings.CustomButtonEnabled[i];
        }
        _settings.Save();
        Append("Custom command configuration saved locally.");
    }

    private async Task ExecuteCustomAsync(int index)
    {
        SaveCustomButtons();
        if (index < 0 || index >= 10) return;
        var name = _settings.CustomButtonNames[index];
        var command = _settings.CustomButtonCommands[index];
        if (!_settings.CustomButtonEnabled[index]) { Append($"CUSTOM [{name}] blocked: button disabled."); return; }
        Append($"CUSTOM [{name}] -> {command}");
        await ExecuteCommandAsync(command);
    }

    private async Task ExecuteCommandAsync(string command)
    {
        if (string.IsNullOrWhiteSpace(command)) return;
        Append($"PM5> {command}");

        if (command.Equals("help", StringComparison.OrdinalIgnoreCase))
        {
            Append("Allowed commands:");
            foreach (var item in Whitelist) Append($"  {item.Name}  (0x{item.Command:X4})");
            Append("  help");
            Append("  clear");
            Append("Custom buttons use this exact same whitelist.");
            Append(string.Empty);
            return;
        }
        if (command.Equals("clear", StringComparison.OrdinalIgnoreCase)) { _output.Clear(); return; }

        var match = Whitelist.FirstOrDefault(x => x.Name.Equals(command, StringComparison.OrdinalIgnoreCase));
        if (match == default)
        {
            Append("ERROR: command blocked by read-only whitelist.");
            Append("No bytes were sent to the device.");
            Append(string.Empty);
            return;
        }

        _send.Enabled = false;
        try
        {
            var port = GetSerialPorts().FirstOrDefault(p => p.Equals("COM3", StringComparison.OrdinalIgnoreCase)) ?? GetSerialPorts().FirstOrDefault();
            if (port is null) { Append("ERROR: no Windows serial port detected. No bytes were sent."); return; }
            Append($"TX policy: ALLOWED {match.Name} (0x{match.Command:X4}) on {port}");
            await using var transport = new Pm3SerialTransport(port);
            await transport.ConnectAsync();
            var result = await Pm3ReadOnlyInspector.QueryAsync(transport, match.Command, ToProtocolName(match.Name));
            Append($"TX frame:      {Convert.ToHexString(result.RequestFrame)}");
            var responseMatch = result.ResponseCommandMatches ? "MATCH" : $"MISMATCH expected 0x{result.ExpectedCommand:X4}, got 0x{result.ResponseCommand:X4}";
            Append($"RESPONSE: {(result.Success ? "OK" : "REJECTED")}, {responseMatch}, status={result.Status}, reason={result.Reason}, payload={result.PayloadLength} bytes");
            foreach (var frame in result.UnmatchedResponses)
                Append($"RX unmatched:  cmd=0x{frame.Command:X4}, status={frame.Status}, reason={frame.Reason}, payload={frame.Payload.Length} bytes, raw={Convert.ToHexString(frame.RawFrame)}");
            Append($"RX response:   {Convert.ToHexString(result.RawResponseFrame)}");
            Append($"RX payload:    {Convert.ToHexString(result.Payload)}");
            foreach (var frame in result.DebugFrames) Append($"RX debug 0x{frame.Command:X4}: {Convert.ToHexString(frame.RawFrame)}");
            Append($"Correlation: expected 0x{result.ExpectedCommand:X4}; matched response 0x{result.ResponseCommand:X4}; unmatched={result.UnmatchedResponses.Count}; debug={result.DebugFrames.Count}");
            Append("Safety: read-only whitelist enforced; no write/reset/flash command was authorized.");
            Append(string.Empty);
        }
        catch (OperationCanceledException)
        {
            Append($"TIMEOUT: no matching response for CMD 0x{match.Command:X4} within the transaction timeout.");
            Append("No firmware write, reset or flash operation was attempted.");
            Append(string.Empty);
        }
        catch (Exception ex)
        {
            Append($"ERROR: {ex.Message}");
            Append("No firmware write, reset or flash operation was attempted.");
            Append(string.Empty);
        }
        finally { _send.Enabled = true; _input.Focus(); }
    }

    private static string ToProtocolName(string command) => command switch
    {
        "hw version" => "CMD_VERSION",
        "hw status" => "CMD_STATUS",
        "hw ping" => "CMD_PING",
        "hw capabilities" => "CMD_CAPABILITIES",
        _ => command,
    };

    private static IReadOnlyList<string> GetSerialPorts()
    {
        var result = new List<string>();
        using var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DEVICEMAP\SERIALCOMM");
        if (key is null) return result;
        foreach (var name in key.GetValueNames()) if (key.GetValue(name) is string port && !string.IsNullOrWhiteSpace(port)) result.Add(port);
        return result.OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private void Append(string text) => _output.AppendText($"[{DateTime.Now:HH:mm:ss}] {text}{Environment.NewLine}");
}
