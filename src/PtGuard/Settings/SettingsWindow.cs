using System.Drawing;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using PtGuard.Config;

namespace PtGuard.Settings;

/// <summary>
/// Small, dark, FrameLink-branded settings window hosting the WebView2 UI. The page is served from
/// the on-disk <c>web/</c> folder via a virtual host (so @font-face and the design-system stylesheet
/// load with a real origin, no CDN). Closing hides the window so it reopens instantly from the tray.
/// </summary>
public sealed class SettingsWindow : Form
{
    private const string VirtualHost = "ptguard.local";
    private readonly WebView2 _web = new() { Dock = DockStyle.Fill };
    private readonly AppPaths _paths;
    private readonly BridgeHandlers _handlers;
    private Bridge? _bridge;
    private bool _ready;

    public SettingsWindow(AppPaths paths, BridgeHandlers handlers)
    {
        _paths = paths;
        _handlers = handlers;

        Text = "FrameLink Passthrough Guard";
        ClientSize = new Size(540, 720);
        MinimumSize = new Size(460, 560);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = ColorTranslator.FromHtml("#0c0e12"); // no white flash before the page paints
        ShowInTaskbar = true;
        Icon = Tray.BrandIcon.Load(_paths.IconPath);

        Controls.Add(_web);
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        var dataDir = Path.Combine(_paths.ConfigDir, "webview2");
        Directory.CreateDirectory(dataDir);
        var env = await CoreWebView2Environment.CreateAsync(userDataFolder: dataDir);
        await _web.EnsureCoreWebView2Async(env);

        var core = _web.CoreWebView2;
        core.Settings.AreDefaultContextMenusEnabled = false;
        core.Settings.IsZoomControlEnabled = false;
        core.Settings.AreDevToolsEnabled = false;
        core.Settings.IsStatusBarEnabled = false;

        core.SetVirtualHostNameToFolderMapping(
            VirtualHost, _paths.WebDir, CoreWebView2HostResourceAccessKind.Allow);

        _bridge = new Bridge(_web, _handlers);
        _bridge.Attach();
        core.NavigationCompleted += (_, _) =>
        {
            _ready = true;
            _bridge.PushState();
        };

        core.Navigate($"https://{VirtualHost}/index.html");
    }

    /// <summary>Bring the window up (used by the tray). Restores if minimized; pushes fresh state.</summary>
    public void ShowWindow()
    {
        Show();
        if (WindowState == FormWindowState.Minimized)
            WindowState = FormWindowState.Normal;
        BringToFront();
        Activate();
        PushState();
    }

    /// <summary>Push current state to the page if it has loaded (called on external status changes).</summary>
    public void PushState()
    {
        if (_ready)
            _bridge?.PushState();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // X hides rather than exits — the guard keeps running in the tray.
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
            return;
        }
        base.OnFormClosing(e);
    }
}
