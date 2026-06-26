using PtGuard.Adb;
using PtGuard.Core.Config;
using PtGuard.Core.Guard;

namespace PtGuard.Guard;

public enum GuardState { NoAdb, Disconnected, Unauthorized, Connecting, Guarding, Paused }

/// <summary>Snapshot the tray renders. Immutable so it crosses threads safely.</summary>
public readonly record struct GuardStatus(GuardState State, string Detail, int BounceCount, string? Serial);

/// <summary>
/// The supervisor: connects to the headset, arms the guard, reads logcat into the pure
/// <see cref="GuardEngine"/>, and fires the warm-shell reverse broadcast on a bounce. Owns all
/// resilience — reconnect with backoff on headset sleep / cable pull, logcat auto-restart — and
/// publishes a <see cref="GuardStatus"/> for the tray. Pure decision logic stays in Core; this is
/// the IO/lifecycle shell around it.
/// </summary>
public sealed class GuardRunner : IDisposable
{
    private readonly AdbServer _adb;
    private readonly DeviceManager _devices;
    private readonly IClock _clock;

    private GuardConfig _config;
    private GuardEngine _engine;

    private CancellationTokenSource? _superviseCts;
    private TaskCompletionSource? _reconnectSignal;
    private LogcatStream? _logcat;
    private WarmShell? _warmShell;

    private volatile bool _paused;
    private DateTimeOffset _graceUntil = DateTimeOffset.MinValue;
    private int _bounceCount;
    private string? _serial;
    private GuardState _state = GuardState.Disconnected;

    public GuardRunner(AdbServer adb, DeviceManager devices, GuardConfig config, IClock clock)
    {
        _adb = adb;
        _devices = devices;
        _config = config;
        _clock = clock;
        _engine = new GuardEngine(config);
    }

    public event Action<GuardStatus>? StatusChanged;

    public GuardStatus Status => new(_state, DetailFor(_state), _bounceCount, _serial);

    public void Start()
    {
        _superviseCts = new CancellationTokenSource();
        _ = SuperviseAsync(_superviseCts.Token);
    }

    // ---- user controls -------------------------------------------------------------------

    public void SetPaused(bool paused)
    {
        _paused = paused;
        if (!paused)
            _engine.ResetDebounce();
        Publish(paused ? GuardState.Paused : _serial is null ? GuardState.Disconnected : GuardState.Guarding);
    }

    public bool IsPaused => _paused;

    /// <summary>Suspend bouncing for <see cref="GuardConfig.GraceSeconds"/> so an intentional
    /// passthrough sticks, then re-arm automatically.</summary>
    public void StartGrace()
    {
        _graceUntil = _clock.Now.AddSeconds(_config.GraceSeconds);
        Publish(_state);
    }

    /// <summary>"Get me back to VR" — fire the reverse broadcast immediately, ignoring debounce.</summary>
    public void ManualBounce() => _warmShell?.FireReverse();

    /// <summary>Cut any backoff wait short and retry the connection now.</summary>
    public void ReconnectNow() => _reconnectSignal?.TrySetResult();

    public void UpdateConfig(GuardConfig config)
    {
        _config = config;
        _engine = new GuardEngine(config); // bounce set / debounce may have changed
    }

    // ---- supervision loop ----------------------------------------------------------------

    private async Task SuperviseAsync(CancellationToken ct)
    {
        var backoff = TimeSpan.FromSeconds(1);
        while (!ct.IsCancellationRequested)
        {
            // Prefer a remembered Wi-Fi endpoint so an unplugged-but-paired headset re-arms hands-free.
            if (_config.WifiDeviceIp is { Length: > 0 } wifi)
                await _devices.ConnectAsync(wifi);

            var device = await _devices.PickPrimaryAsync();
            if (device is not { IsReady: true })
            {
                _serial = null;
                Publish(device?.State == DeviceState.Unauthorized ? GuardState.Unauthorized : GuardState.Disconnected);
                await BackoffAsync(backoff, ct);
                backoff = Grow(backoff);
                continue;
            }

            await ArmAndRunAsync(device.Value.Serial, ct);
            backoff = TimeSpan.FromSeconds(1); // healthy connection resets the backoff
        }
    }

    private async Task ArmAndRunAsync(string serial, CancellationToken ct)
    {
        _serial = serial;
        _warmShell = new WarmShell(_adb, serial);
        _warmShell.Open();

        var dead = new TaskCompletionSource();
        _logcat = new LogcatStream(_adb, serial);
        _logcat.LineReceived += OnLogcatLine;
        _logcat.Exited += () => dead.TrySetResult();
        _logcat.Start();

        Publish(_paused ? GuardState.Paused : GuardState.Guarding);

        using (ct.Register(() => dead.TrySetResult()))
            await dead.Task; // returns when logcat dies or we shut down

        _logcat.LineReceived -= OnLogcatLine;
        _logcat.Dispose();
        _logcat = null;
        _warmShell.Dispose();
        _warmShell = null;
    }

    private void OnLogcatLine(string line)
    {
        if (!Armed)
            return;

        if (_engine.Process(line, _clock.Now) == GuardDecision.Bounce)
        {
            _warmShell?.FireReverse();
            _bounceCount++;
            Publish(_state);
        }
    }

    /// <summary>Armed = connected, not paused, and past any active grace window.</summary>
    private bool Armed => !_paused && _clock.Now >= _graceUntil;

    private async Task BackoffAsync(TimeSpan delay, CancellationToken ct)
    {
        _reconnectSignal = new TaskCompletionSource();
        var timeout = Task.Delay(delay, ct);
        await Task.WhenAny(timeout, _reconnectSignal.Task);
    }

    private static TimeSpan Grow(TimeSpan b) =>
        TimeSpan.FromSeconds(Math.Min(b.TotalSeconds * 2, 15));

    // ---- status --------------------------------------------------------------------------

    private void Publish(GuardState state)
    {
        _state = state;
        StatusChanged?.Invoke(Status);
    }

    private string DetailFor(GuardState state) => state switch
    {
        GuardState.NoAdb => "adb not found — reinstall to repair",
        GuardState.Disconnected => "No headset — click to connect",
        GuardState.Unauthorized => "Headset unauthorized — tap Allow in the headset",
        GuardState.Connecting => "Connecting to headset…",
        GuardState.Paused => "Paused",
        GuardState.Guarding => _bounceCount == 1 ? "Guarding · 1 bounce" : $"Guarding · {_bounceCount} bounces",
        _ => string.Empty,
    };

    public void Dispose()
    {
        _superviseCts?.Cancel();
        _logcat?.Dispose();
        _warmShell?.Dispose();
        _superviseCts?.Dispose();
    }
}
