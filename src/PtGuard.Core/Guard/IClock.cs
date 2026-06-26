namespace PtGuard.Core.Guard;

/// <summary>
/// Time source. Injected so the debounce / engine never read wall-clock <c>DateTimeOffset.Now</c>
/// directly — tests drive a deterministic fake clock instead.
/// </summary>
public interface IClock
{
    DateTimeOffset Now { get; }
}

/// <summary>Real wall-clock implementation used by the running app.</summary>
public sealed class SystemClock : IClock
{
    public static readonly SystemClock Instance = new();
    public DateTimeOffset Now => DateTimeOffset.UtcNow;
}
