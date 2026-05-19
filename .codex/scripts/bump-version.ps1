param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$Version,

    [switch]$AllowEmptyChangelog
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$ScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = Split-Path -Parent (Split-Path -Parent $ScriptRoot)

$ProjectPath = Join-Path $RepoRoot "Bloodcraft.csproj"
$ThunderstorePath = Join-Path $RepoRoot "thunderstore.toml"
$ChangelogPath = Join-Path $RepoRoot "CHANGELOG.md"

function Set-TextPreservingUtf8Bom {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [string]$Value
    )

    $Bytes = [System.IO.File]::ReadAllBytes($Path)
    $HasUtf8Bom = $Bytes.Length -ge 3 -and $Bytes[0] -eq 0xEF -and $Bytes[1] -eq 0xBB -and $Bytes[2] -eq 0xBF
    $Encoding = [System.Text.UTF8Encoding]::new($HasUtf8Bom)
    [System.IO.File]::WriteAllText($Path, $Value, $Encoding)
}

function Update-FirstMatch {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [string]$Pattern,
        [Parameter(Mandatory = $true)]
        [string]$Replacement,
        [Parameter(Mandatory = $true)]
        [string]$Description
    )

    $Text = Get-Content -Raw -Path $Path
    $Regex = [regex]::new($Pattern, [System.Text.RegularExpressions.RegexOptions]::Multiline)
    $Match = $Regex.Match($Text)
    if (-not $Match.Success) {
        throw "Unable to update $Description in $Path."
    }

    $Updated = $Text.Substring(0, $Match.Index) + $Regex.Replace($Match.Value, $Replacement, 1) + $Text.Substring($Match.Index + $Match.Length)
    Set-TextPreservingUtf8Bom -Path $Path -Value $Updated
}

function Update-Changelog {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [string]$Version,
        [Parameter(Mandatory = $true)]
        [bool]$AllowEmpty
    )

    $Text = Get-Content -Raw -Path $Path

    $VersionHeadingPattern = '(?m)^## v' + [regex]::Escape($Version) + '$'
    if ($Text -match $VersionHeadingPattern) {
        throw "CHANGELOG.md already contains a v$Version entry."
    }

    $UnreleasedPattern = '(?ms)^## Unreleased\s*(?<body>.*?)(?=^## |\z)'
    $Match = [regex]::Match($Text, $UnreleasedPattern)
    if (-not $Match.Success) {
        throw "CHANGELOG.md must contain an '## Unreleased' section before bumping."
    }

    $Body = $Match.Groups["body"].Value.Trim()
    if (-not $AllowEmpty -and [string]::IsNullOrWhiteSpace($Body)) {
        throw "CHANGELOG.md '## Unreleased' is empty. Use -AllowEmptyChangelog to bump anyway."
    }

    $ReleasedBody = if ([string]::IsNullOrWhiteSpace($Body)) { "- No user-facing changes recorded." } else { $Body }
    $Replacement = "## Unreleased`r`n`r`n## v$Version`r`n`r`n$ReleasedBody`r`n`r`n"
    $Updated = $Text.Substring(0, $Match.Index) + $Replacement + $Text.Substring($Match.Index + $Match.Length)
    Set-TextPreservingUtf8Bom -Path $Path -Value $Updated
}

Update-FirstMatch `
    -Path $ProjectPath `
    -Pattern '<Version>[^<]+</Version>' `
    -Replacement "<Version>$Version</Version>" `
    -Description "project version"

Update-FirstMatch `
    -Path $ThunderstorePath `
    -Pattern '^versionNumber = "[^"]+"' `
    -Replacement "versionNumber = `"$Version`"" `
    -Description "Thunderstore version"

Update-Changelog -Path $ChangelogPath -Version $Version -AllowEmpty:$AllowEmptyChangelog.IsPresent

Write-Host "Updated Bloodcraft version metadata to $Version."
