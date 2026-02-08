param(
  [string]$OutDir = "${PSScriptRoot}\\Backups"
)

$root = $PSScriptRoot
$apiPath = Join-Path $root "RestaurantPos.Api"
$dbPath = Join-Path $apiPath "restaurantpos.db"
$imagesPath = Join-Path $apiPath "wwwroot\\images"

if (-not (Test-Path $dbPath)) {
  Write-Error "Database not found at $dbPath"
  exit 1
}

$stamp = Get-Date -Format "yyyyMMdd_HHmmss"
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
$tempDir = Join-Path $OutDir "RestaurantPos_$stamp"
New-Item -ItemType Directory -Force -Path $tempDir | Out-Null

Copy-Item -Path $dbPath -Destination (Join-Path $tempDir "restaurantpos.db") -Force
if (Test-Path $imagesPath) {
  Copy-Item -Path $imagesPath -Destination (Join-Path $tempDir "images") -Recurse -Force
}

$zipPath = Join-Path $OutDir "RestaurantPos_$stamp.zip"
if (Test-Path $zipPath) {
  Remove-Item -Force $zipPath
}

Compress-Archive -Path (Join-Path $tempDir "*") -DestinationPath $zipPath
Remove-Item -Recurse -Force $tempDir

Write-Host "Backup created: $zipPath"
