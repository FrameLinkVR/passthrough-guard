namespace PtGuard.Core.Adb;

/// <summary>
/// Resolves which <c>adb</c> binary to use. Pure logic (filesystem existence is injected) so the
/// "bundled wins over system" rule is unit-tested.
/// </summary>
/// <remarks>
/// We prefer the adb.exe we ship in <c>resources/platform-tools/</c> (so the tool works even when
/// the user has no adb installed), and drive it as a CLIENT of the standard shared adb server
/// (default port 5037). adb is designed for one server with many clients, so this lets us coexist
/// with whatever already owns the USB device (Meta Quest Developer Hub, scrcpy, Android Studio,
/// FrameLink) instead of running a competing private server that a USB-held Quest is invisible to.
/// We never <c>kill-server</c>.
/// </remarks>
public static class AdbLocator
{
    /// <summary>
    /// Pick the adb path: the bundled binary if it exists, else a system fallback if that exists,
    /// else null (caller surfaces "adb missing"). The bundled path is always preferred even when a
    /// system adb is present.
    /// </summary>
    /// <param name="bundledPath">Absolute path to the shipped adb(.exe).</param>
    /// <param name="systemPath">Optional system/PATH adb to fall back to.</param>
    /// <param name="exists">Filesystem existence probe (inject <c>File.Exists</c> in production).</param>
    public static string? Resolve(string bundledPath, string? systemPath, Func<string, bool> exists)
    {
        if (!string.IsNullOrEmpty(bundledPath) && exists(bundledPath))
            return bundledPath;

        if (!string.IsNullOrEmpty(systemPath) && exists(systemPath!))
            return systemPath;

        return null;
    }
}
