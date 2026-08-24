using Microsoft.Win32;
using PM5Control.Core.Connections;
using PM5Control.Core.Protocols.Pm3;

namespace PM5Control.Desktop;

/// <summary>
/// Interactive-looking PM5 console backed by a strict read-only command whitelist.
/// It is deliberately not a raw command passthrough.
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

    private readonly TextBox _output = new();
    private readonly TextBox _input = new();
    private readonly Button _send = new();
    private readonly Label _policy = new();

    public Pm5ReadOnlyConsoleForm()
    {
        Text = "PM5 Read-Only Console";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(900, 560);
        Size = new Size(1120, 700);
        BackColor = Color.FromArgb(18, 20, 24);
        ForeColor = Color.FromArgb(226, 230, 236);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(12),
            BackColor = BackColor,
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(root);

        _policy.Text = "READ-ONLY POLICY  ·  whitelist: hw version / hw status / hw ping / hw capabilities  ·  writes, reset and flash blocked";
        _policy.AutoSize = true;
        _policy.Font = new Font("Segoe UI Semibold", 9F);
        _policy.ForeColor = Color.FromArgb(94, 214, 130);
        root.Controls.Add(_policy, 0, 0);

        _output.Multiline = true;
        _output.ReadOnly = true;
        _output.ScrollBars = ScrollBars.Both;
        _output.WordWrap = false;
        _output.Dock = DockStyle.Fill;
        _output.Font = new Font("Consolas", 9.5F);
        _output.BackColor = Color.FromArgb(12, 13, 16);
        _output.ForeColor = Color.FromArgb(94, 214, 130);
        root.Controls.Add(_output, 0, 1);

        var commandRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 10, 0, 0),
        };
        commandRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        commandRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        _input.Dock = DockStyle.Fill;
        _input.Font = new Font("Consolas", 10F);
        _input.BackColor = Color.FromArgb(26, 29, 35);
        _input.ForeColor = Color.FromArgb(226, 230, 236);
        _input.BorderStyle = BorderStyle.FixedSingle;
        _input.Text = "hw capabilities";
        _input.KeyDown += InputKeyDown;
        commandRow.Controls.Add(_input, 0, 0);

        _send.Text = "Send read-only";
        _send.AutoSize = true;
        _send.Height = 30;
        _send.Click += async (_, _) => await ExecuteInputAsync();
        commandRow.Controls.Add(_send, 1, 0);
        root.Controls.Add(commandRow, 0, 2);

        Append("PM5> Read-only console ready.");
        Append("PM5> Type 'help' for the exact whitelist. Unknown commands are rejected before transport access.");
        Append("PM5> Raw TX/RX frames are logged for every accepted transaction.");
        Append(string.Empty);
    }

    private async void InputKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Enter || e.Shift || e.Control || e.Alt) return;
        e.SuppressKeyPress = true;
        await ExecuteInputAsync();
    }

    private async Task ExecuteInputAsync()
    {
        var command = _input.Text.Trim();
        if (command.Length == 0) return;
        _input.Clear();
        Append($"PM5> {command}");

        if (command.Equals("help", StringComparison.OrdinalIgnoreCase))
        {
            Append("Allowed commands:");
            foreach (var item in Whitelist) Append($"  {item.Name}  (0x{item.Command:X4})");
            Append("  help");
            Append("  clear");
            Append("Everything else is blocked. No generic/raw command entry exists.");
            Append(string.Empty);
            return;
        }

        if (command.Equals("clear", StringComparison.OrdinalIgnoreCase))
        {
            _output.Clear();
            return;
        }

        var match = Whitelist.FirstOrDefault(x => x.Name.Equals(command, StringComparison.OrdinalIgnoreCase));
        if (match == default)
        {
            Append("ERROR: command blocked by read-only whitelist.");
            Append("No bytes were sent to the device.");
            Append(string.Empty);
            return;
        }

        _send.Enabled = false;
        _input.Enabled = false;
        try
        {
            var port = GetSerialPorts().FirstOrDefault(p => p.Equals("COM3", StringComparison.OrdinalIgnoreCase)) ?? GetSerialPorts().FirstOrDefault();
            if (port is null)
            {
                Append("ERROR: no Windows serial port detected. No bytes were sent.");
                return;
            }

            Append($"TX policy: ALLOWED read-only command {match.Name} (0x{match.Command:X4}) on {port}");
            await using var transport = new Pm3SerialTransport(port);
            await transport.ConnectAsync();
            var result = await Pm3ReadOnlyInspector.QueryAsync(transport, match.Command, ToProtocolName(match.Name));

            Append($"TX frame:      {Convert.ToHexString(result.RequestFrame)}");
            var responseMatch = result.ResponseCommandMatches
                ? "MATCH"
                : $"MISMATCH expected 0x{result.ExpectedCommand:X4}, got 0x{result.ResponseCommand:X4}";
            Append($"RESPONSE: {(result.Success ? "OK" : "REJECTED")}, {responseMatch}, status={result.Status}, reason={result.Reason}, payload={result.PayloadLength} bytes");
            Append($"RX response:   {Convert.ToHexString(result.RawResponseFrame)}");
            Append($"RX payload:    {Convert.ToHexString(result.Payload)}");
            if (result.DebugFrames.Count > 0)
            {
                Append($"RX debug frames: {result.DebugFrames.Count}");
                foreach (var frame in result.DebugFrames)
                    Append($"  RX debug 0x{frame.Command:X4}: {Convert.ToHexString(frame.RawFrame)}");
            }
            Append("Safety: transaction was limited to the read-only whitelist; no write/reset/flash command was authorized.");
            Append(string.Empty);
        }
        catch (Exception ex)
        {
            Append($"ERROR: {ex.Message}");
            Append("No firmware write, reset or flash operation was attempted.");
            Append(string.Empty);
        }
        finally
        {
            _send.Enabled = true;
            _input.Enabled = true;
            _input.Focus();
        }
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
        foreach (var name in key.GetValueNames())
            if (key.GetValue(name) is string port && !string.IsNullOrWhiteSpace(port)) result.Add(port);
        return result.OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private void Append(string text) => _output.AppendText($"[{DateTime.Now:HH:mm:ss}] {text}{Environment.NewLine}");
}
