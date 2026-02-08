param(
  [string]$SitePath = "C:\\inetpub\\RestaurantPos",
  [string]$BackupsDir = "$PSScriptRoot\\deploy-backups",
  [string]$BackupPath,
  [string]$AppPool = "RestaurantPos",
  [switch]$SkipIisRestart
)

if (-not (Test-Path $BackupsDir)) {
  Write-Error "Backups directory not found: $BackupsDir"
  exit 1
}

if (-not $BackupPath) {
  $latest = Get-ChildItem -Path $BackupsDir -Directory | Sort-Object LastWriteTime -Descending | Select-Object -First 1
  if (-not $latest) {
    Write-Error "No backups found in $BackupsDir"
    exit 1
  }
  $BackupPath = $latest.FullName
}

if (-not (Test-Path $BackupPath)) {
  Write-Error "Backup path not found: $BackupPath"
  exit 1
}

if (-not (Test-Path $SitePath)) {
  New-Item -ItemType Directory -Force -Path $SitePath | Out-Null
}

Write-Host "Restoring deploy from $BackupPath to $SitePath..."
robocopy $BackupPath $SitePath /MIR /NFL /NDL /NJH /NJS /NP | Out-Null

if (-not $SkipIisRestart) {
  try {
    Write-Host "Restarting IIS app pool $AppPool..."
    & "$env:windir\\system32\\inetsrv\\appcmd.exe" recycle apppool /apppool.name:"$AppPool" | Out-Null
  } catch {
    Write-Warning "Could not restart app pool. Ensure IIS is installed and AppPool name is correct."
  }
}

Write-Host "Rollback complete." -ForegroundColor Green
