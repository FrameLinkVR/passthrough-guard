# Installer redist (release/rig step)

Drop the **WebView2 Evergreen bootstrapper** here before compiling the installer:

- `MicrosoftEdgeWebview2Setup.exe`

Source: <https://developer.microsoft.com/microsoft-edge/webview2/> → "Evergreen Bootstrapper".
It is ~2 MB and is redistributable. The installer stages it to `{tmp}` and runs it
`/silent /install` **only if** the WebView2 runtime is not already present (`NeedsWebView2` in the
`[Code]` section), then deletes it. Most Windows 10/11 machines already have the runtime, so this
is usually a no-op.

This file is **not committed** (it is a Microsoft redistributable, not our source).
