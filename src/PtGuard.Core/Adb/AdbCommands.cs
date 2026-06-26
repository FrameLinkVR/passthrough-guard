namespace PtGuard.Core.Adb;

/// <summary>
/// The exact adb command lines the guard issues, in one place so the wire strings are reviewable
/// and unit-testable. Verified live on a Quest 3 — do not paraphrase the broadcast.
/// </summary>
public static class AdbCommands
{
    /// <summary>
    /// The reverse broadcast that snaps passthrough back off. Sent to a warm <c>adb shell</c> (one
    /// line written to its stdin) so the round-trip is ~10 ms instead of 35-44 ms cold. It toggles
    /// via QuickActionMenu, which logs "to value 0 from QuickActionMenu" — a line the detector
    /// ignores (To != 1), so the bounce can never re-trigger itself.
    /// </summary>
    public const string ReverseBroadcast =
        "am broadcast -a com.oculus.vrshell.intent.action.UPDATE_FULL_PASSTHROUGH " +
        "-n com.oculus.vrshell/.ShellControlBroadcastReceiver " +
        "--es toggle_point QuickActionMenu";

    /// <summary>Logcat invocation for the long-lived reader: clear nothing, dump the controller tag
    /// plus the catch-all (the controller logs under the default buffer). We filter in-process via
    /// <c>LogcatParser</c> rather than trusting a tag filter, so we never miss a renamed tag.</summary>
    public static string[] LogcatArgs(string? serial) =>
        serial is { Length: > 0 }
            ? ["-s", serial, "logcat", "-v", "brief"]
            : ["logcat", "-v", "brief"];

    /// <summary>Arguments to open the persistent reverse-path shell.</summary>
    public static string[] ShellArgs(string? serial) =>
        serial is { Length: > 0 } ? ["-s", serial, "shell"] : ["shell"];
}
