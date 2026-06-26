using PtGuard.Core.Guard;
using Xunit;

namespace PtGuard.Tests;

public class LogcatParserTests
{
    private const string DetectLine =
        "01-01 00:00:00.000  1234  1234 I FullPassthroughController: isInFullPassthrough is updated from 0 to value 1 from control point DoubleTap";

    private const string ReverseEchoLine =
        "01-01 00:00:00.100  1234  1234 I FullPassthroughController: isInFullPassthrough is updated from 1 to value 0 from QuickActionMenu";

    [Fact]
    public void Parses_the_detect_line()
    {
        Assert.True(LogcatParser.TryParse(DetectLine, out var evt));
        Assert.Equal(0, evt.From);
        Assert.Equal(1, evt.To);
        Assert.True(evt.TurnedOn);
        Assert.Equal(ControlPoint.DoubleTap, evt.ControlPoint);
    }

    [Fact]
    public void Parses_the_reverse_echo_without_control_point_prefix()
    {
        // The reverse broadcast logs "from QuickActionMenu" (no "control point" words). It must
        // still parse so the To==0 filter can ignore it — robustness against the format quirk.
        Assert.True(LogcatParser.TryParse(ReverseEchoLine, out var evt));
        Assert.Equal(0, evt.To);
        Assert.False(evt.TurnedOn);
        Assert.Equal(ControlPoint.QuickActionMenu, evt.ControlPoint);
    }

    [Theory]
    [InlineData("")]
    [InlineData("some unrelated logcat noise")]
    [InlineData("I SomethingElse: isInFullPassthrough mentioned but not the update line")]
    public void Returns_false_for_non_matching_lines(string line)
    {
        Assert.False(LogcatParser.TryParse(line, out _));
    }

    [Fact]
    public void Unknown_control_point_maps_to_Unknown_not_a_guess()
    {
        const string line =
            "I FullPassthroughController: isInFullPassthrough is updated from 0 to value 1 from control point SomethingNew";
        Assert.True(LogcatParser.TryParse(line, out var evt));
        Assert.Equal(ControlPoint.Unknown, evt.ControlPoint);
        Assert.Equal("SomethingNew", evt.RawControlPoint);
    }
}
