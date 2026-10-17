param(
    [Parameter(Mandatory=$true)][string]$ProgramPath,
    [string]$InterfaceIndexesCsv = ''
)

$ErrorActionPreference = 'Stop'
if (-not (Test-Path -LiteralPath $ProgramPath -PathType Leaf)) { throw 'POS executable not found.' }
foreach ($interfaceIndex in ($InterfaceIndexesCsv -split ',' | Where-Object { $_ })) {
    $index = [int]$interfaceIndex
    $profile = Get-NetConnectionProfile -InterfaceIndex $index -ErrorAction Stop
    if ($profile.NetworkCategory -eq 'Public') {
        Set-NetConnectionProfile -InterfaceIndex $index -NetworkCategory Private -ErrorAction Stop
    }
}
$ruleName = 'Restaurant POS Local Host (Private Wi-Fi)'
$existing = Get-NetFirewallRule -DisplayName $ruleName -ErrorAction SilentlyContinue
if ($existing) { Remove-NetFirewallRule -DisplayName $ruleName }
New-NetFirewallRule -DisplayName $ruleName -Direction Inbound -Action Allow -Protocol TCP -LocalPort 5268 -Profile Private -Program $ProgramPath | Out-Null
