using PtGuard.Adb;
using PtGuard.Config;
using PtGuard.Core.Guard;
using PtGuard.Guard;
using PtGuard.Settings;
using PtGuard.Tray;

namespace PtGuard;

/// <summary>
/// Wires the whole tray app together: config + adb + guard runner + tray + WebView2 settings. Owns
/// the lifetime, marshals background status changes onto the UI thread, and routes every user
/// action (tray menu or web UI) into the runner/config. No guard logic lives here — it is the
/// composition root.
/// </summary>
public sealed class TrayAppContext : ApplicationContext
{
    private readonly AppPaths _paths;
    private readonly AppConfigStore _store;
    private readonly Core.Config.GuardConfig _config;
    private readonly IconStates _icons;
    private readonly TrayIcon _tray;
    private readonly SettingsWindow _settings;
    private readonly SynchronizationContext _ui;

    private readonly AdbServer? _adb;
    private readonly DeviceManager? _devices;
    private readonly GuardRunner? _runner;
    private IReadOnlyList<DeviceView> _deviceViews = Array.Empty<DeviceView>();

    public TrayAppContext()
    {
        _paths = AppPaths.Resolve();
        _store = new AppConfigStore(_paths.ConfigFile);
        _config = _store.Load();
        _config.AutoStart = AutoStart.IsEnabled(); // registry is the source of truth

        var adbPath = _paths.ResolveAdb();
        if (adbPath is not null)
        {
            _adb = new AdbServer(adbPath);
            _devices = new DeviceManager(_adb);
            _runner = new GuardRunner(_adb, _devices, _config, SystemClock.Instance);
        }

        _icons = new IconStates(_paths.IconPath);
        _tray = new TrayIcon(_icons, BuildTrayCallbacks());
        _settings = new SettingsWindow(_paths, BuildBridgeHandlers());
        _ui = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();

        if (_runner is not null)
        {
            _runner.StatusChanged += OnStatusChanged;
            _ = _adb!.StartServerAsync();
            _runner.Start();
        }

        _tray.SetBounceChecks(_config.BounceSet);
        _tray.SetAutoStartChecked(_config.AutoStart);
        _tray.UpdateStatus(_runner?.Status ?? new GuardStatus(GuardState.NoAdb, "adb not found — reinstall to repair", 0, null));
        _tray.Show();
    }

    // ---- status fan-out (background → UI thread) -----------------------------------------

    private void OnStatusChanged(GuardStatus status) =>
        _ui.Post(_ =>
        {
            _tray.UpdateStatus(status);
            _settings.PushState();
        }, null);

    // ---- tray menu wiring ----------------------------------------------------------------

    private TrayCallbacks BuildTrayCallbacks() => new(
        TogglePause: () => _runner?.SetPaused(!_runner.IsPaused),
        ToggleBounce: cp => ToggleBounce(cp),
        Reconnect: () => _runner?.ReconnectNow(),
        OpenSettings: () => _settings.ShowWindow(),
        ToggleAutoStart: () => SetAutoStart(!_config.AutoStart),
        GetMeBackToVr: () => _runner?.ManualBounce(),
        Quit: Quit);

    // ---- web UI wiring -------------------------------------------------------------------

    private BridgeHandlers BuildBridgeHandlers() => new(
        Snapshot: BuildSnapshot,
        SetPaused: p => _runner?.SetPaused(p),
        SetBounce: (cp, on) => SetBounce(cp, on),
        Reconnect: () => _runner?.ReconnectNow(),
        EnableWifi: EnableWifiAsync,
        ManualBounce: () => _runner?.ManualBounce(),
        SetAutoStart: SetAutoStart,
        SetGuardOnlyWhileStreaming: on => { _config.GuardOnlyWhileStreaming = on; Persist(); },
        Refresh: RefreshDevicesAsync);

    // ---- actions -------------------------------------------------------------------------

    private void ToggleBounce(ControlPoint cp) => SetBounce(cp, !_config.BounceSet.Contains(cp));

    private void SetBounce(ControlPoint cp, bool on)
    {
        if (on)
            _config.BounceSet.Add(cp);
        else
            _config.BounceSet.Remove(cp);
        // Never leave the guard fully disarmed by accident: keep DoubleTap if the set empties.
        if (_config.BounceSet.Count == 0)
            _config.BounceSet.Add(ControlPoint.DoubleTap);

        _runner?.UpdateConfig(_config);
        _tray.SetBounceChecks(_config.BounceSet);
        Persist();
    }

    private void SetAutoStart(bool on)
    {
        AutoStart.Set(on);
        _config.AutoStart = AutoStart.IsEnabled();
        _tray.SetAutoStartChecked(_config.AutoStart);
        Persist();
    }

    private async Task EnableWifiAsync()
    {
        if (_devices is null)
            return;
        var device = await _devices.PickPrimaryAsync();
        if (device is not { IsReady: true, IsUsb: true })
            return; // Wi-Fi handoff needs a USB-paired headset first

        var endpoint = await _devices.EnableWifiAsync(device.Value.Serial);
        if (endpoint is not null)
        {
            _config.WifiDeviceIp = endpoint;
            Persist();
            _runner?.ReconnectNow();
        }
        await RefreshDevicesAsync();
    }

    private async Task RefreshDevicesAsync()
    {
        if (_devices is null)
            return;
        var list = await _devices.ListAsync();
        _deviceViews = list.Select(d => new DeviceView(
            d.Serial, d.State.ToString(), d.IsWifi ? "Wi-Fi" : "USB")).ToList();
    }

    private SettingsSnapshot BuildSnapshot()
    {
        var s = _runner?.Status ?? new GuardStatus(GuardState.NoAdb, "adb not found — reinstall to repair", 0, null);
        return new SettingsSnapshot
        {
            GuardState = s.State.ToString(),
            Detail = s.Detail,
            BounceCount = s.BounceCount,
            Serial = s.Serial,
            Paused = _runner?.IsPaused ?? false,
            BounceDoubleTap = _config.BounceSet.Contains(ControlPoint.DoubleTap),
            BounceActionButton = _config.BounceSet.Contains(ControlPoint.ActionButton),
            AutoStart = _config.AutoStart,
            GuardOnlyWhileStreaming = _config.GuardOnlyWhileStreaming,
            WifiIp = _config.WifiDeviceIp,
            Devices = _deviceViews,
        };
    }

    private void Persist() => _store.Save(_config);

    private void Quit()
    {
        _runner?.Dispose();
        _adb?.Dispose(); // no-op: we share the system adb server and never kill it
        _tray.Dispose();
        _settings.Dispose();
        _icons.Dispose();
        ExitThread();
    }
}
