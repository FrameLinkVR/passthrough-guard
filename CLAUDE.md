# FrameLink Passthrough Guard (`pt-guard`)

A standalone Windows system-tray app that stops the Quest 3's accidental
**double-tap → passthrough** gesture from yanking a sim racer out of VR. It does **not** disable
passthrough (that toggle is server-locked on retail headsets). It **detects** the accidental tap in
`adb logcat` and instantly fires a **reverse broadcast**, so passthrough blips for ~1/10 s then
snaps back.

## Language policy — sanctioned carve-out
This is **C# / .NET 8**, deliberately. The sibling `app/`'s strict **TypeScript / Zig / C++** language
split (see `app/CLAUDE.md`) **does NOT apply here.** `pt-guard` is a sanctioned satellite: a Windows
tray + WebView2 desktop utility where WinForms + a self-contained .NET single-file exe is the
lowest-friction path to "make installing easy." Do not port this to the app/ stack.

## The mechanism (verified live on a Quest 3 — do NOT re-derive)
- **Detect** (one logcat line): `FullPassthroughController: isInFullPassthrough is updated from 0 to
  value 1 from control point <CP>`. Act only when passthrough turns **on** (`to value 1`) **and**
  `<CP>` is in the configured bounce set (default `DoubleTap`).
- **Reverse** (warm `adb shell`):
  `am broadcast -a com.oculus.vrshell.intent.action.UPDATE_FULL_PASSTHROUGH -n com.oculus.vrshell/.ShellControlBroadcastReceiver --es toggle_point QuickActionMenu`
- **No feedback loop:** the reverse logs `… to value 0 from QuickActionMenu`. The filter only matches
  `to value 1`, so it self-ignores. Repeat detect lines are debounced ~250 ms.
- **Latency:** broadcast round-trip is 35-44 ms cold / ~10 ms warm → keep **one long-lived `adb shell`**
  open for the reverse path.

## Architecture (where the logic lives)
| Layer | Project | Notes |
|---|---|---|
| Pure guard logic | `src/PtGuard.Core` (**net8.0, no WinForms**) | parser, control-point filter, debounce, `GuardEngine`, `AdbLocator`, `GuardConfig`. Unit-tested. |
| Tray app | `src/PtGuard` (**net8.0-windows**, WinForms + WebView2) | adb process layer, `GuardRunner` supervisor, tray, settings window, config IO, autostart. |
| Tests | `tests/PtGuard.Tests` (**net8.0**, references Core only) | runs on macOS/Linux/CI. |

The `Core` vs `windows` split is load-bearing: it keeps the testable mechanism off the WinForms
runtime so `dotnet test` is the cross-OS verification gate. **Keep parsing/filter/debounce logic in
`Core`.** The WinForms layer is the IO/lifecycle shell around it.

### Key invariants
- **Bundled adb on a PRIVATE server port.** `AdbLocator` resolves the shipped
  `resources/platform-tools/adb.exe` first; every adb process inherits `ANDROID_ADB_SERVER_PORT`
  (= `AdbLocator.PrivateServerPort`, not 5037) so we never clash with or hijack the user's adb
  (FrameLink streaming / Android Studio). We `kill-server` only our private port on exit.
- **The filter is the safety.** Only `to value 1` AND a configured control point bounces. The default
  set is `DoubleTap` only — an intentional `QuickActionMenu` open is never fought, and our own reverse
  (which toggles via `QuickActionMenu`) can never re-trigger the guard.
- **Warm shell for the reverse path.** Latency-critical; `WarmShell` keeps one `adb shell` attached
  and reopens it if it dies. `GuardRunner` owns reconnect/backoff (headset sleep, cable pull).
- **No wall-clock in the unit.** Debounce/engine take an injected `IClock`; tests drive a fake clock.

## BUILD-ON-RIG (what only runs on Windows)
This dev box is **macOS**. These compile/test here; the rest is the user's Windows rig (Fractal):
- **Builds here:** `dotnet build` (incl. the `net8.0-windows` WinForms project, via
  `EnableWindowsTargeting`), `dotnet publish -r win-x64 --self-contained -p:PublishSingleFile=true`.
- **Tests here:** `dotnet test` (Core logic — the verification gate).
- **Rig only:** running the tray, WebView2 UI, real `adb`, the live passthrough bounce, vendoring
  `platform-tools` + fonts + the WebView2 bootstrapper, and compiling the Inno Setup installer
  (`ISCC.exe`). See `README.md`'s BUILD-ON-RIG checklist.

## Verification
```sh
dotnet test tests/PtGuard.Tests/PtGuard.Tests.csproj   # 20 logic tests — runs on macOS
dotnet build FrameLinkPassthroughGuard.sln             # whole solution compiles cross-OS
```

## Discipline
Commit before + after each discrete change (`verb: desc`, atomic, no batching). Never push unless
asked. KISS / YAGNI / SRP; explicit > clever; surface unexpected errors, handle expected ones at the
boundary (e.g. a torn-down warm shell is expected — reconnect, don't crash).
