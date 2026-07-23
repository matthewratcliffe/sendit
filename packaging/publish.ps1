<#
.SYNOPSIS
    Builds self-contained single-file sendit binaries for all supported platforms and
    zips/tars them into ./artifacts, ready to attach to a GitHub Release.
#>
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root 'src\SendIt.Cli\SendIt.Cli.csproj'
$artifacts = Join-Path $root 'artifacts'

Remove-Item $artifacts -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $artifacts | Out-Null

$rids = @('win-x64', 'win-arm64', 'linux-x64', 'linux-arm64', 'osx-x64', 'osx-arm64')

foreach ($rid in $rids) {
    Write-Host "Publishing $rid..." -ForegroundColor Cyan
    $out = Join-Path $artifacts $rid
    dotnet publish $project -c Release -r $rid -o $out `
        -p:PublishSingleFile=true -p:SelfContained=true --self-contained true

    if ($rid.StartsWith('win-')) {
        Compress-Archive -Path (Join-Path $out 'sendit.exe') -DestinationPath (Join-Path $artifacts "sendit-$rid.zip") -Force
    } else {
        $tarPath = Join-Path $artifacts "sendit-$rid.tar.gz"
        tar -czf $tarPath -C $out sendit
    }
}

Write-Host "Artifacts written to $artifacts" -ForegroundColor Green
