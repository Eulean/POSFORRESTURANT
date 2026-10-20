param(
    [string]$OutputDirectory = ''
)

$ErrorActionPreference = 'Stop'
$webDirectory = Join-Path $PSScriptRoot 'RestaurantPos.Web'
$apiProject = Join-Path $PSScriptRoot 'RestaurantPos.Api\RestaurantPos.Api.csproj'
$outRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot 'out'))
$temporaryOutput = [string]::IsNullOrWhiteSpace($OutputDirectory)
if ($temporaryOutput) { $OutputDirectory = Join-Path $outRoot ('.package-' + [Guid]::NewGuid().ToString('N')) }
$resolvedOutput = [System.IO.Path]::GetFullPath($OutputDirectory)
if (-not $resolvedOutput.StartsWith($outRoot + [System.IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'OutputDirectory must be inside this repository\out.'
}

try {
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
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Restore-LocalHost.ps1') -Destination $resolvedOutput -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Restore Restaurant POS.cmd') -Destination $resolvedOutput -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'USER_GUIDE_MY.md') -Destination $resolvedOutput -Force
foreach ($unusedConfig in @('appsettings.Development.json', 'appsettings.Production.json')) {
    $unusedPath = Join-Path $resolvedOutput $unusedConfig
    if (Test-Path -LiteralPath $unusedPath) { Remove-Item -LiteralPath $unusedPath -Force }
}

$zipPath = Join-Path $outRoot 'RestaurantPos-LocalHost.zip'
Compress-Archive -Path (Join-Path $resolvedOutput '*') -DestinationPath $zipPath -Force

if (-not $temporaryOutput) { Write-Host "Release folder: $resolvedOutput" }
Write-Host "ZIP for copying: $zipPath"
Write-Host 'Extract the ZIP on the restaurant laptop, then double-click Start Restaurant POS.cmd.'
} finally {
    if ($temporaryOutput -and (Test-Path -LiteralPath $resolvedOutput) -and
        $resolvedOutput.StartsWith($outRoot + [System.IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        Remove-Item -LiteralPath $resolvedOutput -Recurse -Force
    }
}
