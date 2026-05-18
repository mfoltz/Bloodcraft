Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$ScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$PrereleaseNotesPath = Join-Path $ScriptRoot "prerelease-notes.sh"
$BashPath = "C:\Program Files\Git\bin\bash.exe"

if (-not (Test-Path -LiteralPath $BashPath)) {
    throw "Git Bash was not found at $BashPath."
}

function Assert-Match {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Text,
        [Parameter(Mandatory = $true)]
        [string]$Pattern,
        [Parameter(Mandatory = $true)]
        [string]$Message
    )

    if ($Text -notmatch $Pattern) {
        throw $Message
    }
}

function New-Fixture {
    $FixtureRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("bloodcraft-prerelease-notes-" + [guid]::NewGuid().ToString("N"))
    New-Item -ItemType Directory -Path $FixtureRoot | Out-Null
    return $FixtureRoot
}

function Invoke-PrereleaseNotes {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    & $BashPath $PrereleaseNotesPath @Arguments 2>&1 | Out-String
}

function Test-PrereleaseNotesIncludesChangelogAndDetailsCard {
    $FixtureRoot = New-Fixture
    try {
        $ChangelogPath = Join-Path $FixtureRoot "CHANGELOG.md"
        $OutputPath = Join-Path $FixtureRoot "prerelease-notes.md"
        Set-Content -Path $ChangelogPath -Value @'
## Unreleased

`1.2.3`
- added staged Thunderstore packaging
- tightened prerelease receipts

`1.2.2`
- previous release
'@

        $Output = Invoke-PrereleaseNotes -Arguments @(
            "--changelog", $ChangelogPath,
            "--version", "1.2.3",
            "--tag", "v1.2.3-pre",
            "--branch", "main",
            "--commit", "1234567890abcdef",
            "--run-id", "42",
            "--output", $OutputPath)
        if ($LASTEXITCODE -ne 0) {
            throw "prerelease-notes.sh exited with $LASTEXITCODE`n$Output"
        }

        $Notes = Get-Content -Raw -Path $OutputPath
        if ($Notes -match '<details') {
            throw "Release notes should keep the Thunderstore handoff card visible without a dropdown."
        }

        Assert-Match -Text $Notes -Pattern '### 📦 Thunderstore handoff' -Message "Release notes did not include the handoff card heading."
        Assert-Match -Text $Notes -Pattern '📝 Changelog' -Message "Release notes did not include the changelog cue."
        Assert-Match -Text $Notes -Pattern '🌿 Branch' -Message "Release notes did not include the branch cue."
        Assert-Match -Text $Notes -Pattern '🔖 Commit' -Message "Release notes did not include the commit cue."
        Assert-Match -Text $Notes -Pattern '▶️ Run' -Message "Release notes did not include the workflow run cue."
        Assert-Match -Text $Notes -Pattern '🏷️ Tag' -Message "Release notes did not include the tag cue."
        Assert-Match -Text $Notes -Pattern '📦 Package' -Message "Release notes did not include the package cue."
        Assert-Match -Text $Notes -Pattern '## Unreleased.*empty' -Message "Release notes did not describe changelog turnover."
        Assert-Match -Text $Notes -Pattern 'Package.*1\.2\.3' -Message "Release notes did not include the Thunderstore package version."
        Assert-Match -Text $Notes -Pattern 'staged Thunderstore packaging' -Message "Release notes did not include current version changelog notes."
        Assert-Match -Text $Notes -Pattern '1234567890ab' -Message "Release notes did not include the short commit."
    }
    finally {
        Remove-Item -Recurse -Force -LiteralPath $FixtureRoot
    }
}

function Test-PrereleaseNotesRejectsUnreleasedContent {
    $FixtureRoot = New-Fixture
    try {
        $ChangelogPath = Join-Path $FixtureRoot "CHANGELOG.md"
        Set-Content -Path $ChangelogPath -Value @'
## Unreleased
- still parked for the next release

`1.2.3`
- added staged Thunderstore packaging
'@

        $Output = Invoke-PrereleaseNotes -Arguments @(
            "--changelog", $ChangelogPath,
            "--version", "1.2.3",
            "--check-only")
        if ($LASTEXITCODE -eq 0) {
            throw "prerelease-notes.sh unexpectedly accepted non-empty Unreleased notes."
        }

        Assert-Match -Text $Output -Pattern 'CHANGELOG\.md ## Unreleased must be empty' -Message "Unreleased rejection message was not specific."
    }
    finally {
        Remove-Item -Recurse -Force -LiteralPath $FixtureRoot
    }
}

function Test-PrereleaseNotesRejectsMissingUnreleasedHeader {
    $FixtureRoot = New-Fixture
    try {
        $ChangelogPath = Join-Path $FixtureRoot "CHANGELOG.md"
        Set-Content -Path $ChangelogPath -Value @"
`1.2.3`
- added staged Thunderstore packaging
"@

        $Output = Invoke-PrereleaseNotes -Arguments @(
            "--changelog", $ChangelogPath,
            "--version", "1.2.3",
            "--check-only")
        if ($LASTEXITCODE -eq 0) {
            throw "prerelease-notes.sh unexpectedly accepted a missing Unreleased header."
        }

        Assert-Match -Text $Output -Pattern 'must contain a ## Unreleased section' -Message "Missing Unreleased rejection message was not specific."
    }
    finally {
        Remove-Item -Recurse -Force -LiteralPath $FixtureRoot
    }
}

function Test-PrereleaseNotesRejectsMissingVersionEntry {
    $FixtureRoot = New-Fixture
    try {
        $ChangelogPath = Join-Path $FixtureRoot "CHANGELOG.md"
        Set-Content -Path $ChangelogPath -Value @'
## Unreleased

`1.2.2`
- previous release
'@

        $Output = Invoke-PrereleaseNotes -Arguments @(
            "--changelog", $ChangelogPath,
            "--version", "1.2.3",
            "--check-only")
        if ($LASTEXITCODE -eq 0) {
            throw "prerelease-notes.sh unexpectedly accepted a missing version entry."
        }

        Assert-Match -Text $Output -Pattern "does not contain notes for '1\.2\.3'" -Message "Missing version rejection message was not specific."
    }
    finally {
        Remove-Item -Recurse -Force -LiteralPath $FixtureRoot
    }
}

function Test-ReleaseWorkflowStagesAndChecksReleaseChangelog {
    $WorkflowPath = Join-Path (Split-Path -Parent (Split-Path -Parent $ScriptRoot)) ".github/workflows/release.yml"
    $WorkflowText = Get-Content -Raw -Path $WorkflowPath

    $PreserveMarker = "      - name: Preserve release helper scripts"
    $CheckoutMarker = "      - name: Checkout selected release tag"
    $StageMarker = "      - name: Stage selected release contents"
    $ChangelogMarker = "      - name: Validate staged release changelog"

    $PreserveIndex = $WorkflowText.IndexOf($PreserveMarker, [StringComparison]::Ordinal)
    $CheckoutIndex = $WorkflowText.IndexOf($CheckoutMarker, [StringComparison]::Ordinal)
    $StageIndex = $WorkflowText.IndexOf($StageMarker, [StringComparison]::Ordinal)
    $ChangelogIndex = $WorkflowText.IndexOf($ChangelogMarker, [StringComparison]::Ordinal)

    if ($PreserveIndex -lt 0) {
        throw "release.yml is missing the Preserve release helper scripts step."
    }

    if ($CheckoutIndex -lt 0) {
        throw "release.yml is missing selected tag checkout."
    }

    if ($StageIndex -lt 0) {
        throw "release.yml is missing the Stage selected release contents step."
    }

    if ($ChangelogIndex -lt 0) {
        throw "release.yml is missing staged release changelog validation."
    }

    if ($CheckoutIndex -lt $PreserveIndex) {
        throw "Release helper scripts must be preserved before checking out the selected tag."
    }

    if ($StageIndex -lt $CheckoutIndex) {
        throw "Release contents must be staged after checking out the selected tag."
    }

    if ($ChangelogIndex -lt $StageIndex) {
        throw "Staged release changelog validation must run after staging release contents."
    }

    if ($WorkflowText -notmatch 'cd \./dist/thunderstore-publish') {
        throw "Thunderstore publish should run from the staged publish root."
    }

    if ($WorkflowText -notmatch '\$RUNNER_TEMP/prerelease-notes\.sh') {
        throw "release.yml should run changelog validation from the preserved helper script."
    }
}

Test-PrereleaseNotesIncludesChangelogAndDetailsCard
Test-PrereleaseNotesRejectsUnreleasedContent
Test-PrereleaseNotesRejectsMissingUnreleasedHeader
Test-PrereleaseNotesRejectsMissingVersionEntry
Test-ReleaseWorkflowStagesAndChecksReleaseChangelog

Write-Host "prerelease-notes tests passed"
