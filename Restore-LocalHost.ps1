param([string]$ZipPath = '')

$ErrorActionPreference = 'Stop'
$dataDirectory = Join-Path $env:LOCALAPPDATA 'RestaurantPos'
$serverExe = Join-Path $PSScriptRoot 'RestaurantPos.Api.exe'
if (-not (Test-Path -LiteralPath $serverExe)) { throw 'RestaurantPos.Api.exe is missing from this folder.' }
if (-not (Test-Path -LiteralPath $dataDirectory -PathType Container)) { throw 'No Restaurant POS data folder was found on this laptop.' }

if ([string]::IsNullOrWhiteSpace($ZipPath)) {
    Add-Type -AssemblyName System.Windows.Forms
    $picker = New-Object System.Windows.Forms.OpenFileDialog
    $picker.Title = 'Choose a Restaurant POS backup ZIP'
    $picker.Filter = 'ZIP backups (*.zip)|*.zip'
    $picker.InitialDirectory = Join-Path $dataDirectory 'Backups'
    if ($picker.ShowDialog() -ne [System.Windows.Forms.DialogResult]::OK) { exit 0 }
    $ZipPath = $picker.FileName
}
$ZipPath = [System.IO.Path]::GetFullPath($ZipPath)
if (-not (Test-Path -LiteralPath $ZipPath -PathType Leaf)) { throw 'The backup ZIP was not found.' }

Write-Host ''
Write-Host "Selected backup: $ZipPath"
Write-Host 'All current orders, users, menu, settings, and images will be replaced by this backup.'
Write-Host 'A safety copy of current data will be saved in the Backups folder first.'
Write-Host 'IMPORTANT: First double-click Stop Restaurant POS.cmd and close any running POS server.'
$confirmation = Read-Host 'Type RESTORE to continue'
if ($confirmation -cne 'RESTORE') { Write-Host 'Restore cancelled. No data changed.'; exit 0 }

& $serverExe --restore-backup $ZipPath $dataDirectory
if ($LASTEXITCODE -ne 0) { throw 'Restore did not complete. Read the error above; if rollback also failed, do not restart and seek technical help.' }
Write-Host 'Now double-click Start Restaurant POS.cmd and sign in with the account from the backup.'
