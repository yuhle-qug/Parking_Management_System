param(
  [int]$Port = 5173,
  [string]$Subdomain = ""
)

$ErrorActionPreference = 'Stop'

function Stop-ListenerOnPort([int]$p) {
  $conn = Get-NetTCPConnection -LocalPort $p -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1
  if (-not $conn) { return }

  $processId = $conn.OwningProcess
  $proc = Get-Process -Id $processId -ErrorAction SilentlyContinue
  $name = if ($proc) { $proc.ProcessName } else { 'unknown' }

  Write-Host "Port $p is currently used by PID=$processId ($name). Stopping it..."
  Stop-Process -Id $processId -Force -ErrorAction SilentlyContinue
  Start-Sleep -Milliseconds 400
}

try {
  Push-Location $PSScriptRoot

  # Ensure we don't point the tunnel at an old/stale Vite instance.
  Stop-ListenerOnPort -p $Port

  Write-Host "Starting Vite on 0.0.0.0:$Port ..."
  $vite = Start-Process -FilePath "cmd.exe" -ArgumentList "/c", "npx vite --host 0.0.0.0 --port $Port --strictPort" -PassThru

  # Wait until the port is listening.
  $deadline = (Get-Date).AddSeconds(20)
  while ((Get-Date) -lt $deadline) {
    $conn = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($conn) { break }
    Start-Sleep -Milliseconds 250
  }

  $conn = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1
  if (-not $conn) {
    throw "Vite did not start listening on port $Port within 20 seconds."
  }

  Write-Host "Starting localtunnel for port $Port ..."
  Write-Host "(Use Ctrl+C to stop the tunnel. Close the Vite window to stop Vite.)"

  function Normalize-Subdomain([string]$s) {
    if ([string]::IsNullOrWhiteSpace($s)) { return "" }
    $x = $s.ToLowerInvariant()
    # localtunnel expects something URL-safe-ish; keep letters/numbers/hyphen only
    $x = ($x -replace '[^a-z0-9-]', '-')
    $x = ($x -replace '-{2,}', '-')
    $x = $x.Trim('-')
    if ($x.Length -gt 40) { $x = $x.Substring(0, 40).Trim('-') }
    return $x
  }

  if ([string]::IsNullOrWhiteSpace($Subdomain)) {
    $auto = "pms-frontend-$($env:COMPUTERNAME)-$($env:USERNAME)"
    $Subdomain = Normalize-Subdomain $auto
  } else {
    $Subdomain = Normalize-Subdomain $Subdomain
  }

  if (-not [string]::IsNullOrWhiteSpace($Subdomain)) {
    Write-Host "Requested fixed URL: https://$Subdomain.loca.lt"
    npx localtunnel --port $Port --subdomain $Subdomain

    if ($LASTEXITCODE -ne 0) {
      Write-Host ""
      Write-Host "Localtunnel failed to claim subdomain '$Subdomain'." -ForegroundColor Yellow
      Write-Host "Either it's taken or temporarily unavailable." -ForegroundColor Yellow
      Write-Host "Retrying without fixed subdomain (URL will change each run)..." -ForegroundColor Yellow
      Write-Host "Tip: pick another with: .\\DevTunnel.ps1 -Subdomain your-name" -ForegroundColor Yellow
      Write-Host ""
      npx localtunnel --port $Port
    }
  } else {
    npx localtunnel --port $Port
  }
}
finally {
  Pop-Location
}
