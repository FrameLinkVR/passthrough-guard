namespace PtGuard.Core.Config;

using PtGuard.Core.Guard;

/// <summary>
/// User-facing configuration for the guard. Plain data so it serializes to JSON
/// (%APPDATA%\FrameLink\pt-guard\config.json — IO lives in the WinForms app) and is trivially
/// unit-tested. Defaults encode the verified-safe behaviour: always armed, DoubleTap-only.
/// </summary>
public sealed class GuardConfig
{
    /// <summary>Control points that trigger a reverse bounce. Default: DoubleTap only.</summary>
    /// <remarks>
    /// QuickActionMenu is deliberately NOT here: it is how a user intentionally opens passthrough,
    /// and it is also the toggle point our own reverse broadcast uses — leaving it out is what keeps
    /// the guard from fighting deliberate actions or itself.
    /// </remarks>
    public HashSet<ControlPoint> BounceSet { get; set; } = new() { ControlPoint.DoubleTap };

    /// <summary>Suppress repeat detect lines within this window (verified ~250 ms on the rig).</summary>
    public int DebounceMs { get; set; } = 250;

    /// <summary>When set, the guard arms only while a FrameLink/SteamVR session is detected.
    /// Default false = always armed (safe because the default bounce set is DoubleTap-only).</summary>
    public bool GuardOnlyWhileStreaming { get; set; }

    /// <summary>Last known Quest IP for Wi-Fi auto-reconnect (set after a USB <c>tcpip</c> handoff).</summary>
    public string? WifiDeviceIp { get; set; }

    /// <summary>Launch with Windows (mirrors the HKCU Run key; the registry is the source of truth).</summary>
    public bool AutoStart { get; set; }

    /// <summary>Seconds the grace hotkey suspends the guard so an intentional passthrough sticks.</summary>
    public int GraceSeconds { get; set; } = 8;
}
