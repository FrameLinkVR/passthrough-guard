using Microsoft.Win32;

namespace PtGuard.Config;

/// <summary>
/// Opt-in "start with Windows" via the per-user Run key (HKCU, no admin needed). The registry
/// value is the source of truth; the config flag just mirrors it for the UI.
/// </summary>
public static class AutoStart
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "FrameLinkPassthroughGuard";

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey);
        return key?.GetValue(ValueName) is string;
    }

    public static void Set(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKey);
        if (key is null)
            return;

        if (enabled)
            key.SetValue(ValueName, $"\"{Environment.ProcessPath}\"");
        else
            key.DeleteValue(ValueName, throwOnMissingValue: false);
    }
}
