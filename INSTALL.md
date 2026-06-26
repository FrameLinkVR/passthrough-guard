# Installing FrameLink Passthrough Guard

A first-time setup guide. The Guard stops your Quest 3's accidental **double-tap → passthrough**
from yanking you out of VR mid-race — without you having to touch a single setting again once it's
running.

Whole thing takes about 5 minutes, most of it one-time headset setup.

---

## What you need

- A **Windows 10 or 11 PC** — the one you play / stream from.
- A **Meta Quest 3** (or 3S).
- A **USB-C cable** for first-time pairing. (You can go fully wireless afterwards.)
- The **Meta Horizon** app on your phone (the Meta Quest companion app) — for turning on Developer Mode.

Nothing else to download. The installer bundles everything — the .NET runtime *and* its own copy of
`adb`. That bundled `adb` runs on a **private port**, so it never clashes with FrameLink streaming,
scrcpy, or Android Studio.

---

## Step 1 — Run the installer

1. Double-click **`FrameLinkPassthroughGuard-Setup.exe`**.
2. Windows asks for permission (the blue "Do you want to allow…?" prompt) — click **Yes**.
3. On the **Select Additional Tasks** screen, leave **"Start … when I sign in to Windows"** ticked
   (recommended — it keeps the Guard always on). A desktop shortcut is optional.
4. Click **Install**. If your PC doesn't already have the WebView2 runtime, the installer adds it
   automatically — no clicks needed.
5. Leave **"Launch FrameLink Passthrough Guard now"** ticked and click **Finish**.

---

## Step 2 — Find it in the system tray

The Guard runs quietly in the background — it doesn't open a window on its own.

- Look in the **system tray** (bottom-right of the taskbar). Click the **^** to show hidden icons if
  you don't see it. The FrameLink icon is there.
- Right now it's **red** — *"No headset"*. That's expected; it hasn't met your Quest yet.
- **Left-click the icon** to open the Guard window.

---

## Step 3 — Connect your Quest 3 (one-time, over USB)

The window shows a **"Connect your headset"** checklist. Work through it top to bottom:

1. **Enable Developer Mode** — in the **Meta Horizon** app on your phone: select your headset →
   **Developer Mode** → turn it **on**. (One-time. Free Meta account, no coding.)
2. **Turn on USB debugging** — put the headset on: **Settings → System → Developer → USB debugging**.
3. **Plug in over USB** — connect the Quest to this PC with the cable. Put the headset on and tap
   **Allow** on the *"Allow USB debugging?"* prompt. Tick **"Always allow from this computer"** so you
   never see it again.
4. Back on the PC, click **Rescan**.

When it connects, the status dot turns **green** and reads **"Guarding"**. You're protected. The
checklist disappears and your headset appears under **Devices**.

> **If it says "Unauthorized"** — the Allow prompt wasn't accepted yet. Put the headset on, tap
> **Allow** (tick "Always allow from this computer"), then click **Rescan**.

---

## Step 4 — (Optional) Go wireless

Want the Guard to keep working with the cable unplugged?

1. With the headset still plugged in and showing **green**, click **Enable Wi-Fi**.
2. The Guard switches to your headset's Wi-Fi address and remembers it. **Unplug the cable** — you're
   still guarded.

Your PC and Quest must be on the same network. This coexists with FrameLink wireless streaming — they
share the same Wi-Fi link.

---

## Step 5 — Test that it works

Put the headset on while it's guarding, and **double-tap the side of the headset** (the accidental
gesture):

- Passthrough flashes on for about **a tenth of a second**, then snaps you straight back to VR.
- Open the Guard window again — the **bounce counter** ticks up.
- An *intentional* passthrough (the system **Quick Actions** menu) is left completely alone.

That's it. Leave it running and forget about it.

---

## Living with it — the tray menu

**Right-click** the tray icon any time:

| Item | What it does |
|---|---|
| **Pause / Resume** | Temporarily stop guarding — e.g. when you *want* passthrough for a while. |
| **Get me back to VR** | Snap out of passthrough right now, manually. |
| **Bounce on ▸** | Pick which gestures get reversed: **Double-tap** (on by default) and optionally **Action button**. |
| **Reconnect** | Re-pair after the headset sleeps or you swap the cable. |
| **Open settings…** | The full window — status, devices, options. |
| **Start with Windows** | Keep the Guard launching at sign-in. |
| **Quit** | Stop the Guard (also shuts down its private `adb`). |

**Left-click** the icon opens the window directly.

---

## Options (in the window)

- **Bounce on → Accidental double-tap** — the wheel/pedal-vibration trigger. **Keep this on.**
- **Action button** — also reverse passthrough from the headset's action button.
- **Start with Windows** — launch automatically at sign-in.
- **Guard only while streaming** — arm only during a FrameLink / SteamVR session. Off (the default)
  means *always armed*, which is the safe choice.

---

## Troubleshooting

- **Icon stays red / "No headset"** → check the cable, confirm USB debugging is on, click **Rescan**.
  A flaky USB cable is the usual culprit — try a different one or a different port.
- **"Unauthorized"** → put the headset on and tap **Allow** on the USB-debugging prompt (tick
  "Always allow from this computer"), then **Rescan**.
- **Headset slept / cable bumped** → right-click → **Reconnect**.
- **Wireless stopped working** → the Quest's Wi-Fi address can change. Plug the USB cable back in once
  and click **Enable Wi-Fi** again to refresh it.
- **You actually want passthrough for a bit** → right-click → **Pause** (then **Resume** when done).

---

## Uninstall

**Windows Settings → Apps → FrameLink Passthrough Guard → Uninstall.** It stops the tray, removes the
"start with Windows" entry, and shuts down its private `adb`. Clean removal, nothing left behind.
