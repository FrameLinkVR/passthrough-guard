# Bundled adb / platform-tools (release/rig step)

pt-guard ships a **bundled** `adb` so the tool works even when the user has none installed, and
drives it as a **client of the standard shared adb server** (default `5037`) — the same server
Meta Quest Developer Hub, scrcpy, Android Studio or FrameLink already use. We never run a private
server and never `kill-server`: a USB device can be claimed by only one adb server, so a separate
server would be permanently blind to a Quest another tool is holding. `AdbLocator` resolves this
folder first (`AppPaths.BundledAdb`) and only falls back to a PATH adb if it is absent.

These three files are **not committed** (they are an Apache-2.0 redistributable, not our source).
Drop them here from Google's official Android platform-tools release before building the installer:

| File | Purpose |
|---|---|
| `adb.exe` | the adb client/server we drive |
| `AdbWinApi.dll` | adb USB device enumeration (required next to adb.exe) |
| `AdbWinUsbApi.dll` | WinUSB backend for adb (required next to adb.exe) |

Source: <https://developer.android.com/tools/releases/platform-tools> →
`platform-tools-latest-windows.zip`. License: Apache-2.0 (ship `NOTICE` in the release).

How to fetch (on a networked machine):

```sh
curl -LO https://dl.google.com/android/repository/platform-tools-latest-windows.zip
unzip -j platform-tools-latest-windows.zip \
  platform-tools/adb.exe platform-tools/AdbWinApi.dll platform-tools/AdbWinUsbApi.dll \
  -d src/PtGuard/resources/platform-tools/
```

The installer (`installer/pt-guard.iss`) ships this folder as `platform-tools/` beside the exe.
At runtime the app runs `adb start-server` against the default shared server (a no-op if another
tool already started one) and never `kill-server` on exit, so other adb tools keep working.
