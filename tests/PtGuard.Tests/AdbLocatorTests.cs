using PtGuard.Core.Adb;
using Xunit;

namespace PtGuard.Tests;

public class AdbLocatorTests
{
    private const string Bundled = @"C:\Program Files\FrameLink\pt-guard\platform-tools\adb.exe";
    private const string System = @"C:\platform-tools\adb.exe";

    [Fact]
    public void Prefers_the_bundled_adb_even_when_a_system_adb_exists()
    {
        var path = AdbLocator.Resolve(Bundled, System, p => p == Bundled || p == System);
        Assert.Equal(Bundled, path);
    }

    [Fact]
    public void Falls_back_to_system_adb_when_bundled_is_missing()
    {
        var path = AdbLocator.Resolve(Bundled, System, p => p == System);
        Assert.Equal(System, path);
    }

    [Fact]
    public void Returns_null_when_neither_exists()
    {
        var path = AdbLocator.Resolve(Bundled, System, _ => false);
        Assert.Null(path);
    }
}
