param(
  [string]$Mode = "dev"
)

$root = $PSScriptRoot
$apiPath = Join-Path $root "RestaurantPos.Api"
$webPath = Join-Path $root "RestaurantPos.Web"

Write-Host "Building web..."
Push-Location $webPath
npm install | Out-Null
npm run build
Pop-Location

$wwwrootApp = Join-Path $apiPath "wwwroot\app"
if (Test-Path $wwwrootApp) {
  Remove-Item -Recurse -Force $wwwrootApp
}
New-Item -ItemType Directory -Force -Path $wwwrootApp | Out-Null
Copy-Item -Recurse -Force (Join-Path $webPath "dist\*") $wwwrootApp

if ($Mode -eq "dev") {
  Write-Host "Starting API (dev)..."
  dotnet run --project $apiPath
} else {
  Write-Host "Starting API (release)..."
  dotnet run --project $apiPath --configuration Release
}
