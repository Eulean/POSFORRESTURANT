param(
  [string]$SitePath = "C:\\inetpub\\RestaurantPos",
  [string]$SiteName = "RestaurantPos",
  [string]$AppPool = "RestaurantPos",
  [switch]$SkipIisRestart,
  [switch]$SkipBackup,
  [string]$BackupsDir = "$PSScriptRoot\\deploy-backups"
)

$root = $PSScriptRoot
$apiPath = Join-Path $root "RestaurantPos.Api"
$webPath = Join-Path $root "RestaurantPos.Web"
$publishPath = Join-Path $root "publish"

if (-not $SkipBackup) {
  if (Test-Path $SitePath) {
    New-Item -ItemType Directory -Force -Path $BackupsDir | Out-Null
    $stamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $backupPath = Join-Path $BackupsDir "RestaurantPos_$stamp"
    Write-Host "Backing up current deploy to $backupPath..."
    New-Item -ItemType Directory -Force -Path $backupPath | Out-Null
    robocopy $SitePath $backupPath /MIR /NFL /NDL /NJH /NJS /NP | Out-Null
  }
}

Write-Host "Building web..."
Push-Location $webPath
npm install | Out-Null
npm run build
Pop-Location

$wwwrootApp = Join-Path $apiPath "wwwroot\\app"
if (Test-Path $wwwrootApp) {
  Remove-Item -Recurse -Force $wwwrootApp
}
New-Item -ItemType Directory -Force -Path $wwwrootApp | Out-Null
Copy-Item -Recurse -Force (Join-Path $webPath "dist\\*") $wwwrootApp

Write-Host "Publishing API..."
dotnet publish $apiPath -c Release -o $publishPath

if (-not (Test-Path $SitePath)) {
  New-Item -ItemType Directory -Force -Path $SitePath | Out-Null
}

Write-Host "Deploying to $SitePath..."
robocopy $publishPath $SitePath /MIR /NFL /NDL /NJH /NJS /NP | Out-Null

if (-not $SkipIisRestart) {
  try {
    Write-Host "Restarting IIS app pool $AppPool..."
    & "$env:windir\\system32\\inetsrv\\appcmd.exe" recycle apppool /apppool.name:"$AppPool" | Out-Null
  } catch {
    Write-Warning "Could not restart app pool. Ensure IIS is installed and AppPool name is correct."
  }
}

Write-Host "Deploy complete." -ForegroundColor Green
