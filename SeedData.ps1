[CmdletBinding()]
param(
	[int]$CustomerCount = 20,
	[int]$MonthlyTicketCount = 10,
	[int]$SessionCount = 60,
	[int]$ActiveSessionCount = 8,
	[int]$IncidentCount = 10,
	[int]$Seed = 42,
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

function Read-JsonArrayOrEmpty([string]$path) {
	if (-not (Test-Path -LiteralPath $path)) { return @() }
	try {
		$raw = Get-Content -LiteralPath $path -Raw -Encoding UTF8
		if ([string]::IsNullOrWhiteSpace($raw)) { return @() }
		$parsed = $raw | ConvertFrom-Json
		if ($null -eq $parsed) { return @() }
		if ($parsed -is [System.Array]) { return @($parsed) }
		# If someone accidentally stored an object, wrap it.
		return @($parsed)
	}
	catch {
		return @()
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
	# hex string length = 2*bytes; slice to requested chars
	$hex = -join ($bytes | ForEach-Object { $_.ToString('X2') })
	return $hex.Substring(0, $length)
}

function Pick([System.Random]$rng, [object[]]$items) {
	return $items[$rng.Next(0, $items.Length)]
}

function New-VietnamPlate([System.Random]$rng, [string]$vehicleType) {
	$province = Pick $rng @('11','12','14','15','18','29','30','31','32','33','34','36','37','38','43','47','50','51','59','60','61','62','63','65','66','67','68','69','70','71','72','73','74','75','76','77','78','79','81','82','83','84','85','86','88','89','90','92')
	$letter = Pick $rng @('A','B','C','D','E','F','G','H','K','L','M','N','P','S','T','U','V','X','Y','Z')
	$digits1 = $rng.Next(10, 99)
	$digits2 = $rng.Next(1000, 99999)
	# Motorbike plates are often longer; keep it flexible.
	if ($vehicleType -like '*MOTORBIKE*') {
		$digits2 = $rng.Next(10000, 999999)
	}
	return "${province}${letter}-$digits1$digits2"
}

function To-LocalOffset([datetime]$dt) {
	# Force +07:00 to match existing DataStore files.
	return ([DateTimeOffset]$dt).ToOffset([TimeSpan]::FromHours(7))
}

$repoRoot = Resolve-RepoRoot
if ([string]::IsNullOrWhiteSpace($OutDir)) {
	$OutDir = Join-Path $repoRoot 'backend\Parking.API\DataStore'
}

Ensure-Dir $OutDir

$paths = [ordered]@{
	customers = Join-Path $OutDir 'customers.json'
	monthlyTickets = Join-Path $OutDir 'monthly_tickets.json'
	sessions = Join-Path $OutDir 'sessions.json'
	tickets = Join-Path $OutDir 'tickets.json'
	incidents = Join-Path $OutDir 'incidents.json'
	zones = Join-Path $OutDir 'zones.json'
}

if (-not $NoBackup) {
	$backupDir = Join-Path $OutDir (Join-Path '_backup' (Get-Date -Format 'yyyyMMdd-HHmmss'))
	Ensure-Dir $backupDir
	foreach ($k in @('customers','monthlyTickets','sessions','tickets','incidents')) {
		$p = $paths[$k]
		if (Test-Path -LiteralPath $p) {
			Copy-Item -LiteralPath $p -Destination (Join-Path $backupDir (Split-Path -Leaf $p)) -Force
		}
	}
}

$zones = Read-JsonArrayOrEmpty $paths.zones
if ($zones.Count -eq 0) {
	# Fallback to the default zones used by the repo.
	$zones = @(
		[pscustomobject]@{ ZoneId = 'ZONE-A'; VehicleCategory = 'CAR'; ElectricOnly = $false },
		[pscustomobject]@{ ZoneId = 'ZONE-B'; VehicleCategory = 'MOTORBIKE'; ElectricOnly = $false },
		[pscustomobject]@{ ZoneId = 'ZONE-E'; VehicleCategory = 'CAR'; ElectricOnly = $true }
	)
}

$rng = [System.Random]::new($Seed)

# --- Customers ---
$firstNames = @('An','Binh','Chi','Dang','Duy','Giang','Ha','Hung','Huy','Khanh','Lan','Linh','Minh','Nam','Ngan','Nga','Phuc','Phuong','Quang','Son','Thao','Trang','Tuan','Viet','Yen')
$lastNames = @('Nguyen','Tran','Le','Pham','Hoang','Vu','Phan','Vuong','Dang','Bui','Do','Ho','Ngo','Duong')

$customers = @()
for ($i = 0; $i -lt $CustomerCount; $i++) {
	$cid = [Guid]::NewGuid().ToString()
	$name = "$(Pick $rng $lastNames) $(Pick $rng $firstNames)"
	$phone = "0$($rng.Next(32, 99))$($rng.Next(1000000, 9999999))"
	$identity = "" # existing data uses empty string often
	$customers += [pscustomobject]@{
		CustomerId = $cid
		Name = $name
		Phone = $phone
		IdentityNumber = $identity
	}
}

# --- Monthly tickets ---
$monthlyTickets = @()
$monthlyVehicleTypes = @('CAR','MOTORBIKE','ELECTRIC_CAR','ELECTRIC_MOTORBIKE')
$usedMonthlyPlates = New-Object 'System.Collections.Generic.HashSet[string]'

$now = Get-Date
for ($i = 0; $i -lt $MonthlyTicketCount; $i++) {
	$customer = Pick $rng $customers
	$vt = Pick $rng $monthlyVehicleTypes
	$plate = New-VietnamPlate $rng $vt
	# ensure plate uniqueness among monthly tickets for nicer demo data
	$attempts = 0
	while ($usedMonthlyPlates.Contains($plate) -and $attempts -lt 10) {
		$plate = New-VietnamPlate $rng $vt
		$attempts++
	}
	$usedMonthlyPlates.Add($plate) | Out-Null

	$ticketId = "M-$(New-HexId $rng 8)"
	$start = $now.AddDays(-1 * $rng.Next(0, 25)).AddHours(-1 * $rng.Next(0, 24))
	$expiry = $start.AddDays(30)
	$status = if ($rng.NextDouble() -lt 0.2) { 'Expired' } else { 'Active' }
	if ($status -eq 'Expired') {
		$expiry = $now.AddDays(-1 * $rng.Next(1, 10))
	}

	$monthlyFee = switch ($vt.ToUpperInvariant()) {
		'ELECTRIC_CAR' { 1000000; break }
		'ELECTRIC_MOTORBIKE' { 100000; break }
		'MOTORBIKE' { 120000; break }
		default { 1500000; break }
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

# --- Sessions & tickets ---
$sessions = @()
$tickets = @()
$usedTicketIds = New-Object 'System.Collections.Generic.HashSet[string]'

$gateIds = @('GATE-01','GATE-02')
$vehicleTypes = @('CAR','MOTORBIKE','ELECTRIC_CAR','ELECTRIC_MOTORBIKE')

function Pick-ZoneId([System.Random]$rng, [object[]]$zones, [string]$vehicleType) {
	$vt = $vehicleType
	if ($null -eq $vt) { $vt = '' }
	$vt = $vt.ToUpperInvariant()
	$isElectric = $vt -like 'ELECTRIC*'
	if ($vt.Contains('CAR')) {
		if ($isElectric) {
			$z = $zones | Where-Object {
				$cat = $_.VehicleCategory
				if ($null -eq $cat) { $cat = '' }
				$_.ElectricOnly -eq $true -and $cat.ToUpperInvariant() -eq 'CAR'
			} | Select-Object -First 1
			if ($null -ne $z) { return $z.ZoneId }
		}
		$z = $zones | Where-Object {
			$cat = $_.VehicleCategory
			if ($null -eq $cat) { $cat = '' }
			$_.ElectricOnly -ne $true -and $cat.ToUpperInvariant() -eq 'CAR'
		} | Select-Object -First 1
		if ($null -ne $z -and -not [string]::IsNullOrWhiteSpace($z.ZoneId)) { return $z.ZoneId }
		return 'ZONE-A'
	}
	if ($vt.Contains('MOTORBIKE')) {
		$z = $zones | Where-Object {
			$cat = $_.VehicleCategory
			if ($null -eq $cat) { $cat = '' }
			$_.ElectricOnly -ne $true -and $cat.ToUpperInvariant() -eq 'MOTORBIKE'
		} | Select-Object -First 1
		if ($null -ne $z -and -not [string]::IsNullOrWhiteSpace($z.ZoneId)) { return $z.ZoneId }
		return 'ZONE-B'
	}
	return (Pick $rng ($zones | ForEach-Object { $_.ZoneId }))
}

function New-UniqueTicketId([System.Random]$rng, [System.Collections.Generic.HashSet[string]]$used, [string]$prefix = '') {
	for ($i = 0; $i -lt 50; $i++) {
		$id = "${prefix}$(New-HexId $rng 8)"
		if (-not $used.Contains($id)) {
			$used.Add($id) | Out-Null
			return $id
		}
	}
	throw 'Failed to generate unique TicketId'
}

# Create some active sessions first (no ExitTime/Payment)
for ($i = 0; $i -lt $ActiveSessionCount; $i++) {
	$vt = Pick $rng $vehicleTypes
	$zoneId = Pick-ZoneId $rng $zones $vt
	$plate = New-VietnamPlate $rng $vt
	$entry = $now.AddMinutes(-1 * $rng.Next(1, 360)).AddSeconds(-1 * $rng.Next(0, 59))
	$gateId = Pick $rng $gateIds

	# 20% chance this is a monthly ticket (free entry)
	$monthly = $monthlyTickets | Where-Object { $_.VehiclePlate -eq $plate -and $_.Status -eq 'Active' } | Select-Object -First 1
	$useMonthly = $false
	if ($null -ne $monthly -and $rng.NextDouble() -lt 0.8) { $useMonthly = $true }
	if (-not $useMonthly -and $rng.NextDouble() -lt 0.2 -and $monthlyTickets.Count -gt 0) {
		$monthly = Pick $rng $monthlyTickets
		if ($monthly.Status -eq 'Active') {
			$plate = $monthly.VehiclePlate
			$vt = $monthly.VehicleType
			$zoneId = Pick-ZoneId $rng $zones $vt
			$useMonthly = $true
		}
	}

	$ticketId = if ($useMonthly -and $null -ne $monthly) { $monthly.TicketId } else { New-UniqueTicketId $rng $usedTicketIds '' }

	$ticketObj = [pscustomobject]@{
		TicketId = $ticketId
		IssueTime = (To-LocalOffset $entry).ToString('o')
		GateId = $gateId
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
	}
}

# Completed sessions
for ($i = 0; $i -lt ($SessionCount - $ActiveSessionCount); $i++) {
	$vt = Pick $rng $vehicleTypes
	$zoneId = Pick-ZoneId $rng $zones $vt
	$plate = New-VietnamPlate $rng $vt
	$entry = $now.AddDays(-1 * $rng.Next(0, 7)).AddMinutes(-1 * $rng.Next(10, 12 * 60))
	$durationMinutes = $rng.Next(2, 8 * 60)
	$exit = $entry.AddMinutes($durationMinutes).AddSeconds($rng.Next(0, 59))
	$gateId = Pick $rng $gateIds

	# 25% chance this is a monthly ticket (no payment, fee 0)
	$useMonthly = $false
	$monthly = $null
	if ($rng.NextDouble() -lt 0.25 -and $monthlyTickets.Count -gt 0) {
		$monthly = Pick $rng $monthlyTickets
		if ($monthly.Status -eq 'Active') {
			$useMonthly = $true
			$plate = $monthly.VehiclePlate
			$vt = $monthly.VehicleType
			$zoneId = Pick-ZoneId $rng $zones $vt
		}
	}

	$ticketId = if ($useMonthly -and $null -ne $monthly) { $monthly.TicketId } else { New-UniqueTicketId $rng $usedTicketIds '' }

	$ticketObj = [pscustomobject]@{
		TicketId = $ticketId
		IssueTime = (To-LocalOffset $entry).ToString('o')
		GateId = $gateId
	}
	$tickets += $ticketObj

	$feeBasePerHour = if ($vt.ToUpperInvariant().Contains('MOTORBIKE')) { 10000 } else { 20000 }
	$factor = switch ($vt.ToUpperInvariant()) {
		'ELECTRIC_CAR' { 0.8 }
		'ELECTRIC_MOTORBIKE' { 0.8 }
		default { 1.0 }
	}
	$hours = [Math]::Ceiling($durationMinutes / 60.0)
	$fee = [int]([Math]::Round($feeBasePerHour * $hours * $factor))
	if ($useMonthly) { $fee = 0 }

	$paymentObj = $null
	if (-not $useMonthly -and $fee -gt 0) {
		$paymentObj = [pscustomobject]@{
			PaymentId = [Guid]::NewGuid().ToString()
			Amount = $fee
			Time = (To-LocalOffset $exit.AddSeconds($rng.Next(5, 60))).ToString('o')
			Method = 'Cash/QR'
			Status = 'Completed'
		}
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

# De-dupe tickets (same monthly ticket can appear multiple times)
$tickets = $tickets | Group-Object TicketId | ForEach-Object { $_.Group | Select-Object -First 1 }

# --- Incidents ---
$incidentTitles = @(
	'Barrier khong mo',
	'Mat ve xe',
	'Khong quet duoc QR',
	'Loi tinh phi',
	'Sai khu vuc do',
	'Ket cong ra'
)

$incidents = @()
for ($i = 0; $i -lt $IncidentCount; $i++) {
	$id = "INC-$(New-HexId $rng 8)"
	$reportedDate = $now.AddDays(-1 * $rng.Next(0, 14)).AddMinutes(-1 * $rng.Next(0, 12 * 60))
	$title = Pick $rng $incidentTitles
	$status = if ($rng.NextDouble() -lt 0.55) { 'Resolved' } else { 'Open' }
	$ref = if ($rng.NextDouble() -lt 0.5 -and $tickets.Count -gt 0) { (Pick $rng $tickets).TicketId } else { (Pick $rng $sessions).Vehicle.LicensePlate }
	$desc = switch ($title) {
		'Barrier khong mo' { 'Barrier khong mo khi quet ve.' }
		'Mat ve xe' { "Khach bao mat ve, ref: $ref" }
		default { "Su co phat sinh, ref: $ref" }
	}
	$resNotes = if ($status -eq 'Resolved') { 'Da kiem tra va xu ly.' } else { $null }

	$incidents += [pscustomobject]@{
		IncidentId = $id
		ReportedDate = (To-LocalOffset $reportedDate).ToString('o')
		Title = $title
		Description = $desc
		Status = $status
		ReportedBy = 'admin'
		ReferenceId = $ref
		ResolutionNotes = $resNotes
	}
}

Write-JsonArray $paths.customers $customers
Write-JsonArray $paths.monthlyTickets $monthlyTickets
Write-JsonArray $paths.tickets $tickets
Write-JsonArray $paths.sessions $sessions
Write-JsonArray $paths.incidents $incidents

Write-Host "Seeded data written to: $OutDir"
Write-Host "- customers.json:        $($customers.Count)"
Write-Host "- monthly_tickets.json:  $($monthlyTickets.Count)"
Write-Host "- tickets.json:          $($tickets.Count)"
Write-Host "- sessions.json:         $($sessions.Count)"
Write-Host "- incidents.json:        $($incidents.Count)"