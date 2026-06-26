// Settings UI controller. Pure view: posts intent messages to the C# bridge and renders the
// SettingsSnapshot the bridge pushes back as { type: "state", payload }.
"use strict";

const wv = window.chrome && window.chrome.webview;
const post = (msg) => wv && wv.postMessage(msg);
const $ = (id) => document.getElementById(id);

const DOT_BY_STATE = {
  Guarding: "good",
  Paused: "warn",
  Connecting: "accent",
  Disconnected: "err",
  Unauthorized: "err",
  NoAdb: "err",
};

function render(s) {
  // status line
  $("sdot").className = "sdot " + (DOT_BY_STATE[s.guardState] || "idle");
  $("detail").textContent = s.detail || s.guardState;
  $("counter").textContent = s.bounceCount > 0
    ? s.bounceCount + (s.bounceCount === 1 ? " bounce" : " bounces")
    : "";

  $("pauseBtn").textContent = s.paused ? "Resume" : "Pause";

  // connection wizard vs devices
  const connected = s.guardState === "Guarding" || s.guardState === "Paused";
  $("wizard").classList.toggle("hidden", connected);
  $("devicesPanel").classList.toggle("hidden", !s.devices || s.devices.length === 0);
  renderDevices(s.devices || []);

  // toggles (reflect, don't fight the user)
  $("bounceDoubleTap").checked = s.bounceDoubleTap;
  $("bounceActionButton").checked = s.bounceActionButton;
  $("autoStart").checked = s.autoStart;
  $("guardOnlyWhileStreaming").checked = s.guardOnlyWhileStreaming;
}

function renderDevices(devices) {
  const list = $("deviceList");
  list.replaceChildren();
  for (const d of devices) {
    const row = document.createElement("div");
    row.className = "pickrow";
    const ready = d.state === "Device";
    row.innerHTML =
      '<span class="sdot ' + (ready ? "good" : "warn") + '"></span>' +
      '<span class="grow mono" style="font-size:12px">' + escapeHtml(d.serial) + "</span>" +
      '<span class="chip-mini">' + escapeHtml(d.kind) + "</span>" +
      '<span class="chip-mini">' + escapeHtml(d.state) + "</span>";
    list.appendChild(row);
  }
}

function escapeHtml(v) {
  return String(v).replace(/[&<>"]/g, (c) =>
    ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;" }[c]));
}

// ---- intents ----
$("pauseBtn").onclick = () => post({ type: "pause", value: $("pauseBtn").textContent === "Pause" });
$("backBtn").onclick = () => post({ type: "manualBounce" });
$("refreshBtn").onclick = () => post({ type: "refresh" });
$("refreshBtn2").onclick = () => post({ type: "refresh" });
$("wifiBtn").onclick = () => post({ type: "enableWifi" });
$("bounceDoubleTap").onchange = (e) => post({ type: "setBounce", point: "DoubleTap", value: e.target.checked });
$("bounceActionButton").onchange = (e) => post({ type: "setBounce", point: "ActionButton", value: e.target.checked });
$("autoStart").onchange = (e) => post({ type: "setAutoStart", value: e.target.checked });
$("guardOnlyWhileStreaming").onchange = (e) => post({ type: "setGuardOnlyWhileStreaming", value: e.target.checked });

// ---- bridge wiring ----
if (wv) {
  wv.addEventListener("message", (e) => {
    const m = e.data;
    if (m && m.type === "state") render(m.payload);
  });
  post({ type: "ready" });
  post({ type: "refresh" });
}
