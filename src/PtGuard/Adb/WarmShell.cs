using System.Diagnostics;
using PtGuard.Core.Adb;

namespace PtGuard.Adb;

/// <summary>
/// A persistent <c>adb shell</c> kept open so the reverse broadcast is a single line written to an
/// already-attached shell's stdin (~10 ms warm) instead of a fresh <c>adb shell am broadcast</c>
/// each time (35-44 ms cold). This is the latency-critical path: when an accidental passthrough is
/// detected, every millisecond before the screen snaps back is visible to the user.
/// </summary>
public sealed class WarmShell(AdbServer adb, string? serial) : IDisposable
{
    private Process? _proc;
    private readonly object _gate = new();

    public void Open()
    {
        lock (_gate)
        {
            if (IsAlive)
                return;
            _proc = adb.Start(AdbCommands.ShellArgs(serial));
        }
    }

    private bool IsAlive
    {
        get
        {
            try
            {
                return _proc is { HasExited: false };
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Fire the reverse broadcast NOW. Reopens the shell first if it died (headset slept, adb
    /// bounced), so a single dropped shell never silently disarms the guard.
    /// </summary>
    public void FireReverse()
    {
        lock (_gate)
        {
            if (!IsAlive)
                Open();

            // Expected failure: the shell can still race-close between the check and the write
            // (USB yank). Swallow IO errors here — the supervisor's reconnect path re-establishes
            // the shell; surfacing every transient pipe break would be noise, not signal.
            try
            {
                _proc!.StandardInput.WriteLine(AdbCommands.ReverseBroadcast);
                _proc.StandardInput.Flush();
            }
            catch (Exception ex) when (ex is IOException or InvalidOperationException or ObjectDisposedException)
            {
                _proc = null;
            }
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_proc is null)
                return;
            try
            {
                if (!_proc.HasExited)
                {
                    _proc.StandardInput.WriteLine("exit");
                    if (!_proc.WaitForExit(1000))
                        _proc.Kill(entireProcessTree: true);
                }
            }
            catch (Exception ex) when (ex is IOException or InvalidOperationException)
            {
                // Shell already gone.
            }
            _proc.Dispose();
            _proc = null;
        }
    }
}
