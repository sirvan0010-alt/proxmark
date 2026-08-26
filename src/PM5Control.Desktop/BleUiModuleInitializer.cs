using System.Runtime.CompilerServices;

namespace PM5Control.Desktop;

internal static class BleUiModuleInitializer
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
        if (tabs is null || tabs.TabPages.Cast<TabPage>().Any(p => p.Text == "BLUETOOTH")) return;

        var page = new TabPage("BLUETOOTH") { BackColor = Color.FromArgb(18, 20, 24), ForeColor = Color.White };
        var button = new Button { Text = "Open PM5 Bluetooth / BLE panel", AutoSize = true, Location = new Point(24, 24) };
        var info = new Label
        {
            AutoSize = false,
            Width = 850,
            Height = 110,
            Location = new Point(24, 70),
            ForeColor = Color.FromArgb(145, 153, 164),
            Text = "Uses the PM5 BWM BLE SPP transport (service 0xAE86 / characteristic 0xAE88).\r\n" +
                   "The normal PM3-NG diagnostic frames are carried unchanged over BLE.\r\n" +
                   "Only the existing read-only probe is enabled: VERSION, CAPABILITIES, STATUS and PING.\r\n" +
                   "No firmware flashing, reset or destructive BWM operation is exposed."
        };
        button.Click += (_, _) =>
        {
            using var dialog = new BleControlForm();
            dialog.ShowDialog(form);
        };
        page.Controls.Add(button);
        page.Controls.Add(info);
        tabs.TabPages.Add(page);
    }
}
