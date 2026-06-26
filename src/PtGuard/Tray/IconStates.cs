using System.Drawing;
using System.Runtime.InteropServices;
using PtGuard.Guard;

namespace PtGuard.Tray;

/// <summary>
/// Renders the tray icon per guard state: the FrameLink brand mark with a status dot composited in
/// the corner, tinted with the brand signal colours (good / warn / err / accent). Generated icons
/// are cached per state — four small bitmaps for the whole session, and the native HICONs are
/// destroyed on dispose so the tray never leaks GDI handles.
/// </summary>
public sealed class IconStates(string iconPath) : IDisposable
{
    // FrameLink brand signal colours (verified tokens).
    private static readonly Color Good = ColorTranslator.FromHtml("#36d39b");
    private static readonly Color Warn = ColorTranslator.FromHtml("#f0b24e");
    private static readonly Color Err = ColorTranslator.FromHtml("#f26a5d");
    private static readonly Color Accent = ColorTranslator.FromHtml("#7c83ff");
    private static readonly Color Bg = ColorTranslator.FromHtml("#0c0e12");

    private readonly Icon _base = BrandIcon.Load(iconPath);
    private readonly Dictionary<GuardState, Icon> _cache = new();
    private readonly List<nint> _handles = new();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(nint handle);

    public Icon ForState(GuardState state)
    {
        if (_cache.TryGetValue(state, out var cached))
            return cached;

        var dot = state switch
        {
            GuardState.Guarding => Good,
            GuardState.Paused => Warn,
            GuardState.Connecting => Accent,
            _ => Err, // NoAdb / Disconnected / Unauthorized
        };

        var icon = Compose(dot);
        _cache[state] = icon;
        return icon;
    }

    private Icon Compose(Color dot)
    {
        using var bmp = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bmp))
        {
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.DrawIcon(_base, new Rectangle(0, 0, 32, 32));

            // status dot, bottom-right, ringed in the app background so it reads on any tray colour
            var d = new Rectangle(18, 18, 12, 12);
            using var ring = new SolidBrush(Bg);
            using var fill = new SolidBrush(dot);
            g.FillEllipse(ring, d.X - 2, d.Y - 2, d.Width + 4, d.Height + 4);
            g.FillEllipse(fill, d);
        }

        var handle = bmp.GetHicon();
        _handles.Add(handle);
        // Clone so the Icon owns a managed copy and the HICON can be destroyed on dispose.
        using var fromHandle = Icon.FromHandle(handle);
        return (Icon)fromHandle.Clone();
    }

    public void Dispose()
    {
        foreach (var icon in _cache.Values)
            icon.Dispose();
        foreach (var handle in _handles)
            DestroyIcon(handle);
        _base.Dispose();
    }
}
