param([string]$Tag)

$ErrorActionPreference = 'Stop'
$repo = 'Eulean/POSFORRESTURANT'
$zipPath = Join-Path $PSScriptRoot 'out\RestaurantPos-LocalHost.zip'

Push-Location $PSScriptRoot
try {
    if ((git branch --show-current).Trim() -ne 'main') { throw 'Switch to main before publishing a release.' }
    if (git status --porcelain --untracked-files=no) { throw 'Commit tracked changes before publishing a release.' }
    $commit = (git rev-parse HEAD).Trim()
    $remoteLine = git ls-remote origin refs/heads/main
    if ($LASTEXITCODE -ne 0 -or -not $remoteLine) { throw 'Could not check origin/main.' }
    $remoteCommit = ($remoteLine -split '\s+')[0]
    if ($commit -ne $remoteCommit) { throw 'Push main to origin before publishing a release.' }

    if (-not $Tag) { $Tag = "local-host-$(Get-Date -Format 'yyyyMMdd-HHmmss')-$($commit.Substring(0, 7))" }
    if ($Tag -notmatch '^[A-Za-z0-9._-]+$') { throw 'Release tag may contain only letters, numbers, periods, underscores, and hyphens.' }
    if (-not (Get-Command gh -ErrorAction SilentlyContinue)) { throw 'Install and sign in to GitHub CLI (gh) before publishing.' }
    gh auth status 2>$null | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Sign in to GitHub CLI with gh auth login before publishing.' }

    & (Join-Path $PSScriptRoot 'Build-LocalHost.ps1')
    if (-not (Test-Path -LiteralPath $zipPath -PathType Leaf)) { throw 'The build did not create a ZIP.' }
    $zip = Get-Item -LiteralPath $zipPath
    $hash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()

    $notes = "Windows x64 offline LAN host. Extract the ZIP, then run Start Restaurant POS.cmd. Use only on a trusted Private Wi-Fi; never forward port 5268 to the internet.`n`nSHA-256: $hash"
    gh release create $Tag "$zipPath#RestaurantPos-LocalHost.zip" --repo $repo --target main --title "Restaurant POS Local Host $Tag" --notes $notes --draft | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'GitHub could not create the draft release and upload the ZIP.' }

    $release = gh release view $Tag --repo $repo --json assets,url | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or $release.assets.Count -ne 1 -or $release.assets[0].size -ne $zip.Length) {
        throw "Uploaded ZIP size mismatch; draft release $Tag remains on GitHub."
    }

    gh release edit $Tag --repo $repo --draft=false | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "The ZIP uploaded, but draft release $Tag could not be published." }
    $published = gh release view $Tag --repo $repo --json url,assets | ConvertFrom-Json
    Write-Host "Release: $($published.url)"
    Write-Host "ZIP: $($published.assets[0].url)"
    Write-Host "SHA-256: $hash"
} finally {
    Pop-Location
}
