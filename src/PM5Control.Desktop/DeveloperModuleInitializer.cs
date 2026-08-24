using System.Runtime.CompilerServices;

namespace PM5Control.Desktop;

internal static class DeveloperModuleInitializer
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        EventHandler? handler = null;
        handler = (_, _) =>
        {
            var form = Application.OpenForms.OfType<MainForm>().FirstOrDefault();
            if (form is null) return;
            Application.Idle -= handler;
            DeveloperUiPatcher.Install(form);
        };
        Application.Idle += handler;
    }
}
