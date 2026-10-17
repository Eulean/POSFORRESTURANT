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

    & (Join-Path $PSScriptRoot 'Build-LocalHost.ps1')
    if (-not (Test-Path -LiteralPath $zipPath -PathType Leaf)) { throw 'The build did not create a ZIP.' }
    $zip = Get-Item -LiteralPath $zipPath
    $hash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()

    $credentialLines = "protocol=https`nhost=github.com`n`n" | git credential fill
    $passwordLine = $credentialLines | Where-Object { $_ -like 'password=*' } | Select-Object -First 1
    if (-not $passwordLine) { throw 'Sign in to GitHub through Git Credential Manager first.' }
    $token = $passwordLine.Substring(9)
    $curlConfig = "header = `"Authorization: Bearer $token`"`nheader = `"Accept: application/vnd.github+json`"`nheader = `"X-GitHub-Api-Version: 2022-11-28`"`n"

    $notes = "Windows x64 offline LAN host. Extract the ZIP, then run Start Restaurant POS.cmd. Use only on a trusted Private Wi-Fi; never forward port 5268 to the internet.`n`nSHA-256: $hash"
    $body = @{
        tag_name = $Tag
        target_commitish = $commit
        name = "Restaurant POS Local Host $Tag"
        body = $notes
        draft = $true
        prerelease = $false
    } | ConvertTo-Json -Compress
    $releaseJson = $curlConfig | curl.exe -sS -f --max-time 30 --config - -H 'Content-Type: application/json' --data-raw $body "https://api.github.com/repos/$repo/releases"
    if ($LASTEXITCODE -ne 0) { throw 'GitHub could not create the draft release.' }
    $release = $releaseJson | ConvertFrom-Json
    if (-not $release.id) { throw 'GitHub returned an invalid release response.' }

    $uploadUrl = "https://uploads.github.com/repos/$repo/releases/$($release.id)/assets?name=RestaurantPos-LocalHost.zip"
    $assetJson = $curlConfig | curl.exe -sS -f --max-time 300 --config - -H 'Content-Type: application/zip' --data-binary "@$zipPath" $uploadUrl
    if ($LASTEXITCODE -ne 0) { throw "ZIP upload failed; draft release $($release.id) remains on GitHub." }
    $asset = $assetJson | ConvertFrom-Json
    if ($asset.size -ne $zip.Length) { throw "Uploaded ZIP size mismatch; draft release $($release.id) remains on GitHub." }

    $publishJson = $curlConfig | curl.exe -sS -f --max-time 30 --config - -X PATCH -H 'Content-Type: application/json' --data-raw '{"draft":false}' "https://api.github.com/repos/$repo/releases/$($release.id)"
    if ($LASTEXITCODE -ne 0) { throw "The ZIP uploaded, but draft release $($release.id) could not be published." }
    $published = $publishJson | ConvertFrom-Json
    Write-Host "Release: $($published.html_url)"
    Write-Host "ZIP: $($asset.browser_download_url)"
    Write-Host "SHA-256: $hash"
} finally {
    $token = $null
    $curlConfig = $null
    Pop-Location
}
