using PtGuard.Core.Config;
using PtGuard.Core.Guard;
using Xunit;

namespace PtGuard.Tests;

public class GuardEngineTests
{
    private static string Detect(string controlPoint) =>
        $"I FullPassthroughController: isInFullPassthrough is updated from 0 to value 1 from control point {controlPoint}";

    private const string ReverseEcho =
        "I FullPassthroughController: isInFullPassthrough is updated from 1 to value 0 from QuickActionMenu";

    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void DoubleTap_to_value_1_bounces()
    {
        var engine = new GuardEngine(new GuardConfig());
        Assert.Equal(GuardDecision.Bounce, engine.Process(Detect("DoubleTap"), T0));
        Assert.Equal(1, engine.BounceCount);
    }

    [Fact]
    public void Reverse_echo_to_value_0_is_ignored_no_feedback_loop()
    {
        var engine = new GuardEngine(new GuardConfig());
        Assert.Equal(GuardDecision.Ignore, engine.Process(ReverseEcho, T0));
        Assert.Equal(0, engine.BounceCount);
    }

    [Fact]
    public void Control_point_outside_the_bounce_set_is_ignored()
    {
        // QuickActionMenu = intentional menu open. Not in the default set → never bounced.
        var engine = new GuardEngine(new GuardConfig());
        Assert.Equal(GuardDecision.Ignore, engine.Process(Detect("QuickActionMenu"), T0));
    }

    [Fact]
    public void ActionButton_bounces_only_when_added_to_the_set()
    {
        var off = new GuardEngine(new GuardConfig());
        Assert.Equal(GuardDecision.Ignore, off.Process(Detect("ActionButton"), T0));

        var cfg = new GuardConfig { BounceSet = { ControlPoint.ActionButton } };
        var on = new GuardEngine(cfg);
        Assert.Equal(GuardDecision.Bounce, on.Process(Detect("ActionButton"), T0));
    }

    [Fact]
    public void Repeat_within_debounce_window_is_suppressed_then_taken_after_it()
    {
        var engine = new GuardEngine(new GuardConfig { DebounceMs = 250 });
        Assert.Equal(GuardDecision.Bounce, engine.Process(Detect("DoubleTap"), T0));
        Assert.Equal(GuardDecision.Suppress, engine.Process(Detect("DoubleTap"), T0.AddMilliseconds(100)));
        Assert.Equal(GuardDecision.Suppress, engine.Process(Detect("DoubleTap"), T0.AddMilliseconds(249)));
        Assert.Equal(GuardDecision.Bounce, engine.Process(Detect("DoubleTap"), T0.AddMilliseconds(300)));
        Assert.Equal(2, engine.BounceCount);
    }

    [Fact]
    public void Unrelated_lines_are_ignored()
    {
        var engine = new GuardEngine(new GuardConfig());
        Assert.Equal(GuardDecision.Ignore, engine.Process("just some logcat chatter", T0));
    }
}
