namespace PtGuard.Core.Adb;

/// <summary>
/// Resolves which <c>adb</c> binary to use, and on which server port. Pure logic (filesystem
/// existence is injected) so the "bundled wins over system" rule is unit-tested.
/// </summary>
/// <remarks>
/// We always prefer the adb.exe we ship in <c>resources/platform-tools/</c> and run it on a
/// private server port, so pt-guard never clashes with — or hijacks — the user's own adb server
/// (which FrameLink streaming, scrcpy, Android Studio, etc. may already be driving on the default
/// 5037).
/// </remarks>
public static class AdbLocator
{
    /// <summary>Private adb server port. Deliberately not the default 5037 so we own our own
    /// server and never disturb the user's. Exported via <c>ANDROID_ADB_SERVER_PORT</c>.</summary>
    public const int PrivateServerPort = 5677;

    public const string ServerPortEnvVar = "ANDROID_ADB_SERVER_PORT";

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
