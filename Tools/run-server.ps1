param(
    [string]$ExePath = ".\Builds\Pongo\Pong_MP_2025.exe",
    [ValidateSet(2, 4)][int]$MaxPlayers = 2,
    [string]$LogFile = ".\Logs\dedicated-server.log"
)

$PidFile = Join-Path (Join-Path (Join-Path $PSScriptRoot "..") "Logs") "server.pid"
$KillScript = Join-Path $PSScriptRoot "kill-servers.ps1"

& $KillScript -ErrorAction SilentlyContinue

$LogDir = Split-Path $LogFile -Parent
if ($LogDir -and -not (Test-Path $LogDir)) { New-Item -ItemType Directory -Force $LogDir | Out-Null }

$proc = Start-Process `
    -FilePath $ExePath `
    -ArgumentList "-batchmode", "-nographics", "-dedicated", "-maxPlayers", $MaxPlayers, "-logFile", $LogFile `
    -PassThru

$proc.Id | Set-Content $PidFile -Encoding UTF8
Write-Host "Server started: PID $($proc.Id), maxPlayers=$MaxPlayers, log=$LogFile"
