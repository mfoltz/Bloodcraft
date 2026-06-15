Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$CodexRoot = Split-Path -Path $PSScriptRoot -Parent
$CacheRoot = Join-Path $CodexRoot "tmp\scarlet-rcon"
$DownloadRoot = Join-Path $CacheRoot "downloads"
$ExtractRoot = Join-Path $CacheRoot "extracted"
$PluginRoot = Join-Path $CacheRoot "plugins"

$Packages = @(
    [ordered]@{
        Name = "ScarletCore"
        Version = "1.3.11"
        Url = "https://thunderstore.io/package/download/ScarletMods/ScarletCore/1.3.11/"
        Sha256 = "1A879411B8258F9C28B8D6804720563E825FA1788ECA87F7B89BAB081CA6569F"
        Dll = "ScarletCore.dll"
    },
    [ordered]@{
        Name = "ScarletRCON"
        Version = "1.2.9"
        Url = "https://thunderstore.io/package/download/ScarletMods/ScarletRCON/1.2.9/"
        Sha256 = "05F481108569830428B3BD1E57332280B445F30885D8F048D7735355160504BF"
        Dll = "ScarletRCON.dll"
    }
)

function Test-ZipHash {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [string]$ExpectedHash
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        return $false
    }

    $ActualHash = (Get-FileHash -Path $Path -Algorithm SHA256).Hash
    return [string]::Equals($ActualHash, $ExpectedHash, [System.StringComparison]::OrdinalIgnoreCase)
}

New-Item -ItemType Directory -Path $DownloadRoot -Force | Out-Null
New-Item -ItemType Directory -Path $ExtractRoot -Force | Out-Null
New-Item -ItemType Directory -Path $PluginRoot -Force | Out-Null

foreach ($Package in $Packages) {
    $ZipPath = Join-Path $DownloadRoot "$($Package.Name)-$($Package.Version).zip"
    if (-not (Test-ZipHash -Path $ZipPath -ExpectedHash $Package.Sha256)) {
        Write-Host "[scarlet-rcon] Downloading $($Package.Name) $($Package.Version)"
        Invoke-WebRequest -Uri $Package.Url -OutFile $ZipPath
    }

    if (-not (Test-ZipHash -Path $ZipPath -ExpectedHash $Package.Sha256)) {
        $ActualHash = if (Test-Path -LiteralPath $ZipPath) { (Get-FileHash -Path $ZipPath -Algorithm SHA256).Hash } else { "<missing>" }
        throw "Downloaded $($Package.Name) $($Package.Version) hash mismatch. Expected $($Package.Sha256), got $ActualHash."
    }

    $PackageExtractRoot = Join-Path $ExtractRoot "$($Package.Name)-$($Package.Version)"
    $ExtractedDllPath = Join-Path $PackageExtractRoot $Package.Dll
    if (-not (Test-Path -LiteralPath $ExtractedDllPath)) {
        if (Test-Path -LiteralPath $PackageExtractRoot) {
            Remove-Item -LiteralPath $PackageExtractRoot -Recurse -Force
        }

        New-Item -ItemType Directory -Path $PackageExtractRoot -Force | Out-Null
        Expand-Archive -LiteralPath $ZipPath -DestinationPath $PackageExtractRoot -Force
    }

    if (-not (Test-Path -LiteralPath $ExtractedDllPath)) {
        throw "$($Package.Dll) was not found in $ZipPath."
    }

    Copy-Item -LiteralPath $ExtractedDllPath -Destination (Join-Path $PluginRoot $Package.Dll) -Force
}

Write-Host "[scarlet-rcon] Prepared ScarletRCON harness dependencies in $PluginRoot"
