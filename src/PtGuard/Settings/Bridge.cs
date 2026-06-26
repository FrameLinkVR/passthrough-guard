using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Web.WebView2.WinForms;
using PtGuard.Core.Guard;

namespace PtGuard.Settings;

/// <summary>Handlers the web UI drives through the bridge; wired to the runner/config by the app.</summary>
public sealed record BridgeHandlers(
    Func<SettingsSnapshot> Snapshot,
    Action<bool> SetPaused,
    Action<ControlPoint, bool> SetBounce,
    Action Reconnect,
    Func<Task> EnableWifi,
    Action ManualBounce,
    Action<bool> SetAutoStart,
    Action<bool> SetGuardOnlyWhileStreaming,
    Func<Task> Refresh);

/// <summary>
/// The WebView2 ↔ C# postMessage bridge. The web UI posts <c>{ type, ... }</c> messages; this maps
/// each to a handler and pushes a fresh <see cref="SettingsSnapshot"/> back as <c>{ type: "state" }</c>.
/// All UI state crosses as JSON — no native objects exposed to the page.
/// </summary>
public sealed class Bridge(WebView2 web, BridgeHandlers handlers)
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public void Attach() => web.WebMessageReceived += OnMessage;

    /// <summary>Push the current state to the page (call after any change).</summary>
    public void PushState()
    {
        var envelope = new { type = "state", payload = handlers.Snapshot() };
        web.CoreWebView2?.PostWebMessageAsJson(JsonSerializer.Serialize(envelope, Json));
    }

    private async void OnMessage(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs e)
    {
        var msg = JsonSerializer.Deserialize<InboundMessage>(e.WebMessageAsJson, Json);
        if (msg is null)
            return;

        switch (msg.Type)
        {
            case "ready": break; // initial PushState below
            case "pause": handlers.SetPaused(msg.Value ?? true); break;
            case "setBounce":
                if (TryPoint(msg.Point, out var cp))
                    handlers.SetBounce(cp, msg.Value ?? false);
                break;
            case "reconnect": handlers.Reconnect(); break;
            case "enableWifi": await handlers.EnableWifi(); break;
            case "manualBounce": handlers.ManualBounce(); break;
            case "setAutoStart": handlers.SetAutoStart(msg.Value ?? false); break;
            case "setGuardOnlyWhileStreaming": handlers.SetGuardOnlyWhileStreaming(msg.Value ?? false); break;
            case "refresh": await handlers.Refresh(); break;
        }

        PushState();
    }

    private static bool TryPoint(string? token, out ControlPoint point)
    {
        point = ControlPoints.Parse(token ?? "");
        return point is ControlPoint.DoubleTap or ControlPoint.ActionButton;
    }

    private sealed record InboundMessage(string Type, bool? Value, string? Point);
}
