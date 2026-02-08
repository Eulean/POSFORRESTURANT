param(
  [string]$ZipPath,
  [string]$BackupsDir = "${PSScriptRoot}\\Backups"
)

$root = $PSScriptRoot
$apiPath = Join-Path $root "RestaurantPos.Api"
$dbPath = Join-Path $apiPath "restaurantpos.db"
$imagesPath = Join-Path $apiPath "wwwroot\\images"

if (-not $ZipPath) {
  if (-not (Test-Path $BackupsDir)) {
    Write-Error "Backups directory not found: $BackupsDir"
    exit 1
  }
  $latest = Get-ChildItem -Path $BackupsDir -Filter "RestaurantPos_*.zip" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
  if (-not $latest) {
    Write-Error "No backup zip found in $BackupsDir"
    exit 1
  }
  $ZipPath = $latest.FullName
}

if (-not (Test-Path $ZipPath)) {
  Write-Error "Backup zip not found: $ZipPath"
  exit 1
}

$tempDir = Join-Path $env:TEMP ("RestaurantPos_Restore_" + [guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Force -Path $tempDir | Out-Null

Expand-Archive -Path $ZipPath -DestinationPath $tempDir -Force

$srcDb = Join-Path $tempDir "restaurantpos.db"
if (-not (Test-Path $srcDb)) {
  Remove-Item -Recurse -Force $tempDir
  Write-Error "Backup zip missing restaurantpos.db"
  exit 1
}

Copy-Item -Path $srcDb -Destination $dbPath -Force

$srcImages = Join-Path $tempDir "images"
if (Test-Path $srcImages) {
  New-Item -ItemType Directory -Force -Path $imagesPath | Out-Null
  Copy-Item -Path (Join-Path $srcImages "*") -Destination $imagesPath -Recurse -Force
}

Remove-Item -Recurse -Force $tempDir

Write-Host "Restore completed from: $ZipPath"
