param([switch]$SkipFirewall, [switch]$NoBrowser)

$ErrorActionPreference = 'Stop'
$hostExe = Join-Path $PSScriptRoot 'RestaurantPos.Api.exe'
$dataDirectory = Join-Path $env:LOCALAPPDATA 'RestaurantPos'
$configPath = Join-Path $dataDirectory 'local-host.json'
$readyMarker = Join-Path $dataDirectory 'initialized.txt'
$pidPath = Join-Path $dataDirectory 'server.pid'
$localUrl = 'http://localhost:5268/'
$server = $null

function Get-RestaurantNetworks {
    foreach ($profile in @(Get-NetConnectionProfile -ErrorAction SilentlyContinue)) {
        $addresses = @(Get-NetIPAddress -InterfaceIndex $profile.InterfaceIndex -AddressFamily IPv4 -ErrorAction SilentlyContinue |
            Where-Object { $_.IPAddress -match '^(10\.|192\.168\.|172\.(1[6-9]|2[0-9]|3[01])\.)' } |
            Select-Object -ExpandProperty IPAddress)
        if ($addresses.Count -gt 0) {
            [pscustomobject]@{ Profile = $profile; Addresses = $addresses }
        }
    }
}

function Test-RestaurantFirewallRule {
    $rule = Get-NetFirewallRule -DisplayName 'Restaurant POS Local Host (Private Wi-Fi)' -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $rule -or $rule.Enabled -ne 'True' -or $rule.Direction -ne 'Inbound' -or $rule.Action -ne 'Allow' -or $rule.Profile.ToString() -notmatch 'Private') {
        return $false
    }
    $application = Get-NetFirewallApplicationFilter -AssociatedNetFirewallRule $rule -ErrorAction SilentlyContinue
    $port = Get-NetFirewallPortFilter -AssociatedNetFirewallRule $rule -ErrorAction SilentlyContinue
    return ($application.Program -eq $hostExe -and $port.LocalPort -eq '5268' -and $port.Protocol -eq 'TCP')
}

try {
    if (-not (Test-Path -LiteralPath $hostExe -PathType Leaf)) { throw 'RestaurantPos.Api.exe is missing. Copy the entire release folder.' }
    New-Item -ItemType Directory -Path $dataDirectory -Force | Out-Null

    if (-not (Test-Path -LiteralPath $configPath)) {
        $secretBytes = New-Object byte[] 48
        $random = [Security.Cryptography.RandomNumberGenerator]::Create()
        try { $random.GetBytes($secretBytes) } finally { $random.Dispose() }
        $secret = [Convert]::ToBase64String($secretBytes)
        $dbPath = Join-Path $dataDirectory 'restaurantpos.db'
        $config = @{
            ConnectionStrings = @{ DefaultConnection = "Data Source=$dbPath" }
            Jwt = @{ Key = $secret; Issuer = 'RestaurantPos'; Audience = 'RestaurantPos'; ExpiryMinutes = 480 }
            LocalHost = @{ Port = 5268; DataDirectory = $dataDirectory }
        }
        $config | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $configPath -Encoding UTF8
    }

    if (Get-NetTCPConnection -LocalPort 5268 -State Listen -ErrorAction SilentlyContinue) {
        throw 'Port 5268 is already in use. Close the other POS window or application first.'
    }

    $password = $null
    if (-not (Test-Path -LiteralPath $readyMarker)) {
        Write-Host 'First setup: choose an admin password (at least 10 characters).'
        do {
            $securePassword = Read-Host 'Admin password' -AsSecureString
            $pointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
            try { $password = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer) }
            finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer) }
            if ($password.Length -lt 10) { Write-Host 'Please use at least 10 characters.' }
        } while ($password.Length -lt 10)
        Write-Host 'Username: admin. Remember this password; it is not saved in the launcher.'
    }

    $networks = @(Get-RestaurantNetworks)
    $publicNetworks = @($networks | Where-Object { $_.Profile.NetworkCategory -eq 'Public' })
    $approvedIndexes = @()
    if (-not $SkipFirewall) {
        foreach ($network in $publicNetworks) {
            Write-Host "Wi-Fi/network '$($network.Profile.Name)' is marked Public by Windows."
            $answer = Read-Host 'Is this your trusted restaurant network? Type YES to let staff devices connect'
            if ($answer -eq 'YES') { $approvedIndexes += $network.Profile.InterfaceIndex }
        }
        $hasPrivateNetwork = @($networks | Where-Object { $_.Profile.NetworkCategory -eq 'Private' }).Count -gt 0
        if ($approvedIndexes.Count -gt 0 -or ($hasPrivateNetwork -and -not (Test-RestaurantFirewallRule))) {
            Write-Host 'Windows will ask once for administrator approval to configure local access.'
            try {
                $helper = Join-Path $PSScriptRoot 'Allow-LocalHostFirewall.ps1'
                $indexes = $approvedIndexes -join ','
                $arguments = '-NoProfile -ExecutionPolicy Bypass -File "' + $helper + '" -ProgramPath "' + $hostExe + '" -InterfaceIndexesCsv "' + $indexes + '"'
                $firewall = Start-Process powershell.exe -Verb RunAs -ArgumentList $arguments -Wait -PassThru -WindowStyle Hidden
                if ($firewall.ExitCode -ne 0) { Write-Warning 'Windows network setup was not completed.' }
            } catch {
                Write-Warning 'Windows network setup was not approved.'
            }
        }
    }
    $networks = @(Get-RestaurantNetworks)
    $privateAddresses = @($networks | Where-Object { $_.Profile.NetworkCategory -eq 'Private' } |
        ForEach-Object { $_.Addresses } | Select-Object -Unique)
    $firewallReady = Test-RestaurantFirewallRule
    $lanWarning = ''
    if ($privateAddresses.Count -eq 0) {
        $lanWarning = 'No trusted Private network is ready. Connect to restaurant Wi-Fi, approve it as trusted when starting the POS, then restart.'
    } elseif (-not $firewallReady -and -not $SkipFirewall) {
        $lanWarning = 'Windows Firewall has not allowed the POS yet. Restart the POS and approve the Windows administrator prompt.'
    }
    if ($lanWarning) { Write-Warning $lanWarning }

    $env:ASPNETCORE_ENVIRONMENT = 'LocalHost'
    $env:ASPNETCORE_URLS = (@('http://localhost:5268') + @($privateAddresses | ForEach-Object { "http://${_}:5268" })) -join ';'
    $env:RESTAURANTPOS_LAN_ADDRESSES = $privateAddresses -join ';'
    $env:RESTAURANTPOS_NETWORK_NAMES = @($networks | Where-Object { $_.Profile.NetworkCategory -eq 'Private' } | ForEach-Object { $_.Profile.Name }) -join ';'
    $env:RESTAURANTPOS_LAN_READY = [string]($privateAddresses.Count -gt 0 -and ($firewallReady -or $SkipFirewall))
    $env:RESTAURANTPOS_LAN_WARNING = $lanWarning
    $env:RESTAURANTPOS_CONFIG_PATH = $configPath
    if ($password) { $env:RESTAURANTPOS_BOOTSTRAP_PASSWORD = $password }
    $stdout = Join-Path $dataDirectory 'server-output.log'
    $stderr = Join-Path $dataDirectory 'server-error.log'
    $server = Start-Process -FilePath $hostExe -WorkingDirectory $PSScriptRoot -PassThru -WindowStyle Hidden -RedirectStandardOutput $stdout -RedirectStandardError $stderr
    Set-Content -LiteralPath $pidPath -Value $server.Id
    Remove-Item Env:\RESTAURANTPOS_BOOTSTRAP_PASSWORD -ErrorAction SilentlyContinue
    $password = $null

    $ready = $false
    for ($attempt = 0; $attempt -lt 30; $attempt++) {
        Start-Sleep -Milliseconds 500
        $server.Refresh()
        if ($server.HasExited) { break }
        try {
            $response = Invoke-WebRequest -Uri ($localUrl + 'health') -UseBasicParsing -TimeoutSec 8
            if ($response.StatusCode -eq 200) { $ready = $true; break }
        } catch { }
    }
    if (-not $ready) {
        $details = if (Test-Path -LiteralPath $stderr) { (Get-Content -LiteralPath $stderr -Tail 20) -join "`n" } else { 'No server log available.' }
        throw "The POS server did not start. $details"
    }

    if (-not (Test-Path -LiteralPath $readyMarker)) { Set-Content -LiteralPath $readyMarker -Value 'Admin account initialized.' }
    Write-Host ''
    Write-Host "Restaurant POS is running: $localUrl"
    foreach ($address in $privateAddresses) { Write-Host "Staff URL: http://${address}:5268/" }
    Write-Host 'Sign in as admin, then open Connect devices to show phone/tablet QR codes.'
    Write-Host 'Keep this window and the laptop open while staff are using the POS.'
    Write-Host "Data folder: $dataDirectory"
    Write-Host 'To stop hosting, double-click Stop Restaurant POS.cmd.'
    if (-not $NoBrowser) { Start-Process ($localUrl + 'host') }
    try { Wait-Process -Id $server.Id } finally { Remove-Item -LiteralPath $pidPath -ErrorAction SilentlyContinue }
} catch {
    if ($server) {
        $server.Refresh()
        if (-not $server.HasExited) { Stop-Process -Id $server.Id -ErrorAction SilentlyContinue }
        Remove-Item -LiteralPath $pidPath -ErrorAction SilentlyContinue
    }
    Write-Host ''
    Write-Error $_
    exit 1
}
