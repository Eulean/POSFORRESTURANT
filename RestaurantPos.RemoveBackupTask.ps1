param(
  [string]$TaskName = "RestaurantPosNightlyBackup"
)

schtasks /Query /TN $TaskName > $null 2>&1
if ($LASTEXITCODE -ne 0) {
  Write-Host "Task not found: $TaskName"
  exit 0
}

schtasks /Delete /TN $TaskName /F
if ($LASTEXITCODE -ne 0) {
  Write-Error "Failed to delete task: $TaskName"
  exit 1
}

Write-Host "Task removed: $TaskName"
