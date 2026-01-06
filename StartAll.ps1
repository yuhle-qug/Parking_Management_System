param(
  [switch]$NoNgrok,
  [string]$FrontendSubdomain = ""
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$frontendDir = Join-Path $root 'frontend'
$backendDir = Join-Path $root 'backend'
$backendApiDir = Join-Path $backendDir 'Parking.API'
$devTunnelScript = Join-Path $frontendDir 'DevTunnel.ps1'
$ngrokConfig = Join-Path $root 'ngrok.yml'

function Write-Section([string]$text) {
  Write-Host ''
  Write-Host ('=' * 70)
  Write-Host $text
  Write-Host ('=' * 70)
}

function Get-Listener([int]$port) {
  Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1
}

function Clear-PortIfDotNet([int]$port) {
  $conn = Get-Listener -port $port
  if (-not $conn) { return }

  $processId = $conn.OwningProcess
  $proc = Get-Process -Id $processId -ErrorAction SilentlyContinue
  $name = if ($proc) { $proc.ProcessName } else { 'unknown' }

  if ($name -ieq 'dotnet') {
    Write-Host "Port $port is used by dotnet (PID=$processId). Stopping it..."
    Stop-Process -Id $processId -Force -ErrorAction SilentlyContinue
    Start-Sleep -Milliseconds 300
  } else {
    Write-Host "Port $port is already in use by PID=$processId ($name). Backend may fail to start."
  }
}

Write-Section 'Starting ParkingManagementSystem (Backend + Ngrok + Frontend Tunnel)'

# Backend: ensure port 5166 isn't held by a stale dotnet
Clear-PortIfDotNet -port 5166

Write-Host '1) Starting Backend API (dotnet run)...'
Start-Process -FilePath 'powershell.exe' -ArgumentList @(
  '-NoExit',
  '-ExecutionPolicy', 'Bypass',
  '-Command',
  "Set-Location '$backendApiDir'; dotnet run --launch-profile http"
) | Out-Null

if (-not $NoNgrok) {
  Write-Host '2) Starting ngrok tunnels...'
  if (-not (Test-Path $ngrokConfig)) {
    throw "Missing ngrok config: $ngrokConfig"
  }
  Start-Process -FilePath 'powershell.exe' -ArgumentList @(
    '-NoExit',
    '-ExecutionPolicy', 'Bypass',
    '-Command',
    "Set-Location '$root'; ngrok start --all --config='$ngrokConfig'"
  ) | Out-Null
} else {
  Write-Host '2) Skipping ngrok (NoNgrok switch set).'
}

Write-Host '3) Starting Frontend (Vite) + localtunnel...'
if (-not (Test-Path $devTunnelScript)) {
  throw "Missing frontend tunnel script: $devTunnelScript"
}
$frontendArgs = @(
  '-NoExit',
  '-ExecutionPolicy', 'Bypass',
  '-File', $devTunnelScript,
  '-Port', '5173'
)

if (-not [string]::IsNullOrWhiteSpace($FrontendSubdomain)) {
  $frontendArgs += @('-Subdomain', $FrontendSubdomain)
}

Start-Process -FilePath 'powershell.exe' -ArgumentList $frontendArgs | Out-Null

Write-Section 'Done'
Write-Host 'Backend: http://localhost:5166/swagger'
Write-Host 'Frontend: check the localtunnel window for the https://xxxxx.loca.lt URL'
if (-not $NoNgrok) {
  Write-Host 'Ngrok: check the ngrok window (backend hostname is in ngrok.yml)'
}
Write-Host ''
Write-Host 'Tip: Close the opened PowerShell windows to stop each service.'
