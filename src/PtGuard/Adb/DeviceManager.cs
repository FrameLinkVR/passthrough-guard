using System.Text.RegularExpressions;

namespace PtGuard.Adb;

public enum DeviceState { Device, Unauthorized, Offline, Unknown }

/// <summary>One adb-visible device.</summary>
/// <param name="Serial">adb serial. A "host:5555" form is a Wi-Fi/tcpip endpoint; otherwise USB.</param>
public readonly record struct AdbDevice(string Serial, DeviceState State)
{
    public bool IsWifi => Serial.Contains(':');
    public bool IsUsb => !IsWifi;
    public bool IsReady => State == DeviceState.Device;
}

/// <summary>
/// Device discovery and the USB-to-Wi-Fi handoff. Mirrors the verified rig flow: pair over USB
/// first (RSA "Allow" prompt), then optionally lift to Wi-Fi with <c>tcpip 5555</c> + <c>connect</c>
/// so the guard survives unplugging and coexists with Wi-Fi streaming.
/// </summary>
public sealed partial class DeviceManager(AdbServer adb)
{
    private static readonly TimeSpan Quick = TimeSpan.FromSeconds(8);

    [GeneratedRegex(@"^(?<serial>\S+)\s+(?<state>device|unauthorized|offline)\b", RegexOptions.Multiline)]
    private static partial Regex DeviceLine();

    [GeneratedRegex(@"src\s+(?<ip>\d+\.\d+\.\d+\.\d+)")]
    private static partial Regex RouteSrc();

    public async Task<IReadOnlyList<AdbDevice>> ListAsync()
    {
        var r = await adb.RunAsync(["devices"], Quick);
        var devices = new List<AdbDevice>();
        foreach (Match m in DeviceLine().Matches(r.StdOut))
        {
            // The "List of devices attached" header has no state column, so the regex skips it.
            var state = m.Groups["state"].Value switch
            {
                "device" => DeviceState.Device,
                "unauthorized" => DeviceState.Unauthorized,
                "offline" => DeviceState.Offline,
                _ => DeviceState.Unknown,
            };
            devices.Add(new AdbDevice(m.Groups["serial"].Value, state));
        }
        return devices;
    }

    /// <summary>Pick the device to guard: a ready USB device first (the pairing path), then any
    /// ready Wi-Fi device, then the first device of any state so the UI can report its problem.</summary>
    public async Task<AdbDevice?> PickPrimaryAsync()
    {
        var devices = await ListAsync();
        if (devices.Count == 0)
            return null;

        return devices.FirstOrDefault(d => d is { IsReady: true, IsUsb: true }) is { Serial.Length: > 0 } usb ? usb
            : devices.FirstOrDefault(d => d.IsReady) is { Serial.Length: > 0 } ready ? ready
            : devices[0];
    }

    /// <summary>Ask the headset for its own Wi-Fi address (route source), so we never hard-code an IP.</summary>
    public async Task<string?> ResolveQuestIpAsync(string serial)
    {
        var r = await adb.RunAsync(["-s", serial, "shell", "ip", "route", "get", "1.1.1.1"], Quick);
        var m = RouteSrc().Match(r.StdOut);
        return m.Success ? m.Groups["ip"].Value : null;
    }

    /// <summary>
    /// Lift a USB-paired device onto Wi-Fi: <c>tcpip 5555</c>, learn its IP, then <c>connect</c>.
    /// Returns the "ip:5555" endpoint to persist, or null if the IP could not be learned.
    /// </summary>
    public async Task<string?> EnableWifiAsync(string usbSerial)
    {
        await adb.RunAsync(["-s", usbSerial, "tcpip", "5555"], Quick);
        await Task.Delay(1500); // adbd restarts in tcpip mode

        var ip = await ResolveQuestIpAsync(usbSerial);
        if (ip is null)
            return null;

        var endpoint = $"{ip}:5555";
        await ConnectAsync(endpoint);
        return endpoint;
    }

    /// <summary>(Re)connect to a persisted Wi-Fi endpoint. Idempotent; safe to retry on reconnect.</summary>
    public async Task<bool> ConnectAsync(string endpoint)
    {
        var ep = endpoint.Contains(':') ? endpoint : $"{endpoint}:5555";
        var r = await adb.RunAsync(["connect", ep], Quick);
        return r.Ok && r.StdOut.Contains("connected", StringComparison.OrdinalIgnoreCase);
    }
}
