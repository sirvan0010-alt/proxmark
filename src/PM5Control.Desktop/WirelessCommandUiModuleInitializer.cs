using System.Runtime.CompilerServices;
using PM5Control.Core.Bwm;

namespace PM5Control.Desktop;

internal static class WirelessCommandUiModuleInitializer
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        EventHandler? handler = null;
        handler = (_, _) =>
        {
            var form = Application.OpenForms.OfType<MainForm2>().FirstOrDefault();
            if (form is null) return;
            Application.Idle -= handler;
            Install(form);
        };
        Application.Idle += handler;
    }

    private static void Install(MainForm2 form)
    {
        var root = form.Controls.OfType<TableLayoutPanel>().FirstOrDefault();
        var tabs = root?.Controls.OfType<TabControl>().FirstOrDefault();
        if (tabs is null) return;

        if (!tabs.TabPages.Cast<TabPage>().Any(p => p.Text == "WIFI"))
        {
            var page = new TabPage("WIFI") { BackColor = Color.FromArgb(18, 20, 24), ForeColor = Color.White };
            var title = new Label
            {
                AutoSize = true,
                Location = new Point(24, 20),
                ForeColor = Color.White,
                Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold),
                Text = "PM5 BWM · Wi-Fi"
            };
            var info = new Label
            {
                AutoSize = false,
                Location = new Point(24, 52),
                Width = 900,
                Height = 120,
                ForeColor = Color.FromArgb(160, 168, 180),
                Text = "Current upstream BWM firmware exposes STA/connect, scan, Wi-Fi configuration and SNTP commands.\r\n" +
                       "SoftAP is disabled in the upstream build. Promiscuous/sniffer and raw 802.11 TX are chip/API capabilities but are not exposed by the current BWM command protocol.\r\n" +
                       "No firmware flashing is performed by this panel."
            };
            var capabilities = new Button { Text = "View Wi-Fi capabilities", AutoSize = true, Location = new Point(24, 190) };
            capabilities.Click += (_, _) => ShowCapabilityCatalog(form, "Wi-Fi");
            var commands = new Button { Text = "View Wi-Fi commands", AutoSize = true, Location = new Point(205, 190) };
            commands.Click += (_, _) => new BwmCommandCatalogForm("Wi-Fi").ShowDialog(form);
            page.Controls.Add(title);
            page.Controls.Add(info);
            page.Controls.Add(capabilities);
            page.Controls.Add(commands);
            tabs.TabPages.Add(page);
        }

        if (!tabs.TabPages.Cast<TabPage>().Any(p => p.Text == "BWM COMMANDS"))
        {
            var page = new TabPage("BWM COMMANDS") { BackColor = Color.FromArgb(18, 20, 24), ForeColor = Color.White };
            var title = new Label
            {
                AutoSize = true,
                Location = new Point(24, 20),
                ForeColor = Color.White,
                Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold),
                Text = $"BWM command catalog · {BwmCommandCatalog.All.Count} commands"
            };
            var info = new Label
            {
                AutoSize = false,
                Location = new Point(24, 52),
                Width = 900,
                Height = 100,
                ForeColor = Color.FromArgb(160, 168, 180),
                Text = "This list mirrors the current upstream main/app_com_defs.h command numbering.\r\n" +
                       "It is an evidence catalog, not a raw command terminal. Commands are not sent until a verified PM5↔BWM transport is implemented.\r\n" +
                       "The BWM firmware command UART defaults to 460800 baud; stock PM5 ARM↔BWM bridging is currently documented upstream as incomplete."
            };
            var open = new Button { Text = "Open complete BWM command list", AutoSize = true, Location = new Point(24, 170) };
            open.Click += (_, _) => new BwmCommandCatalogForm().ShowDialog(form);
            page.Controls.Add(title);
            page.Controls.Add(info);
            page.Controls.Add(open);
            tabs.TabPages.Add(page);
        }
    }

    private static void ShowCapabilityCatalog(MainForm2 owner, string category)
    {
        var items = BwmWirelessCapabilityCatalog.All.Where(x => x.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
        var text = string.Join(Environment.NewLine + Environment.NewLine, items.Select(x => $"{x.Name}\r\nStatus: {x.Status}\r\n{x.Note}\r\nEvidence: {x.Evidence}"));
        MessageBox.Show(owner, text, $"{category} capabilities", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
