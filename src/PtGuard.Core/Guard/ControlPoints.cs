namespace PtGuard.Core.Guard;

/// <summary>
/// The control points the Quest shell reports when full passthrough turns on. These are the
/// "from control point &lt;CP&gt;" tokens in the FullPassthroughController logcat line.
/// </summary>
/// <remarks>
/// Verified on a Quest 3: the accidental wheel/pedal-vibration trigger arrives as
/// <see cref="DoubleTap"/>; an intentional menu toggle arrives as <see cref="QuickActionMenu"/>.
/// The guard bounces only the configured set (default: DoubleTap), so an intentional
/// QuickActionMenu toggle is left alone — and our own reverse broadcast (which we send via
/// QuickActionMenu) is never in the default bounce set, reinforcing the no-feedback-loop rule.
/// </remarks>
public enum ControlPoint
{
    DoubleTap,
    ActionButton,
    QuickActionMenu,
    Unknown,
}

public static class ControlPoints
{
    /// <summary>The control point our reverse broadcast uses (and which the filter ignores).</summary>
    public const string ReverseTogglePoint = "QuickActionMenu";

    /// <summary>Parse a logcat control-point token into the enum. Case-insensitive; unknown tokens
    /// map to <see cref="ControlPoint.Unknown"/> so they are never accidentally bounced.</summary>
    public static ControlPoint Parse(string token) => token.Trim() switch
    {
        var t when t.Equals("DoubleTap", StringComparison.OrdinalIgnoreCase) => ControlPoint.DoubleTap,
        var t when t.Equals("ActionButton", StringComparison.OrdinalIgnoreCase) => ControlPoint.ActionButton,
        var t when t.Equals("QuickActionMenu", StringComparison.OrdinalIgnoreCase) => ControlPoint.QuickActionMenu,
        _ => ControlPoint.Unknown,
    };
}
