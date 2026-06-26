# FrameLink Passthrough Guard - release build harness (run on the Windows rig).
#
# Every run BUMPS the patch version (single source of truth: the repo-root VERSION file), then:
#   1. publishes the self-contained single-file exe at that version,
#   2. compiles the Inno Setup installer at that version,
#   3. copies the versioned installer onto the Desktop (and clears the old unversioned one).
#
# So no two builds ever share a version, and the freshly built installer is always on the Desktop.
# ASCII only (scp/SSH mangles non-ASCII). Usage:  powershell -ExecutionPolicy Bypass -File scripts\build.ps1
[CmdletBinding()]
param(
  [ValidateSet('patch', 'minor', 'major')] [string] $Bump = 'patch',
  [switch] $NoDesktopCopy
)
$ErrorActionPreference = 'Stop'

$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$versionFile = Join-Path $root 'VERSION'
$iss = Join-Path $root 'installer\pt-guard.iss'
$iscc = 'C:\Program Files (x86)\Inno Setup 6\ISCC.exe'
$ptDir = Join-Path $root 'src\PtGuard\resources\platform-tools'
$redist = Join-Path $root 'installer\redist\MicrosoftEdgeWebview2Setup.exe'

# --- bump the version -------------------------------------------------------------------------
if (-not (Test-Path $versionFile)) { throw "VERSION file missing at $versionFile" }
$cur = (Get-Content $versionFile -Raw).Trim()
$m = [regex]::Match($cur, '^(\d+)\.(\d+)\.(\d+)$')
if (-not $m.Success) { throw "VERSION is not MAJOR.MINOR.PATCH: '$cur'" }
$maj = [int] $m.Groups[1].Value
$min = [int] $m.Groups[2].Value
$pat = [int] $m.Groups[3].Value
switch ($Bump) {
  'major' { $maj++; $min = 0; $pat = 0 }
  'minor' { $min++; $pat = 0 }
  default { $pat++ }   # patch
}
$ver = "$maj.$min.$pat"
Set-Content -Path $versionFile -Value $ver -NoNewline
Write-Host "== version: $cur -> $ver ($Bump) =="

# --- pre-flight: vendored release inputs must be present --------------------------------------
foreach ($f in 'adb.exe', 'AdbWinApi.dll', 'AdbWinUsbApi.dll') {
  if (-not (Test-Path (Join-Path $ptDir $f))) {
    throw "Missing bundled $f in $ptDir - vendor platform-tools first (see that folder's README)."
  }
}
if (-not (Test-Path $redist)) { throw "Missing WebView2 bootstrapper at $redist (see installer\redist\README.md)." }
if (-not (Test-Path $iscc)) { throw "ISCC not found at $iscc (install Inno Setup 6)." }

# --- publish ----------------------------------------------------------------------------------
Write-Host "== dotnet publish (self-contained single-file win-x64) =="
$pub = Join-Path $root 'publish\win-x64'
dotnet publish (Join-Path $root 'src\PtGuard\PtGuard.csproj') -c Release -r win-x64 --self-contained `
  -p:PublishSingleFile=true "-p:Version=$ver" -o $pub --nologo 2>&1 |
  Where-Object { $_ -match 'error|Build succeeded|FrameLinkPassthroughGuard.exe' } | ForEach-Object { "  $_" }
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed ($LASTEXITCODE)" }
$exe = Join-Path $pub 'FrameLinkPassthroughGuard.exe'
if (-not (Test-Path $exe)) { throw "expected exe not produced: $exe" }

# --- compile installer ------------------------------------------------------------------------
Write-Host "== ISCC (installer at $ver) =="
& $iscc "/DAppVersion=$ver" $iss | Where-Object { $_ -match 'error|warning|Successful' } | ForEach-Object { "  $_" }
if ($LASTEXITCODE -ne 0) { throw "ISCC failed ($LASTEXITCODE)" }
$setup = Join-Path $root ("installer\Output\FrameLinkPassthroughGuard-Setup-$ver.exe")
if (-not (Test-Path $setup)) { throw "expected installer not produced: $setup" }

# --- drop on the Desktop ----------------------------------------------------------------------
if (-not $NoDesktopCopy) {
  $desktop = [Environment]::GetFolderPath('Desktop')
  $dst = Join-Path $desktop "FrameLinkPassthroughGuard-Setup-$ver.exe"
  Copy-Item $setup $dst -Force
  Write-Host ("  copied to Desktop: {0}" -f $dst)
  # Best-effort: clear the old UNVERSIONED installer so the user can't run a stale one. If it is
  # locked (open in Explorer / mid-download), just warn - the versioned copy above is what matters.
  $old = Join-Path $desktop 'FrameLinkPassthroughGuard-Setup.exe'
  if (Test-Path $old) {
    try { Remove-Item $old -Force -ErrorAction Stop; Write-Host "  removed stale unversioned installer" }
    catch { Write-Host "  (note) could not remove stale unversioned installer: $($_.Exception.Message)" }
  }
}

Write-Host ""
Write-Host "=================================================="
Write-Host ("BUILD READY  v$ver")
Write-Host ("  installer: {0}" -f $setup)
Write-Host ("  size:      {0:N1} MB" -f ((Get-Item $setup).Length / 1MB))
Write-Host "=================================================="
