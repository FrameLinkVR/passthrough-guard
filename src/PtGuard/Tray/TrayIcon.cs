using PtGuard.Core.Guard;
using PtGuard.Guard;

namespace PtGuard.Tray;

/// <summary>Callbacks the tray invokes; wired to the runner/config by the app context.</summary>
public sealed record TrayCallbacks(
    Action TogglePause,
    Action<ControlPoint> ToggleBounce,
    Action Reconnect,
    Action OpenSettings,
    Action ToggleAutoStart,
    Action GetMeBackToVr,
    Action Quit);

/// <summary>
/// The system-tray presence: a state-tinted brand icon, a status tooltip, and the right-click menu
/// (Pause/Resume · Bounce set · Reconnect · Settings · Start with Windows · Quit). Pure view — it
/// holds no guard logic; it renders status and forwards clicks through <see cref="TrayCallbacks"/>.
/// </summary>
public sealed class TrayIcon : IDisposable
{
    private readonly NotifyIcon _notify;
    private readonly IconStates _icons;
    private readonly TrayCallbacks _cb;

    private readonly ToolStripMenuItem _pauseItem;
    private readonly ToolStripMenuItem _doubleTapItem;
    private readonly ToolStripMenuItem _actionButtonItem;
    private readonly ToolStripMenuItem _autoStartItem;

    public TrayIcon(IconStates icons, TrayCallbacks callbacks)
    {
        _icons = icons;
        _cb = callbacks;

        _pauseItem = new ToolStripMenuItem("Pause", null, (_, _) => _cb.TogglePause());
        _doubleTapItem = new ToolStripMenuItem("Double-tap", null, (_, _) => _cb.ToggleBounce(ControlPoint.DoubleTap)) { CheckOnClick = false };
        _actionButtonItem = new ToolStripMenuItem("Action button", null, (_, _) => _cb.ToggleBounce(ControlPoint.ActionButton)) { CheckOnClick = false };
        _autoStartItem = new ToolStripMenuItem("Start with Windows", null, (_, _) => _cb.ToggleAutoStart()) { CheckOnClick = false };

        var bounceMenu = new ToolStripMenuItem("Bounce on");
        bounceMenu.DropDownItems.AddRange(new ToolStripItem[] { _doubleTapItem, _actionButtonItem });

        var menu = new ContextMenuStrip();
        menu.Items.AddRange(new ToolStripItem[]
        {
            _pauseItem,
            new ToolStripMenuItem("Get me back to VR", null, (_, _) => _cb.GetMeBackToVr()),
            new ToolStripSeparator(),
            bounceMenu,
            new ToolStripMenuItem("Reconnect", null, (_, _) => _cb.Reconnect()),
            new ToolStripSeparator(),
            new ToolStripMenuItem("Open settings…", null, (_, _) => _cb.OpenSettings()),
            _autoStartItem,
            new ToolStripSeparator(),
            new ToolStripMenuItem("Quit", null, (_, _) => _cb.Quit()),
        });

        _notify = new NotifyIcon
        {
            Text = "FrameLink Passthrough Guard",
            Visible = false,
            ContextMenuStrip = menu,
            Icon = _icons.ForState(GuardState.Disconnected),
        };
        // Left-click opens settings (the obvious "click to connect" affordance).
        _notify.MouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
                _cb.OpenSettings();
        };
    }

    public void Show() => _notify.Visible = true;

    public void UpdateStatus(GuardStatus status)
    {
        _notify.Icon = _icons.ForState(status.State);
        _notify.Text = Truncate($"FrameLink Guard — {status.Detail}");
        _pauseItem.Text = status.State == GuardState.Paused ? "Resume" : "Pause";
    }

    public void SetBounceChecks(IReadOnlySet<ControlPoint> set)
    {
        _doubleTapItem.Checked = set.Contains(ControlPoint.DoubleTap);
        _actionButtonItem.Checked = set.Contains(ControlPoint.ActionButton);
    }

    public void SetAutoStartChecked(bool on) => _autoStartItem.Checked = on;

    /// <summary>Transient balloon (e.g. "Bounced — back in VR"). Best-effort; ignored if hidden.</summary>
    public void Notify(string title, string text) =>
        _notify.ShowBalloonTip(2000, title, text, ToolTipIcon.None);

    // NotifyIcon.Text is capped at 63 chars by the shell.
    private static string Truncate(string s) => s.Length <= 63 ? s : s[..62] + "…";

    public void Dispose()
    {
        _notify.Visible = false;
        _notify.Dispose();
    }
}
