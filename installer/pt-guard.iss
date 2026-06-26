; FrameLink Passthrough Guard — Inno Setup installer (per ADR-0005: Inno Setup).
; BUILD-ON-RIG: ISCC.exe runs on Windows only. Before compiling, stage the inputs:
;   1. dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true \
;        -o ..\publish\win-x64        (produces FrameLinkPassthroughGuard.exe + web\)
;   2. drop adb.exe + AdbWinApi.dll + AdbWinUsbApi.dll into
;        ..\src\PtGuard\resources\platform-tools\   (see that folder's README)
;   3. drop MicrosoftEdgeWebview2Setup.exe into .\redist\   (see redist\README.md)
;   4. (optional) vendor the @fontsource .woff2 into ..\src\PtGuard\Settings\web\fonts\
;        and re-publish so they land in publish\win-x64\web\fonts\
; Then: ISCC.exe pt-guard.iss   →   Output\FrameLinkPassthroughGuard-Setup-<ver>.exe

#define AppName "FrameLink Passthrough Guard"
#define AppVersion "0.1.0"
#define AppPublisher "FrameLink"
#define ExeName "FrameLinkPassthroughGuard.exe"

[Setup]
AppId={{8B6F1C2A-9D4E-4A7B-9E2F-2C1A7F3D5B10}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={autopf}\FrameLink\Passthrough Guard
DefaultGroupName=FrameLink
DisableProgramGroupPage=yes
UninstallDisplayIcon={app}\resources\framelink.ico
UninstallDisplayName={#AppName}
OutputDir=Output
OutputBaseFilename=FrameLinkPassthroughGuard-Setup-{#AppVersion}
Compression=lzma2
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
WizardStyle=modern
PrivilegesRequired=admin
SetupIconFile=..\src\PtGuard\resources\framelink.ico
; Authenticode (ADR-0005 / zi5.4): sign on the rig. Configure a "SignTool" named tool in the
; ISCC environment, then uncomment to sign both setup + uninstaller:
; SignTool=signtool
; SignedUninstaller=yes

[Tasks]
Name: "autostart"; Description: "Start {#AppName} when I sign in to Windows"; GroupDescription: "Startup:"
Name: "desktopicon"; Description: "Create a desktop shortcut"; Flags: unchecked

[Files]
Source: "..\publish\win-x64\{#ExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\publish\win-x64\web\*"; DestDir: "{app}\web"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\src\PtGuard\resources\framelink.ico"; DestDir: "{app}\resources"; Flags: ignoreversion
; Bundled adb on a private server port (never clashes with the user's adb). Apache-2.0.
Source: "..\src\PtGuard\resources\platform-tools\adb.exe"; DestDir: "{app}\platform-tools"; Flags: ignoreversion
Source: "..\src\PtGuard\resources\platform-tools\AdbWinApi.dll"; DestDir: "{app}\platform-tools"; Flags: ignoreversion
Source: "..\src\PtGuard\resources\platform-tools\AdbWinUsbApi.dll"; DestDir: "{app}\platform-tools"; Flags: ignoreversion
; WebView2 evergreen bootstrapper — staged to temp, run silently only if the runtime is missing.
Source: "redist\MicrosoftEdgeWebview2Setup.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall; Check: NeedsWebView2

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#ExeName}"; IconFilename: "{app}\resources\framelink.ico"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#ExeName}"; IconFilename: "{app}\resources\framelink.ico"; Tasks: desktopicon

[Registry]
; Auto-start via the SAME HKCU Run value the app's "Start with Windows" toggle manages, so the
; in-app checkbox and the installer stay consistent. Removed on uninstall.
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; \
  ValueName: "FrameLinkPassthroughGuard"; ValueData: """{app}\{#ExeName}"""; \
  Tasks: autostart; Flags: uninsdeletevalue

[Run]
Filename: "{tmp}\MicrosoftEdgeWebview2Setup.exe"; Parameters: "/silent /install"; \
  StatusMsg: "Installing the WebView2 runtime…"; Check: NeedsWebView2
Filename: "{app}\{#ExeName}"; Description: "Launch {#AppName} now"; \
  Flags: nowait postinstall skipifsilent

[UninstallRun]
; Stop the tray so its files unlock and our private adb server is torn down before removal.
Filename: "{cmd}"; Parameters: "/c taskkill /im {#ExeName} /f"; Flags: runhidden; RunOnceId: "StopGuard"

[Code]
// WebView2 evergreen runtime registers its version ("pv") under this client GUID, per-machine
// (incl. WOW6432Node) or per-user. If none is present we run the bootstrapper.
function NeedsWebView2(): Boolean;
const
  Client = '{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}';
var
  v: String;
begin
  Result := not (
    RegQueryStringValue(HKLM, 'SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\' + Client, 'pv', v) or
    RegQueryStringValue(HKLM, 'SOFTWARE\Microsoft\EdgeUpdate\Clients\' + Client, 'pv', v) or
    RegQueryStringValue(HKCU, 'SOFTWARE\Microsoft\EdgeUpdate\Clients\' + Client, 'pv', v));
end;
