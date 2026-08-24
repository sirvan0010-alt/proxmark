using System.Text.Json;

namespace PM5Control.Desktop;

internal sealed class AppSettings
{
    public string EspIpAddress { get; set; } = "";
    public int EspTcpPort { get; set; } = 7891;

    // Ten persistent user-defined read-only debug buttons.
    public string[] CustomButtonNames { get; set; } = Enumerable.Range(1, 10).Select(i => $"Button {i}").ToArray();
    public string[] CustomButtonCommands { get; set; } = new string[10];
    public bool[] CustomButtonEnabled { get; set; } = Enumerable.Repeat(true, 10).ToArray();

    private static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "PM5ControlCenter", "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var loaded = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath));
                if (loaded is not null)
                {
                    loaded.NormalizeCustomButtons();
                    return loaded;
                }
            }
        }
        catch { }
        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            NormalizeCustomButtons();
            var dir = Path.GetDirectoryName(FilePath)!;
            Directory.CreateDirectory(dir);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }

    private void NormalizeCustomButtons()
    {
        CustomButtonNames = Normalize(CustomButtonNames, i => $"Button {i + 1}");
        CustomButtonCommands = Normalize(CustomButtonCommands, _ => "");
        CustomButtonEnabled = Normalize(CustomButtonEnabled, _ => true);
    }

    private static string[] Normalize(string[]? source, Func<int, string> fallback)
    {
        var result = new string[10];
        for (var i = 0; i < result.Length; i++) result[i] = source is { Length: > 0 } && i < source.Length && source[i] is not null ? source[i] : fallback(i);
        return result;
    }

    private static bool[] Normalize(bool[]? source, Func<int, bool> fallback)
    {
        var result = new bool[10];
        for (var i = 0; i < result.Length; i++) result[i] = source is { Length: > 0 } && i < source.Length ? source[i] : fallback(i);
        return result;
    }
}
