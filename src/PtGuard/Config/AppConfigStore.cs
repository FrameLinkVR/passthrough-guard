using System.Text.Json;
using System.Text.Json.Serialization;
using PtGuard.Core.Config;

namespace PtGuard.Config;

/// <summary>
/// Loads and saves <see cref="GuardConfig"/> as JSON under %APPDATA%\FrameLink\pt-guard\.
/// A missing or unreadable file yields safe defaults — config corruption never blocks the guard.
/// </summary>
public sealed class AppConfigStore(string configFile)
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public GuardConfig Load()
    {
        if (!File.Exists(configFile))
            return new GuardConfig();

        // Expected error (first run / hand-edited file): fall back to defaults, don't crash the tray.
        try
        {
            var json = File.ReadAllText(configFile);
            return JsonSerializer.Deserialize<GuardConfig>(json, Options) ?? new GuardConfig();
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            return new GuardConfig();
        }
    }

    public void Save(GuardConfig config)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(configFile)!);
        File.WriteAllText(configFile, JsonSerializer.Serialize(config, Options));
    }
}
