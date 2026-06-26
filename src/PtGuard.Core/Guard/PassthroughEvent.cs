namespace PtGuard.Core.Guard;

/// <summary>
/// A parsed FullPassthroughController logcat line: the passthrough state transition and the
/// control point that caused it.
/// </summary>
/// <param name="From">Previous isInFullPassthrough value (the "from N" field).</param>
/// <param name="To">New isInFullPassthrough value (the "to value N" field). 1 = passthrough turned ON.</param>
/// <param name="ControlPoint">The control point that drove the transition.</param>
/// <param name="RawControlPoint">The exact token as seen in the log (for diagnostics / unknowns).</param>
public readonly record struct PassthroughEvent(
    int From,
    int To,
    ControlPoint ControlPoint,
    string RawControlPoint)
{
    /// <summary>True when passthrough turned ON (the only transition the guard ever reacts to).</summary>
    public bool TurnedOn => To == 1;
}
