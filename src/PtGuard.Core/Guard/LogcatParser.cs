using System.Text.RegularExpressions;

namespace PtGuard.Core.Guard;

/// <summary>
/// Parses the one logcat line the guard cares about:
/// <code>FullPassthroughController: isInFullPassthrough is updated from 0 to value 1 from control point DoubleTap</code>
/// Everything else returns false. This is the single place that knows the wire shape of the log;
/// keep it pure so it is unit-tested without adb.
/// </summary>
public static partial class LogcatParser
{
    // Tolerant on the trailing token: the detect line says "from control point <CP>" while the
    // reverse-broadcast echo logs "... to value 0 from QuickActionMenu" (no "control point"). We
    // accept both so a format quirk never makes us miss a real transition — the To==1 filter, not
    // the prefix, is what prevents the feedback loop.
    [GeneratedRegex(
        @"isInFullPassthrough is updated from (?<from>\d+) to value (?<to>\d+) from (?:control point )?(?<cp>\S+)",
        RegexOptions.CultureInvariant)]
    private static partial Regex PassthroughRegex();

    /// <summary>
    /// Try to parse a single logcat line into a <see cref="PassthroughEvent"/>.
    /// Returns false (and a default event) for any line that is not a passthrough transition.
    /// </summary>
    public static bool TryParse(string line, out PassthroughEvent evt)
    {
        evt = default;
        if (string.IsNullOrEmpty(line))
            return false;

        var m = PassthroughRegex().Match(line);
        if (!m.Success)
            return false;

        if (!int.TryParse(m.Groups["from"].Value, out var from) ||
            !int.TryParse(m.Groups["to"].Value, out var to))
            return false;

        var raw = m.Groups["cp"].Value;
        evt = new PassthroughEvent(from, to, ControlPoints.Parse(raw), raw);
        return true;
    }
}
