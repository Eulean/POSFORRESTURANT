param(
  [string]$TaskName = "RestaurantPosNightlyBackup",
  [string]$Time = "02:00"
)

$root = $PSScriptRoot
$backupScript = Join-Path $root "RestaurantPos.Backup.ps1"

if (-not (Test-Path $backupScript)) {
  Write-Error "Backup script not found: $backupScript"
  exit 1
}

$taskCommand = "powershell -NoProfile -ExecutionPolicy Bypass -File `"$backupScript`""

schtasks /Create /F /SC DAILY /TN $TaskName /TR $taskCommand /ST $Time
if ($LASTEXITCODE -ne 0) {
  Write-Error "Failed to create scheduled task."
  exit 1
}

Write-Host "Scheduled task created: $TaskName (daily at $Time)"
