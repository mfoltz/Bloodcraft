param(
    [string]$BaseRef,

    [int]$LineThreshold = 120,

    [switch]$WarnOnly
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$ScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = Split-Path -Parent (Split-Path -Parent $ScriptRoot)
$ChangelogPath = Join-Path $RepoRoot "CHANGELOG.md"

function Invoke-Git {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    Push-Location $RepoRoot
    try {
        & git -c core.autocrlf=false @Arguments
    }
    finally {
        Pop-Location
    }
}

function Write-Nudge {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Message
    )

    if ($env:GITHUB_ACTIONS -eq "true") {
        Write-Host "::warning title=Release hygiene nudge::$Message"
    }
    else {
        Write-Warning $Message
    }

    $script:NudgeCount++
}

function Get-UnreleasedBody {
    $Text = Get-Content -Raw -Path $ChangelogPath
    $Match = [regex]::Match($Text, '(?ms)^## Unreleased\s*(?<body>.*?)(?=^`[^`]+`\s*$|^## |\z)')

    if (-not $Match.Success) {
        return $null
    }

    return $Match.Groups["body"].Value.Trim()
}

function Test-MeaningfulPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $NormalizedPath = $Path -replace '\\', '/'

    return $NormalizedPath -match '^(BloodcraftEclipseBridge|Commands|Factory|Interfaces|Patches|Services|Systems|Utilities)/' `
        -or $NormalizedPath -match '^Resources/(Localization/)?[^/]+\.(cs|json)$' `
        -or $NormalizedPath -match '^\.codex/scripts/' `
        -or $NormalizedPath -match '^\.github/workflows/' `
        -or $NormalizedPath -match '^[^/]+\.(cs|csproj)$'
}

if ([string]::IsNullOrWhiteSpace($BaseRef)) {
    $BaseRef = if (-not [string]::IsNullOrWhiteSpace($env:GITHUB_BASE_REF)) {
        "origin/$($env:GITHUB_BASE_REF)"
    }
    else {
        "origin/main"
    }
}

$MergeBase = Invoke-Git -Arguments @("merge-base", "HEAD", $BaseRef) 2>$null
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($MergeBase)) {
    Write-Host "release-nudge: unable to resolve merge-base for '$BaseRef'; skipping release hygiene gate."
    exit 0
}

$ChangedFiles = @(Invoke-Git -Arguments @("diff", "--name-only", $MergeBase) 2>$null)
$MeaningfulFiles = @($ChangedFiles | Where-Object { Test-MeaningfulPath $_ })

if ($MeaningfulFiles.Count -eq 0) {
    Write-Host "release-nudge: no meaningful source, script, or workflow changes detected."
    exit 0
}

$NumstatArguments = @("diff", "--numstat", $MergeBase, "--") + $MeaningfulFiles
$Numstat = @(Invoke-Git -Arguments $NumstatArguments 2>$null)
$ChangedLines = 0
foreach ($Line in $Numstat) {
    $Parts = $Line -split "`t"
    if ($Parts.Length -lt 2) {
        continue
    }

    $AddedLines = 0
    if ([int]::TryParse($Parts[0], [ref]$AddedLines)) {
        $ChangedLines += $AddedLines
    }

    $DeletedLines = 0
    if ([int]::TryParse($Parts[1], [ref]$DeletedLines)) {
        $ChangedLines += $DeletedLines
    }
}

$InterfaceTouched = @($MeaningfulFiles | Where-Object { ($_ -replace '\\', '/') -match '^(BloodcraftEclipseBridge|Commands|Interfaces|Services|Systems)/' }).Count -gt 0
$ChangelogChanged = @($ChangedFiles | Where-Object { $_ -eq "CHANGELOG.md" }).Count -gt 0
$UnreleasedBody = Get-UnreleasedBody
$HasUnreleasedNotes = -not [string]::IsNullOrWhiteSpace($UnreleasedBody)
$script:NudgeCount = 0

if (($InterfaceTouched -or $ChangedLines -ge $LineThreshold) -and -not $ChangelogChanged -and -not $HasUnreleasedNotes) {
    Write-Nudge "Meaningful Bloodcraft changes detected ($ChangedLines changed lines across $($MeaningfulFiles.Count) files). Consider adding CHANGELOG.md notes before release."
}

if ($HasUnreleasedNotes) {
    Write-Nudge "Bloodcraft CHANGELOG.md has Unreleased notes. Before a release-bound merge, consider running .codex/scripts/bump-version.ps1 so version metadata and changelog stay aligned."
}

if ($script:NudgeCount -eq 0) {
    Write-Host "release-nudge: no changelog or version-bump nudge needed."
}
elseif (-not $WarnOnly.IsPresent) {
    exit 1
}
