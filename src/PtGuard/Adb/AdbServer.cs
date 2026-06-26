using System.Diagnostics;

namespace PtGuard.Adb;

/// <summary>One adb invocation's result.</summary>
public readonly record struct AdbResult(int ExitCode, string StdOut, string StdErr)
{
    public bool Ok => ExitCode == 0;
}

/// <summary>
/// Runs our bundled <c>adb</c> as a polite CLIENT of the standard shared adb server (default port
/// 5037) — the model adb itself is built around: one server, many clients. We deliberately do NOT
/// run our own server and never <c>kill-server</c>, so we coexist with whatever already owns the
/// USB device (Meta Quest Developer Hub, scrcpy, Android Studio, FrameLink). This matters: a USB
/// device can be claimed by only ONE adb server, so a separate private server would be permanently
/// blind to a Quest another tool is holding. <see cref="StartServerAsync"/> just ensures a shared
/// server exists (a no-op if one is already up) — using whichever adb the user already has, or
/// ours as a fallback when they have none.
/// </summary>
public sealed class AdbServer(string adbPath) : IDisposable
{
    public string AdbPath { get; } = adbPath;

    private ProcessStartInfo BaseInfo(IReadOnlyList<string> args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = AdbPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var a in args)
            psi.ArgumentList.Add(a);
        // No ANDROID_ADB_SERVER_PORT override: we use the default shared server (5037) so we see the
        // same devices every other adb tool does, instead of a private server that can't claim a
        // USB Quest another server already owns.
        return psi;
    }

    /// <summary>Ensure a shared adb server is up so the first real command isn't slowed by a cold
    /// spin-up. A no-op if another tool (MQDH, FrameLink, …) already started one. We start it but
    /// never kill it.</summary>
    public Task StartServerAsync() => RunAsync(["start-server"], TimeSpan.FromSeconds(10));

    /// <summary>Run an adb command to completion, capturing output. Never throws on a non-zero
    /// exit — callers inspect <see cref="AdbResult.Ok"/>.</summary>
    public async Task<AdbResult> RunAsync(IReadOnlyList<string> args, TimeSpan timeout)
    {
        using var proc = new Process { StartInfo = BaseInfo(args) };
        proc.Start();

        var stdout = proc.StandardOutput.ReadToEndAsync();
        var stderr = proc.StandardError.ReadToEndAsync();
        using var cts = new CancellationTokenSource(timeout);

        try
        {
            await proc.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            TryKill(proc);
            return new AdbResult(-1, string.Empty, "adb command timed out");
        }

        return new AdbResult(proc.ExitCode, (await stdout).Trim(), (await stderr).Trim());
    }

    /// <summary>Start a long-lived adb process (logcat / persistent shell) on the shared server.
    /// Caller owns the returned <see cref="Process"/> and its streams.</summary>
    public Process Start(IReadOnlyList<string> args)
    {
        var proc = new Process { StartInfo = BaseInfo(args), EnableRaisingEvents = true };
        proc.Start();
        return proc;
    }

    public void Dispose()
    {
        // Nothing to tear down: we are a client of the SHARED adb server, not its owner. We must
        // NOT kill-server — Meta Quest Developer Hub, FrameLink, scrcpy and others rely on it. Our
        // long-lived logcat/shell processes are owned and disposed by their callers
        // (LogcatStream / WarmShell).
    }

    private static void TryKill(Process proc)
    {
        try
        {
            if (!proc.HasExited)
                proc.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
            // Already exited between the check and the kill.
        }
    }
}
