namespace PtGuard.Core.Guard;

/// <summary>
/// Repeat suppressor: after one accepted event, swallow further events that arrive within the
/// window. The Quest emits the passthrough transition more than once per physical double-tap; a
/// ~250 ms window collapses that burst into a single reverse broadcast.
/// </summary>
/// <remarks>
/// No wall-clock inside the unit — callers pass the current time (from an <see cref="IClock"/>),
/// so the window is exercised deterministically in tests.
/// </remarks>
public sealed class Debounce(int windowMs)
{
    private readonly int _windowMs = windowMs;
    private DateTimeOffset? _lastAccepted;

    /// <summary>
    /// Decide whether an event at <paramref name="now"/> should be accepted. Returns true and arms
    /// the window on accept; returns false (suppresses) while inside the window of the last accept.
    /// </summary>
    public bool Accept(DateTimeOffset now)
    {
        if (_lastAccepted is { } last && (now - last).TotalMilliseconds < _windowMs)
            return false;

        _lastAccepted = now;
        return true;
    }

    /// <summary>Forget the last accept so the next event is always taken (e.g. after resume).</summary>
    public void Reset() => _lastAccepted = null;
}
