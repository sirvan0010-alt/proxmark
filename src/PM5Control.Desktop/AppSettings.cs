using System.Text.Json;

namespace PM5Control.Desktop;

/// <summary>
/// Small local settings store (per-Windows-user AppData), so the ESP32/BWM Wi-Fi
/// endpoint the person types in Settings survives across app restarts.
/// This file only stores connection *hints* the user typed - it never stores
/// anything sent to or received from the device.
/// </summary>
internal sealed class AppSettings
{
    public string EspIpAddress { get; set; } = "";
    public int EspTcpPort { get; set; } = 7891; // default app_tcp_server.c DEFAULT_SERVER_PORT

    private static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "PM5ControlCenter", "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json);
                if (loaded is not null) return loaded;
            }
        }
        catch
        {
            // Corrupt or unreadable settings file - fall back to defaults rather than crash.
        }
        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(FilePath)!;
            Directory.CreateDirectory(dir);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch
        {
            // Best-effort only; failing to persist settings should never crash the UI.
        }
    }
}
