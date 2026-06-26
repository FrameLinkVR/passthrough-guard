namespace PtGuard;

internal static class Program
{
    // Per-user single-instance gate: two guards racing on one headset would fight over adb and
    // double-fire the reverse broadcast. The Local\ prefix scopes it to the logon session.
    private const string MutexName = "Local\\FrameLinkPassthroughGuard";

    [STAThread]
    private static void Main()
    {
        using var mutex = new Mutex(initiallyOwned: true, MutexName, out var isNew);
        if (!isNew)
            return; // already running — the existing tray owns the headset

        ApplicationConfiguration.Initialize();
        Application.Run(new TrayAppContext());

        GC.KeepAlive(mutex);
    }
}
