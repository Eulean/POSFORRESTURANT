param(
  [string]$ApiUrl = "http://localhost:5268",
  [string]$Environment = "Production"
)

$root = $PSScriptRoot
$apiPath = Join-Path $root "RestaurantPos.Api"
$appSettingsProd = Join-Path $apiPath "appsettings.Production.json"
$dbPath = Join-Path $apiPath "restaurantpos.db"

Write-Host "Go-Live checks" -ForegroundColor Cyan

if ($Environment -ne "Production") {
  Write-Warning "Environment is '$Environment' (expected Production)."
} else {
  Write-Host "Environment: Production" -ForegroundColor Green
}

if (-not (Test-Path $appSettingsProd)) {
  Write-Error "Missing appsettings.Production.json"
  exit 1
}

$appSettings = Get-Content -Raw -Path $appSettingsProd | ConvertFrom-Json
$jwtKey = $appSettings.Jwt.Key
$envJwtKey = $env:Jwt__Key
$corsOrigins = $appSettings.Cors.Origins
$connectionString = $appSettings.ConnectionStrings.DefaultConnection

if ([string]::IsNullOrWhiteSpace($jwtKey) -or $jwtKey -eq "") {
  if (-not [string]::IsNullOrWhiteSpace($envJwtKey)) {
    $jwtKey = $envJwtKey
  }
}

if ([string]::IsNullOrWhiteSpace($jwtKey) -or $jwtKey -like "*REPLACE_WITH_SECURE*") {
  Write-Error "Jwt:Key is not set to a secure value."
  exit 1
}

if (-not $corsOrigins -or $corsOrigins.Count -eq 0) {
  Write-Error "Cors:Origins is empty."
  exit 1
}

Write-Host "Config check: OK" -ForegroundColor Green

$dataSource = ""
if ($connectionString -match "Data Source=([^;]+)") {
  $dataSource = $Matches[1]
}

if ([string]::IsNullOrWhiteSpace($dataSource)) {
  Write-Warning "Could not determine database path from connection string."
} else {
  if (-not (Test-Path $dataSource)) {
    Write-Warning "Database not found at $dataSource."
  } else {
    Write-Host "Database found" -ForegroundColor Green
  }
}

try {
  $healthUrl = "$ApiUrl/health"
  $resp = Invoke-WebRequest -Uri $healthUrl -UseBasicParsing -TimeoutSec 5
  if ($resp.StatusCode -eq 200) {
    Write-Host "Health check: OK" -ForegroundColor Green
  } else {
    Write-Warning "Health check returned status $($resp.StatusCode)"
  }
} catch {
  Write-Warning "Health check failed. Is the API running at $ApiUrl?"
}

Write-Host "Go-Live checks complete." -ForegroundColor Cyan
