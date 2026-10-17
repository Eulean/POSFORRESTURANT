param([Parameter(Mandatory=$true)][string]$ProgramPath)

$ErrorActionPreference = 'Stop'
if (-not (Test-Path -LiteralPath $ProgramPath -PathType Leaf)) { throw 'POS executable not found.' }
$ruleName = 'Restaurant POS Local Host (Private Wi-Fi)'
$existing = Get-NetFirewallRule -DisplayName $ruleName -ErrorAction SilentlyContinue
if ($existing) { Remove-NetFirewallRule -DisplayName $ruleName }
New-NetFirewallRule -DisplayName $ruleName -Direction Inbound -Action Allow -Protocol TCP -LocalPort 5268 -Profile Private -Program $ProgramPath | Out-Null
