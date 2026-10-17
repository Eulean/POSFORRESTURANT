param(
    [string]$OutputDirectory = (Join-Path $PSScriptRoot 'out\RestaurantPos-LocalHost')
)

$ErrorActionPreference = 'Stop'
$webDirectory = Join-Path $PSScriptRoot 'RestaurantPos.Web'
$apiProject = Join-Path $PSScriptRoot 'RestaurantPos.Api\RestaurantPos.Api.csproj'
$resolvedOutput = [System.IO.Path]::GetFullPath($OutputDirectory)
$outRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot 'out'))
if (-not $resolvedOutput.StartsWith($outRoot + [System.IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'OutputDirectory must be inside this repository\out.'
}

Write-Host 'Building offline web app...'
Push-Location $webDirectory
try {
    npm ci
    if ($LASTEXITCODE -ne 0) { throw 'npm ci failed.' }
    npm run build
    if ($LASTEXITCODE -ne 0) { throw 'Web build failed.' }
} finally { Pop-Location }

Write-Host 'Publishing self-contained Windows server...'
dotnet publish $apiProject -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o $resolvedOutput
if ($LASTEXITCODE -ne 0) { throw 'Server publish failed.' }

$appDirectory = Join-Path $resolvedOutput 'wwwroot\app'
New-Item -ItemType Directory -Path $appDirectory -Force | Out-Null
Copy-Item -Path (Join-Path $webDirectory 'dist\*') -Destination $appDirectory -Recurse -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'LocalHostLauncher.ps1') -Destination $resolvedOutput -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Allow-LocalHostFirewall.ps1') -Destination $resolvedOutput -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Start Restaurant POS.cmd') -Destination $resolvedOutput -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Stop Restaurant POS.cmd') -Destination $resolvedOutput -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Stop-LocalHost.ps1') -Destination $resolvedOutput -Force
foreach ($unusedConfig in @('appsettings.Development.json', 'appsettings.Production.json')) {
    $unusedPath = Join-Path $resolvedOutput $unusedConfig
    if (Test-Path -LiteralPath $unusedPath) { Remove-Item -LiteralPath $unusedPath -Force }
}

$zipPath = Join-Path $outRoot 'RestaurantPos-LocalHost.zip'
Compress-Archive -Path (Join-Path $resolvedOutput '*') -DestinationPath $zipPath -Force

Write-Host "Ready: $resolvedOutput"
Write-Host "ZIP for copying: $zipPath"
Write-Host 'Extract the ZIP on the restaurant laptop, then double-click Start Restaurant POS.cmd.'
