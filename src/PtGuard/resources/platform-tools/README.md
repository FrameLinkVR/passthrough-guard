# Bundled adb / platform-tools (release/rig step)

pt-guard drives a **bundled** `adb` on a **private server port** so it never clashes with — or
hijacks — the user's own adb (FrameLink streaming, scrcpy, Android Studio may already own the
default `5037`). `AdbLocator` resolves this folder first (`AppPaths.BundledAdb`) and only falls
back to a PATH adb if it is absent.

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
At runtime the app sets `ANDROID_ADB_SERVER_PORT` to a private value and runs `adb start-server`
against it, then `adb kill-server` (private port only) on exit.
