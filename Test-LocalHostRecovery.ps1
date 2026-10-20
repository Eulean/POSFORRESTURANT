$ErrorActionPreference = 'Stop'
$testRoot = Join-Path ([System.IO.Path]::GetTempPath()) ('pos-recovery-test-' + [Guid]::NewGuid().ToString('N'))
$dataDirectory = Join-Path $testRoot 'RestaurantPos'
$apiDll = Join-Path $PSScriptRoot 'RestaurantPos.Api\bin\Release\net8.0\RestaurantPos.Api.dll'
if (-not (Test-Path -LiteralPath $apiDll)) { throw 'Build the Release API before running this test.' }
$previousConfig = $env:RESTAURANTPOS_CONFIG_PATH
$previousEnvironment = $env:ASPNETCORE_ENVIRONMENT
$previousBootstrap = $env:RESTAURANTPOS_BOOTSTRAP_PASSWORD
$server = $null

try {
    New-Item -ItemType Directory -Path $dataDirectory -Force | Out-Null
    $database = Join-Path $dataDirectory 'restaurantpos.db'
    $configPath = Join-Path $testRoot 'test-config.json'
    @{
        ConnectionStrings = @{ DefaultConnection = "Data Source=$database" }
        Jwt = @{ Key = ('test-secret-' * 5); Issuer = 'RestaurantPos'; Audience = 'RestaurantPos' }
        LocalHost = @{ DataDirectory = $dataDirectory; Port = 0 }
    } | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $configPath
    $env:RESTAURANTPOS_CONFIG_PATH = $configPath
    $env:ASPNETCORE_ENVIRONMENT = 'LocalHost'
    $env:RESTAURANTPOS_BOOTSTRAP_PASSWORD = 'test-password-123'
    $server = Start-Process -FilePath 'dotnet' -ArgumentList @($apiDll, '--urls', 'http://127.0.0.1:0') -WorkingDirectory (Join-Path $PSScriptRoot 'RestaurantPos.Api') -PassThru -WindowStyle Hidden -RedirectStandardOutput (Join-Path $testRoot 'server.out') -RedirectStandardError (Join-Path $testRoot 'server.err')
    $ready = $false
    for ($attempt = 0; $attempt -lt 60; $attempt++) {
        Start-Sleep -Milliseconds 500
        if ($server.HasExited) { throw "Test server exited: $(Get-Content -LiteralPath (Join-Path $testRoot 'server.err') -Raw)" }
        if ((Test-Path -LiteralPath $database) -and
            @(Get-ChildItem -LiteralPath (Join-Path $dataDirectory 'Backups') -Filter 'RestaurantPos-*.zip' -ErrorAction SilentlyContinue).Count -gt 0 -and
            (Test-Path -LiteralPath (Join-Path $testRoot 'server.out')) -and
            (Select-String -LiteralPath (Join-Path $testRoot 'server.out') -Pattern 'Now listening on:' -Quiet)) { $ready = $true; break }
    }
    if (-not $ready) { throw 'Test database did not initialize.' }
    Stop-Process -Id $server.Id
    $server.WaitForExit()
    $server = $null
    Set-Content -LiteralPath (Join-Path $dataDirectory 'logo.png') -Value 'before' -NoNewline

    $source = Join-Path $testRoot 'source'
    New-Item -ItemType Directory -Path $source | Out-Null
    $startupZip = Get-ChildItem -LiteralPath (Join-Path $dataDirectory 'Backups') -Filter 'RestaurantPos-*.zip' | Select-Object -First 1
    Expand-Archive -LiteralPath $startupZip.FullName -DestinationPath $source
    Set-Content -LiteralPath (Join-Path $source 'logo.png') -Value 'after' -NoNewline
    $validZip = Join-Path $testRoot 'valid.zip'
    Compress-Archive -Path (Join-Path $source '*') -DestinationPath $validZip
    & dotnet $apiDll --restore-backup $validZip $dataDirectory
    if ($LASTEXITCODE -ne 0) { throw 'Valid backup was rejected.' }
    if ((Get-Content -LiteralPath (Join-Path $dataDirectory 'logo.png') -Raw) -ne 'after') { throw 'Media was not restored.' }
    if (@(Get-ChildItem -LiteralPath (Join-Path $dataDirectory 'Backups') -Filter 'RestaurantPos-before-restore-*.zip').Count -ne 1) { throw 'Safety backup is missing.' }

    Add-Type -AssemblyName System.IO.Compression
    $badZip = Join-Path $testRoot 'bad.zip'
    $archive = [System.IO.Compression.ZipFile]::Open($badZip, [System.IO.Compression.ZipArchiveMode]::Create)
    try {
        $entry = $archive.CreateEntry('../escape.png')
        $writer = New-Object System.IO.StreamWriter($entry.Open())
        try { $writer.Write('unsafe') } finally { $writer.Dispose() }
    } finally { $archive.Dispose() }
    & dotnet $apiDll --restore-backup $badZip $dataDirectory
    if ($LASTEXITCODE -eq 0) { throw 'Unsafe backup was accepted.' }
    if ((Get-Content -LiteralPath (Join-Path $dataDirectory 'logo.png') -Raw) -ne 'after') { throw 'Failed restore changed live data.' }

    $corruptSource = Join-Path $testRoot 'corrupt-source'
    New-Item -ItemType Directory -Path $corruptSource | Out-Null
    Set-Content -LiteralPath (Join-Path $corruptSource 'restaurantpos.db') -Value 'not sqlite'
    $corruptZip = Join-Path $testRoot 'corrupt.zip'
    Compress-Archive -Path (Join-Path $corruptSource '*') -DestinationPath $corruptZip
    & dotnet $apiDll --restore-backup $corruptZip $dataDirectory
    if ($LASTEXITCODE -eq 0) { throw 'Corrupt database was accepted.' }
    if ((Get-Content -LiteralPath (Join-Path $dataDirectory 'logo.png') -Raw) -ne 'after') { throw 'Corrupt restore changed live data.' }
    Write-Host 'Recovery tests passed: valid restore, safety backup, media restore, unsafe ZIP and corrupt database rejection.'
} finally {
    if ($server -and -not $server.HasExited) { Stop-Process -Id $server.Id -ErrorAction SilentlyContinue }
    $env:RESTAURANTPOS_CONFIG_PATH = $previousConfig
    $env:ASPNETCORE_ENVIRONMENT = $previousEnvironment
    $env:RESTAURANTPOS_BOOTSTRAP_PASSWORD = $previousBootstrap
    $resolved = [System.IO.Path]::GetFullPath($testRoot)
    $tempRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
    if ($resolved.StartsWith($tempRoot, [StringComparison]::OrdinalIgnoreCase) -and
        [System.IO.Path]::GetFileName($resolved).StartsWith('pos-recovery-test-', [StringComparison]::Ordinal)) {
        Remove-Item -LiteralPath $resolved -Recurse -Force -ErrorAction SilentlyContinue
    }
}
