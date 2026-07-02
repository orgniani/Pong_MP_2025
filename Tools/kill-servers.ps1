[CmdletBinding(SupportsShouldProcess)]
param(
    [switch]$All,
    [switch]$DryRun
)

$PidFile = Join-Path (Join-Path (Join-Path $PSScriptRoot "..") "Logs") "server.pid"
$ExeName = "Pong_MP_2025"

function Stop-Target {
    param([System.Diagnostics.Process]$proc)
    if ($DryRun) {
        Write-Host "[DryRun] Would stop PID $($proc.Id) ($($proc.ProcessName))"
    } else {
        Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
        Write-Host "Stopped PID $($proc.Id)"
    }
}

if ($All) {
    $procs = Get-Process -Name $ExeName -ErrorAction SilentlyContinue
    if (-not $procs) {
        Write-Host "No running instances of $ExeName found."
        exit 0
    }
    foreach ($p in $procs) { Stop-Target $p }
    if (-not $DryRun -and (Test-Path $PidFile)) { Remove-Item $PidFile -Force }
    exit 0
}

if (-not (Test-Path $PidFile)) {
    Write-Warning "No tracked server found (server.pid missing). Use -All to kill every instance."
    exit 1
}

$trackedPid = [int](Get-Content $PidFile -Raw).Trim()
$proc = Get-Process -Id $trackedPid -ErrorAction SilentlyContinue
if (-not $proc) {
    Write-Warning "Tracked PID $trackedPid is no longer running."
    if (-not $DryRun) { Remove-Item $PidFile -Force }
    exit 0
}

Stop-Target $proc
if (-not $DryRun) { Remove-Item $PidFile -Force }
