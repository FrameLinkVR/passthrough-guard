using System.Diagnostics;
using PtGuard.Core.Adb;

namespace PtGuard.Adb;

/// <summary>One adb invocation's result.</summary>
public readonly record struct AdbResult(int ExitCode, string StdOut, string StdErr)
{
    public bool Ok => ExitCode == 0;
}

/// <summary>
/// Owns a PRIVATE adb server (on <see cref="AdbLocator.PrivateServerPort"/>, not the default 5037)
/// so pt-guard never clashes with or hijacks the user's own adb — which FrameLink streaming or
/// Android Studio may already be driving. Every process this starts inherits the private port via
/// <c>ANDROID_ADB_SERVER_PORT</c>. Disposing kills only our server.
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
        psi.Environment[AdbLocator.ServerPortEnvVar] = AdbLocator.PrivateServerPort.ToString();
        return psi;
    }

    /// <summary>Start our private server explicitly (so the first real command isn't slowed by the
    /// cold server spin-up, and so we own the lifecycle).</summary>
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

    /// <summary>Start a long-lived adb process (logcat / persistent shell) on the private server.
    /// Caller owns the returned <see cref="Process"/> and its streams.</summary>
    public Process Start(IReadOnlyList<string> args)
    {
        var proc = new Process { StartInfo = BaseInfo(args), EnableRaisingEvents = true };
        proc.Start();
        return proc;
    }

    public void Dispose()
    {
        // Best-effort: tear down only OUR server (private port), never the user's default one.
        try
        {
            using var proc = new Process { StartInfo = BaseInfo(["kill-server"]) };
            proc.Start();
            proc.WaitForExit(3000);
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            // adb already gone — nothing to tear down.
        }
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
