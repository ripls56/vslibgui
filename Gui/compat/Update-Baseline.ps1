<#
.SYNOPSIS
    Refreshes the API-compat baseline from a published release zip.

.DESCRIPTION
    Extracts Gui.dll and reads the modinfo.json version from a Releases zip,
    writing them into compat/baseline/ so the next build compares against the
    newly released version.

    Run this after every ModDB publish:
        .\compat\Update-Baseline.ps1 ..\Releases\gui_1.2.3.zip

.PARAMETER ZipPath
    Path to the release zip produced by the build pipeline (or downloaded from ModDB).
#>
param(
    [Parameter(Mandatory)]
    [string] $ZipPath
)

$ErrorActionPreference = 'Stop'

$zipPath = Resolve-Path $ZipPath
$scriptDir = $PSScriptRoot
$baselineDir = Join-Path $scriptDir 'baseline'

if (-not (Test-Path $baselineDir))
{
    New-Item -ItemType Directory -Path $baselineDir | Out-Null
}

$tempDir = Join-Path ([System.IO.Path]::GetTempPath()) ([System.Guid]::NewGuid().ToString())
New-Item -ItemType Directory -Path $tempDir | Out-Null

try
{
    Expand-Archive -Path $zipPath -DestinationPath $tempDir -Force

    $sourceDll = Join-Path $tempDir 'Gui.dll'
    if (-not (Test-Path $sourceDll))
    {
        throw "Gui.dll not found in zip. Entries: $(Get-ChildItem $tempDir -Recurse | Select-Object -ExpandProperty Name)"
    }

    $modinfoPath = Join-Path $tempDir 'modinfo.json'
    if (-not (Test-Path $modinfoPath))
    {
        throw "modinfo.json not found in zip."
    }

    $modinfo = Get-Content $modinfoPath -Raw | ConvertFrom-Json
    $version = $modinfo.version

    Copy-Item -Path $sourceDll -Destination (Join-Path $baselineDir 'Gui.dll') -Force
    Set-Content -Path (Join-Path $baselineDir 'version.txt') -Value $version -NoNewline

    Write-Host "Baseline updated to v$version from $(Split-Path $zipPath -Leaf)"
}
finally
{
    Remove-Item -Recurse -Force $tempDir
}
