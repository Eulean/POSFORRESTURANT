$ErrorActionPreference = 'Stop'
$pidPath = Join-Path (Join-Path $env:LOCALAPPDATA 'RestaurantPos') 'server.pid'
$expectedExe = Join-Path $PSScriptRoot 'RestaurantPos.Api.exe'
if (-not (Test-Path -LiteralPath $pidPath)) {
    Write-Host 'Restaurant POS is not running from this folder.'
    exit 0
}
$serverId = [int](Get-Content -LiteralPath $pidPath -Raw)
$process = Get-Process -Id $serverId -ErrorAction SilentlyContinue
if ($process -and $process.Path -eq $expectedExe) {
    Stop-Process -Id $serverId
    Write-Host 'Restaurant POS stopped.'
} else {
    Write-Host 'The saved POS process is no longer running.'
}
Remove-Item -LiteralPath $pidPath -ErrorAction SilentlyContinue
