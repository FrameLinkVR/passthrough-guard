using PtGuard.Core.Adb;

namespace PtGuard.Config;

/// <summary>
/// Resolves every on-disk location the app uses: the bundled adb, the WebView2 settings assets,
/// the tray icon, and the per-user config under %APPDATA%. One place so the layout is auditable.
/// </summary>
public sealed class AppPaths
{
    public string ExeDir { get; }
    public string BundledAdb { get; }
    public string WebDir { get; }
    public string IconPath { get; }
    public string ConfigDir { get; }
    public string ConfigFile { get; }

    private AppPaths(string exeDir, string configDir)
    {
        ExeDir = exeDir;
        BundledAdb = Path.Combine(exeDir, "platform-tools", "adb.exe");
        WebDir = Path.Combine(exeDir, "web");
        IconPath = Path.Combine(exeDir, "resources", "framelink.ico");
        ConfigDir = configDir;
        ConfigFile = Path.Combine(configDir, "config.json");
    }

    public static AppPaths Resolve()
    {
        var exeDir = AppContext.BaseDirectory;
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var configDir = Path.Combine(appData, "FrameLink", "pt-guard");
        return new AppPaths(exeDir, configDir);
    }

    /// <summary>
    /// Resolve the adb to drive: the bundled one first, else an adb found on PATH. Null means
    /// "no adb" — the app then tells the user the install is incomplete (rig/release step).
    /// </summary>
    public string? ResolveAdb() =>
        AdbLocator.Resolve(BundledAdb, FindOnPath("adb.exe"), File.Exists);

    private static string? FindOnPath(string exe)
    {
        var path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(path))
            return null;

        foreach (var dir in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = Path.Combine(dir.Trim(), exe);
            if (File.Exists(candidate))
                return candidate;
        }
        return null;
    }
}
