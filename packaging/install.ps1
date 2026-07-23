#Requires -Version 5.1
<#
.SYNOPSIS
    Installs the SendIt CLI for Windows.
.DESCRIPTION
    Downloads the latest self-contained sendit.exe from GitHub Releases and installs it
    to %LOCALAPPDATA%\Programs\SendIt, adding that directory to the current user's PATH.
    Intended to be run via:
        irm https://raw.githubusercontent.com/matthewratcliffe/sendit/main/packaging/install.ps1 | iex
#>

$ErrorActionPreference = 'Stop'

$Repo = 'matthewratcliffe/sendit'
$InstallDir = Join-Path $env:LOCALAPPDATA 'Programs\SendIt'

$arch = if ([System.Environment]::Is64BitOperatingSystem) {
    if ($env:PROCESSOR_ARCHITECTURE -eq 'ARM64') { 'win-arm64' } else { 'win-x64' }
} else {
    'win-x64'
}

$existingExe = Join-Path $InstallDir 'sendit.exe'
$isUpdate = Test-Path $existingExe
if ($isUpdate) {
    $currentVersion = (& $existingExe --version) -replace '^sendit\s+', ''
    Write-Host "SendIt $currentVersion is already installed. Checking for updates..." -ForegroundColor Cyan
} else {
    Write-Host "Fetching latest SendIt release ($arch)..." -ForegroundColor Cyan
}

$release = Invoke-RestMethod -Uri "https://api.github.com/repos/$Repo/releases/latest"
$asset = $release.assets | Where-Object { $_.name -like "sendit-$arch.zip" } | Select-Object -First 1

if (-not $asset) {
    throw "Could not find a release asset matching 'sendit-$arch.zip' for $Repo. See https://github.com/$Repo/releases"
}

if ($isUpdate -and $release.tag_name -eq "v$currentVersion") {
    Write-Host "SendIt $currentVersion is already up to date." -ForegroundColor Green
    exit 0
}

New-Item -ItemType Directory -Force -Path $InstallDir | Out-Null
$zipPath = Join-Path $env:TEMP "sendit-$arch.zip"

Write-Host "Downloading $($asset.browser_download_url)..." -ForegroundColor Cyan
Invoke-WebRequest -Uri $asset.browser_download_url -OutFile $zipPath

Write-Host "$(if ($isUpdate) { 'Updating' } else { 'Installing' }) $InstallDir..." -ForegroundColor Cyan
Expand-Archive -Path $zipPath -DestinationPath $InstallDir -Force
Remove-Item $zipPath -Force

$userPath = [Environment]::GetEnvironmentVariable('Path', 'User')
if ($userPath -notlike "*$InstallDir*") {
    [Environment]::SetEnvironmentVariable('Path', "$userPath;$InstallDir", 'User')
    $env:Path += ";$InstallDir"
    Write-Host "Added $InstallDir to your user PATH. Restart your terminal to pick it up." -ForegroundColor Yellow
}

Write-Host "SendIt installed. Run 'sendit --version' to verify (in a new terminal if PATH was just updated)." -ForegroundColor Green
