namespace PtGuard.Core.Guard;

using PtGuard.Core.Config;

/// <summary>What a logcat line resolves to once the guard has looked at it.</summary>
public enum GuardDecision
{
    /// <summary>Not a passthrough line, or not a transition we react to (To != 1, or a control
    /// point outside the configured bounce set). Includes the reverse echo's "to value 0" line.</summary>
    Ignore,

    /// <summary>A real bounce candidate that fell inside the debounce window — swallowed.</summary>
    Suppress,

    /// <summary>Fire the reverse broadcast now: accidental passthrough detected.</summary>
    Bounce,
}

/// <summary>
/// The complete, side-effect-free guard decision: logcat line in, <see cref="GuardDecision"/> out.
/// Combines the parser, the control-point filter, and the debounce. The WinForms host feeds it
/// every logcat line plus the current time, and fires the warm-shell reverse broadcast only on
/// <see cref="GuardDecision.Bounce"/>. Keeping this pure is what makes the mechanism unit-testable
/// on any OS.
/// </summary>
public sealed class GuardEngine
{
    private readonly GuardConfig _config;
    private readonly Debounce _debounce;

    public GuardEngine(GuardConfig config)
    {
        _config = config;
        _debounce = new Debounce(config.DebounceMs);
    }

    /// <summary>Count of bounces fired this session (drives the tray "N bounces today" status).</summary>
    public int BounceCount { get; private set; }

    /// <summary>
    /// Evaluate one logcat line at time <paramref name="now"/>.
    /// </summary>
    public GuardDecision Process(string line, DateTimeOffset now)
    {
        if (!LogcatParser.TryParse(line, out var evt))
            return GuardDecision.Ignore;

        // Only ever react to passthrough turning ON. The reverse broadcast logs "to value 0",
        // which lands here as TurnedOn == false → Ignore. That is the entire feedback-loop guard.
        if (!evt.TurnedOn)
            return GuardDecision.Ignore;

        // Control-point filter: bounce only the configured set (default DoubleTap). An intentional
        // QuickActionMenu open, or any unknown point, is left alone.
        if (!_config.BounceSet.Contains(evt.ControlPoint))
            return GuardDecision.Ignore;

        if (!_debounce.Accept(now))
            return GuardDecision.Suppress;

        BounceCount++;
        return GuardDecision.Bounce;
    }

    /// <summary>Clear the debounce window (call on resume so the next tap is taken immediately).</summary>
    public void ResetDebounce() => _debounce.Reset();
}
