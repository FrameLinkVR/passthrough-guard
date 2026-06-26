namespace PtGuard.Settings;

/// <summary>One adb device as the settings UI shows it.</summary>
public sealed record DeviceView(string Serial, string State, string Kind);

/// <summary>
/// The full state the WebView2 settings UI renders. Serialized to JSON and pushed over the
/// postMessage bridge whenever anything changes, so the web layer stays a pure view.
/// </summary>
public sealed record SettingsSnapshot
{
    public string GuardState { get; init; } = "Disconnected";
    public string Detail { get; init; } = "";
    public int BounceCount { get; init; }
    public string? Serial { get; init; }
    public bool Paused { get; init; }
    public bool BounceDoubleTap { get; init; }
    public bool BounceActionButton { get; init; }
    public bool AutoStart { get; init; }
    public bool GuardOnlyWhileStreaming { get; init; }
    public string? WifiIp { get; init; }
    public IReadOnlyList<DeviceView> Devices { get; init; } = Array.Empty<DeviceView>();
}
