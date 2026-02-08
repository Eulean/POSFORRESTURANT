param(
  [string]$ApiUrl = "http://localhost:5268",
  [int]$IntervalSeconds = 30
)

Write-Host "Health monitor started for $ApiUrl/health" -ForegroundColor Cyan

while ($true) {
  try {
    $resp = Invoke-WebRequest -Uri "$ApiUrl/health" -UseBasicParsing -TimeoutSec 5
    if ($resp.StatusCode -ne 200) {
      Write-Warning "Health check status: $($resp.StatusCode)"
      [console]::beep(800, 300)
    }
  } catch {
    Write-Warning "Health check failed at $(Get-Date -Format T)"
    [console]::beep(800, 300)
  }
  Start-Sleep -Seconds $IntervalSeconds
}
