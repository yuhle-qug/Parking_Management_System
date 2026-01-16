[CmdletBinding()]
param(
	[int]$CustomerCount = 20,
	[int]$MonthlyTicketCount = 10,
	[int]$SessionCount = 60,
	[int]$ActiveSessionCount = 15,
	[int]$IncidentCount = 10,
	[int]$Seed = 12345,
	[switch]$NoBackup,
	[string]$OutDir = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

trap {
	Write-Host "SeedData.ps1 failed: $($_.Exception.Message)" -ForegroundColor Red
	if ($_.InvocationInfo -and $_.InvocationInfo.PositionMessage) {
		Write-Host $_.InvocationInfo.PositionMessage -ForegroundColor Red
	}
	exit 1
}

function Resolve-RepoRoot {
	if ([string]::IsNullOrWhiteSpace($PSScriptRoot)) {
		throw "Cannot resolve script directory. Please run as a .ps1 file."
	}
	return $PSScriptRoot
}

function Ensure-Dir([string]$path) {
	if (-not (Test-Path -LiteralPath $path)) {
		New-Item -ItemType Directory -Path $path | Out-Null
	}
}

function Write-JsonArray([string]$path, [object[]]$data) {
	$dir = Split-Path -Parent $path
	Ensure-Dir $dir
	$json = $data | ConvertTo-Json -Depth 20
	Set-Content -LiteralPath $path -Value $json -Encoding UTF8
}

function New-HexId([System.Random]$rng, [int]$length = 8) {
	$bytes = New-Object byte[] ($length)
	$rng.NextBytes($bytes)
	$hex = -join ($bytes | ForEach-Object { $_.ToString('X2') })
	return $hex.Substring(0, $length)
}

function Pick([System.Random]$rng, [object[]]$items) {
	return $items[$rng.Next(0, $items.Length)]
}

function New-VietnamPlate([System.Random]$rng, [string]$vehicleType) {
	$province = Pick $rng @('29','30','50','51','59','60','61')
	$letter = Pick $rng @('A','B','C','D','E','F','G','H','K','L')
	$digits1 = $rng.Next(10, 99)
	$digits2 = $rng.Next(1000, 99999)
	if ($vehicleType -like '*MOTORBIKE*') {
		$digits2 = $rng.Next(10000, 999999)
	}
	return "${province}${letter}-$digits1$digits2"
}

function To-LocalOffset([datetime]$dt) {
	return ([DateTimeOffset]$dt).ToOffset([TimeSpan]::FromHours(7))
}

$repoRoot = Resolve-RepoRoot
if ([string]::IsNullOrWhiteSpace($OutDir)) {
	$OutDir = Join-Path $repoRoot 'backend\Parking.API\DataStore'
}

Ensure-Dir $OutDir

$paths = [ordered]@{
	users = Join-Path $OutDir 'users.json'
	pricePolicies = Join-Path $OutDir 'price_policies.json'
	zones = Join-Path $OutDir 'zones.json'
	customers = Join-Path $OutDir 'customers.json'
	monthlyTickets = Join-Path $OutDir 'monthly_tickets.json'
	sessions = Join-Path $OutDir 'sessions.json'
	tickets = Join-Path $OutDir 'tickets.json'
	incidents = Join-Path $OutDir 'incidents.json'
}

if (-not $NoBackup) {
	$backupDir = Join-Path $OutDir (Join-Path '_backup' (Get-Date -Format 'yyyyMMdd-HHmmss'))
	Ensure-Dir $backupDir
	foreach ($k in $paths.Keys) {
		$p = $paths[$k]
		if (Test-Path -LiteralPath $p) {
			Copy-Item -LiteralPath $p -Destination (Join-Path $backupDir (Split-Path -Leaf $p)) -Force
		}
	}
}

$rng = [System.Random]::new($Seed)

# --- 1. Users ---
$users = @(
    [pscustomobject]@{ UserId = "USER001"; Username = "admin"; PasswordHash = "admin123"; FullName = "System Administrator"; Role = "Admin" },
    [pscustomobject]@{ UserId = "USER002"; Username = "staff01"; PasswordHash = "staff123"; FullName = "Nguyen Van A"; Role = "Staff" },
    [pscustomobject]@{ UserId = "USER003"; Username = "staff02"; PasswordHash = "staff123"; FullName = "Tran Thi B"; Role = "Staff" }
)

# --- 2. Price Policies ---
$pricePolicies = @(
    [pscustomobject]@{
        PolicyId = "POLICY-CAR-STD"
        Name = "O to Tieu chuan"
        VehicleType = "CAR"
        RatePerHour = 20000
        OvernightSurcharge = 50000
        DailyMax = 300000
        LostTicketFee = 500000
        PeakRanges = @(
            [pscustomobject]@{ StartHour = 17; EndHour = 20; Multiplier = 1.2 }
        )
    },
    [pscustomobject]@{
        PolicyId = "POLICY-MOTO-STD"
        Name = "Xe may Tieu chuan"
        VehicleType = "MOTORBIKE"
        RatePerHour = 5000
        OvernightSurcharge = 10000
        DailyMax = 50000
        LostTicketFee = 100000
        PeakRanges = @()
    },
    [pscustomobject]@{
        PolicyId = "POLICY-VIP"
        Name = "Khu VIP"
        VehicleType = "CAR"
        RatePerHour = 50000
        OvernightSurcharge = 100000
        DailyMax = 1000000
        LostTicketFee = 500000
        PeakRanges = @()
    }
)

# --- 3. Zones ---
$zones = @(
    [pscustomobject]@{ ZoneId = 'ZONE-A'; Name = 'Khu A (O to)'; VehicleCategory = 'CAR'; ElectricOnly = $false; Capacity = 50; PricePolicyId = "POLICY-CAR-STD"; ActiveSessions = @(); GateIds = @("GATE-01") },
    [pscustomobject]@{ ZoneId = 'ZONE-B'; Name = 'Khu B (Xe may)'; VehicleCategory = 'MOTORBIKE'; ElectricOnly = $false; Capacity = 100; PricePolicyId = "POLICY-MOTO-STD"; ActiveSessions = @(); GateIds = @("GATE-02") },
    [pscustomobject]@{ ZoneId = 'ZONE-VIP'; Name = 'Khu VIP'; VehicleCategory = 'CAR'; ElectricOnly = $false; Capacity = 10; PricePolicyId = "POLICY-VIP"; ActiveSessions = @(); GateIds = @("GATE-03") },
    [pscustomobject]@{ ZoneId = 'ZONE-E'; Name = 'Tram Sac Xe Dien'; VehicleCategory = 'CAR'; ElectricOnly = $true; Capacity = 20; PricePolicyId = "POLICY-CAR-STD"; ActiveSessions = @(); GateIds = @("GATE-01") }
)

# --- 4. Customers ---
$firstNames = @('An','Binh','Chi','Dang','Duy','Giang','Ha','Hung','Huy','Khanh','Lan','Linh','Minh','Nam','Ngan','Nga','Phuc','Phuong','Quang','Son','Thao','Trang','Tuan','Viet','Yen')
$lastNames = @('Nguyen','Tran','Le','Pham','Hoang','Vu','Phan','Vuong','Dang','Bui','Do','Ho','Ngo','Duong')
$customers = @()
for ($i = 0; $i -lt $CustomerCount; $i++) {
	$cid = [Guid]::NewGuid().ToString()
	$name = "$(Pick $rng $lastNames) $(Pick $rng $firstNames)"
	$phone = "0$($rng.Next(32, 99))$($rng.Next(1000000, 9999999))"
	$customers += [pscustomobject]@{
		CustomerId = $cid
		Name = $name
		Phone = $phone
		IdentityNumber = ""
	}
}

# --- 5. Monthly Tickets ---
$monthlyTickets = @()
$monthlyVehicleTypes = @('CAR','MOTORBIKE','ELECTRIC_CAR')
$usedMonthlyPlates = New-Object 'System.Collections.Generic.HashSet[string]'
$now = Get-Date

for ($i = 0; $i -lt $MonthlyTicketCount; $i++) {
	$customer = Pick $rng $customers
	$vt = Pick $rng $monthlyVehicleTypes
	$plate = New-VietnamPlate $rng $vt
    
    $attempts = 0
	while ($usedMonthlyPlates.Contains($plate) -and $attempts -lt 10) {
		$plate = New-VietnamPlate $rng $vt; $attempts++
	}
	$usedMonthlyPlates.Add($plate) | Out-Null

	$ticketId = "M-$(New-HexId $rng 8)"
	$start = $now.AddDays(-1 * $rng.Next(0, 25))
	$expiry = $start.AddDays(30)
	$status = if ($rng.NextDouble() -lt 0.8) { 'Active' } else { 'Expired' } # 80% active

	$monthlyFee = switch ($vt) {
		'CAR' { 1500000 }
		'MOTORBIKE' { 120000 }
		default { 1000000 }
	}

	$monthlyTickets += [pscustomobject]@{
		TicketId = $ticketId
		CustomerId = $customer.CustomerId
		VehiclePlate = $plate
		VehicleType = $vt
		StartDate = (To-LocalOffset $start).ToString('o')
		ExpiryDate = (To-LocalOffset $expiry).ToString('o')
		MonthlyFee = [double]$monthlyFee
		Status = $status
	}
}

# --- 6. Sessions & Tickets ---
$sessions = @()
$tickets = @()
$usedTicketIds = New-Object 'System.Collections.Generic.HashSet[string]'

function Get-SuitableZone($vType, $isElectric) {
    if ($vType -match 'CAR') {
        if ($isElectric) { return $zones | Where-Object { $_.ZoneId -eq 'ZONE-E' } | Select-Object -First 1 }
        # Chance for VIP
        if ($rng.NextDouble() -lt 0.1) { return $zones | Where-Object { $_.ZoneId -eq 'ZONE-VIP' } | Select-Object -First 1 }
        return $zones | Where-Object { $_.ZoneId -eq 'ZONE-A' } | Select-Object -First 1
    }
    return $zones | Where-Object { $_.ZoneId -eq 'ZONE-B' } | Select-Object -First 1
}

$vehicleTypes = @('CAR','MOTORBIKE','ELECTRIC_CAR')

# Create Active Sessions
for ($i = 0; $i -lt $ActiveSessionCount; $i++) {
	$vt = Pick $rng $vehicleTypes
    $isElectric = $vt -eq 'ELECTRIC_CAR'
	$zone = Get-SuitableZone $vt $isElectric
    $zoneId = $zone.ZoneId
    
	$entry = $now.AddMinutes(-1 * $rng.Next(5, 300))
	$plate = New-VietnamPlate $rng $vt
    $gateId = $zone.GateIds[0]

    # Check if monthly
    $monthlyIndex = $monthlyTickets | Where-Object { $_.VehiclePlate -eq $plate } | Select-Object -First 1
    $isMonthly = $monthlyIndex -ne $null
    $ticketId = if ($isMonthly) { $monthlyIndex.TicketId } else { New-HexId $rng 8 }

	$ticketObj = [pscustomobject]@{
		TicketId = $ticketId
		IssueTime = (To-LocalOffset $entry).ToString('o')
		GateId = $gateId
        CardId = if ($isMonthly) { $ticketId } else { $null }
	}
	$tickets += $ticketObj

	$sessions += [pscustomobject]@{
		SessionId = [Guid]::NewGuid().ToString()
		EntryTime = (To-LocalOffset $entry).ToString('o')
		ExitTime = $null
		FeeAmount = 0
		Status = 'Active'
		Vehicle = [pscustomobject]@{ VehicleType = $vt; LicensePlate = $plate }
		Ticket = $ticketObj
		Payment = $null
		ParkingZoneId = $zoneId
        CardId = $ticketObj.CardId
	}
}

# Create Completed Sessions
for ($i = 0; $i -lt ($SessionCount); $i++) {
	$vt = Pick $rng $vehicleTypes
    $isElectric = $vt -eq 'ELECTRIC_CAR'
	$zone = Get-SuitableZone $vt $isElectric
    $zoneId = $zone.ZoneId

	$entry = $now.AddDays(-1 * $rng.Next(0, 3)).AddMinutes(-1 * $rng.Next(10, 600))
	$duration = $rng.Next(30, 600)
    $exit = $entry.AddMinutes($duration)
	$plate = New-VietnamPlate $rng $vt
    $gateId = $zone.GateIds[0]

	$ticketId = New-HexId $rng 8
    
	$ticketObj = [pscustomobject]@{
		TicketId = $ticketId
		IssueTime = (To-LocalOffset $entry).ToString('o')
		GateId = $gateId
	}
	$tickets += $ticketObj

    # Simple Fee Calc
    $rate = if ($vt -match 'CAR') { 20000 } else { 5000 }
    $fee = [Math]::Ceiling($duration / 60.0) * $rate
    
	$paymentObj = [pscustomobject]@{
		PaymentId = [Guid]::NewGuid().ToString()
		Amount = $fee
		Time = (To-LocalOffset $exit).ToString('o')
		Method = 'QR'
		Status = 'Completed'
	}

	$sessions += [pscustomobject]@{
		SessionId = [Guid]::NewGuid().ToString()
		EntryTime = (To-LocalOffset $entry).ToString('o')
		ExitTime = (To-LocalOffset $exit).ToString('o')
		FeeAmount = $fee
		Status = 'Completed'
		Vehicle = [pscustomobject]@{ VehicleType = $vt; LicensePlate = $plate }
		Ticket = $ticketObj
		Payment = $paymentObj
		ParkingZoneId = $zoneId
	}
}

# --- 7. Incidents ---
$incidents = @()
for ($i = 0; $i -lt $IncidentCount; $i++) {
    $title = Pick $rng @("Barrier Fail", "Lost Ticket", "Payment Error")
    $incidents += [pscustomobject]@{
        IncidentId = "INC-$(New-HexId $rng 6)"
        ReportedDate = (To-LocalOffset $now.AddHours(-1*$rng.Next(1,48))).ToString('o')
        Title = $title
        Status = if ($rng.NextDouble() -gt 0.5) { "Resolved" } else { "Open" }
        Description = "$title detected at gate."
        ReportedBy = "System"
    }
}

Write-JsonArray $paths.users $users
Write-JsonArray $paths.pricePolicies $pricePolicies
Write-JsonArray $paths.zones $zones
Write-JsonArray $paths.customers $customers
Write-JsonArray $paths.monthlyTickets $monthlyTickets
Write-JsonArray $paths.tickets $tickets
Write-JsonArray $paths.sessions $sessions
Write-JsonArray $paths.incidents $incidents

Write-Host "Done. Data seeded to $OutDir"