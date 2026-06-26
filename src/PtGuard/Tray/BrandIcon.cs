using System.Drawing;

namespace PtGuard.Tray;

/// <summary>
/// Loads the FrameLink brand icon for runtime use. Prefers the loose <c>resources\framelink.ico</c>
/// the installer lays down (full multi-size icon); falls back to the icon embedded in the exe
/// (via <c>ApplicationIcon</c>) so a bare single-file publish still has a brand mark, and finally
/// to the system app icon. Always returns an owned, disposable icon.
/// </summary>
public static class BrandIcon
{
    public static Icon Load(string iconPath)
    {
        if (File.Exists(iconPath))
            return new Icon(iconPath);

        var exe = Environment.ProcessPath;
        if (exe is not null)
        {
            var embedded = Icon.ExtractAssociatedIcon(exe);
            if (embedded is not null)
                return embedded;
        }

        return (Icon)SystemIcons.Application.Clone();
    }
}
