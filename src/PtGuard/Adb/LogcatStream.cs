using System.Diagnostics;
using PtGuard.Core.Adb;

namespace PtGuard.Adb;

/// <summary>
/// A long-lived <c>adb logcat</c> reader. Streams stdout line-by-line to <see cref="LineReceived"/>
/// on a background task and raises <see cref="Exited"/> when the process dies (headset sleep,
/// cable pull, adb restart) so the supervisor (<c>GuardRunner</c>) can reconnect. This class does
/// NOT restart itself — restart policy/backoff lives in the supervisor.
/// </summary>
public sealed class LogcatStream(AdbServer adb, string? serial) : IDisposable
{
    private Process? _proc;
    private CancellationTokenSource? _cts;

    public event Action<string>? LineReceived;
    public event Action? Exited;

    public void Start()
    {
        Stop();
        _cts = new CancellationTokenSource();
        _proc = adb.Start(AdbCommands.LogcatArgs(serial));
        _ = PumpAsync(_proc, _cts.Token);
    }

    private async Task PumpAsync(Process proc, CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var line = await proc.StandardOutput.ReadLineAsync(ct);
                if (line is null)
                    break; // stream closed → process gone
                LineReceived?.Invoke(line);
            }
        }
        catch (OperationCanceledException)
        {
            return; // deliberate Stop()/Dispose() — not a failure
        }

        if (!ct.IsCancellationRequested)
            Exited?.Invoke();
    }

    public void Stop()
    {
        _cts?.Cancel();
        if (_proc is { } p)
        {
            try
            {
                if (!p.HasExited)
                    p.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
                // Already exited.
            }
            p.Dispose();
            _proc = null;
        }
        _cts?.Dispose();
        _cts = null;
    }

    public void Dispose() => Stop();
}
