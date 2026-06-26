# FrameLink Passthrough Guard

Stops the Quest 3's **double-tap → passthrough** gesture from yanking you out of VR mid-race.

Wheel and pedal vibration in sim racing constantly triggers the headset's "double-tap the side to
see through" gesture. On a retail Quest you can't turn it off — the toggle auto-reverts. So this
tray app doesn't disable it. It **watches for the accidental tap and instantly reverses it**:
passthrough blips for about a tenth of a second, then snaps you straight back into the game.

- Runs quietly in the **system tray**.
- **Only** reverses the accidental gesture (the double-tap). Opening passthrough on purpose from the
  Quick Actions menu is left alone.
- Works over **USB or Wi-Fi**, and coexists with FrameLink / Virtual Desktop streaming.
- Uses its **own bundled adb on a private port** — it never disturbs your existing adb setup.

## How it works
1. It reads the headset's log over `adb logcat` and matches the one line that means
   "full passthrough just turned on from the double-tap."
2. It fires a reverse broadcast over a kept-warm `adb shell` (~10 ms) that toggles passthrough back
   off.
3. The reverse logs as a different event, so the guard can never trigger itself. Repeat detections
   are debounced (~250 ms).

## Install (end user)
1. Run **`FrameLinkPassthroughGuard-Setup-<version>.exe`** and follow the prompts. It installs the
   app, a private copy of `adb`, and (only if needed) the WebView2 runtime, and can start with
   Windows.
2. Right-click the tray icon → **Open settings**, then follow the on-screen **Connect your headset**
   steps: enable Developer Mode + USB debugging, plug in over USB, tap **Allow** in the headset.
3. Optional: click **Enable Wi-Fi** so the guard keeps working with the cable unplugged.

The tray icon shows status at a glance: green = guarding, amber = paused, red = no headset.
Right-click for Pause/Resume, the bounce set, Reconnect, settings, Start-with-Windows, and Quit.

## Develop / build / test (macOS or Windows)
.NET 8 SDK required. The pure guard logic is unit-tested and runs on any OS:

```sh
dotnet test  tests/PtGuard.Tests/PtGuard.Tests.csproj   # 20 logic tests (parser/filter/debounce/locator)
dotnet build FrameLinkPassthroughGuard.sln              # whole solution, incl. the WinForms app
```

The `net8.0-windows` WinForms project builds on macOS/Linux too (via `EnableWindowsTargeting`), so
you can compile-check the whole app off-rig. You just can't *run* the tray / WebView2 / real adb
there — that's the rig.

Project layout:
```
src/PtGuard.Core/   pure logic (net8.0, no WinForms) — parser, filter, debounce, GuardEngine, AdbLocator
src/PtGuard/        WinForms tray app (net8.0-windows) — adb layer, GuardRunner, tray, WebView2 settings
tests/PtGuard.Tests/  xUnit logic tests (net8.0) — the cross-OS verification gate
installer/          Inno Setup installer (ADR-0005)
```

## BUILD-ON-RIG checklist (release — Windows only)
Everything above compiles and tests on macOS. **These steps only run on the Windows rig** and are
required to produce a shippable installer:

- [ ] **Vendor adb** — drop `adb.exe`, `AdbWinApi.dll`, `AdbWinUsbApi.dll` into
      `src/PtGuard/resources/platform-tools/` (Apache-2.0; see that folder's `README.md`).
- [ ] **Vendor the WebView2 bootstrapper** — drop `MicrosoftEdgeWebview2Setup.exe` into
      `installer/redist/` (see its `README.md`).
- [ ] **(Optional) Vendor fonts** — drop the @fontsource `.woff2` subsets into
      `src/PtGuard/Settings/web/fonts/` (see its `README.md`). Without them the UI uses system fonts.
- [ ] **Publish the single exe**
      `dotnet publish src/PtGuard/PtGuard.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o publish/win-x64`
      → `publish/win-x64/FrameLinkPassthroughGuard.exe` (+ `web/`). Bundles the .NET runtime, so the
      target PC needs no separate .NET install.
- [ ] **Compile the installer** — `ISCC.exe installer/pt-guard.iss` →
      `installer/Output/FrameLinkPassthroughGuard-Setup-<version>.exe`.
- [ ] **(Optional) Authenticode-sign** — configure a `signtool` named tool and uncomment `SignTool=`
      / `SignedUninstaller=yes` in `pt-guard.iss` (ADR-0005 / zi5.4).
- [ ] **Smoke test on the rig** — install → pair a Quest 3 over USB → in a sim, double-tap the
      headset → confirm passthrough snaps back within ~1/10 s → uninstall → confirm clean removal
      (tray gone, HKCU Run value removed; the shared adb server is left running for other tools).

## Why C#/.NET here (not the FrameLink app/ stack)
`pt-guard` is a sanctioned satellite outside `app/`'s TypeScript / Zig / C++ language policy. A
WinForms tray + WebView2 + a self-contained single-file exe is the lowest-friction way to ship an
easy-to-install Windows utility. See `CLAUDE.md`.
