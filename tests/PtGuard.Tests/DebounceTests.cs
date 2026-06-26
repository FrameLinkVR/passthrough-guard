using PtGuard.Core.Guard;
using Xunit;

namespace PtGuard.Tests;

public class DebounceTests
{
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void First_event_is_always_accepted()
    {
        var d = new Debounce(250);
        Assert.True(d.Accept(T0));
    }

    [Fact]
    public void Events_inside_the_window_are_suppressed()
    {
        var d = new Debounce(250);
        Assert.True(d.Accept(T0));
        Assert.False(d.Accept(T0.AddMilliseconds(1)));
        Assert.False(d.Accept(T0.AddMilliseconds(249)));
    }

    [Fact]
    public void Event_at_or_past_the_window_is_accepted_and_rearms()
    {
        var d = new Debounce(250);
        Assert.True(d.Accept(T0));
        Assert.True(d.Accept(T0.AddMilliseconds(250)));
        // Window now anchored to 250ms; 300ms is only 50ms later → suppressed.
        Assert.False(d.Accept(T0.AddMilliseconds(300)));
    }

    [Fact]
    public void Reset_forces_the_next_event_to_be_taken()
    {
        var d = new Debounce(250);
        Assert.True(d.Accept(T0));
        d.Reset();
        Assert.True(d.Accept(T0.AddMilliseconds(10)));
    }
}
