#!/usr/bin/env bash
set -euo pipefail

WORKFLOW_PREFIX=".github/workflows/"
LOCALIZATION_PREFIXES=("Resources/Localization/" "Localization/" "i18n/" "lang/" "locale/")
RELEASE_FILES=("Bloodcraft.csproj" "CHANGELOG.md" "thunderstore.toml")
CONFIG_PATTERNS=(
    ".editorconfig"
    ".gitattributes"
    ".gitignore"
    ".github/"
    ".vscode/"
    ".codex/"
    "Directory.Build.props"
    "Directory.Build.targets"
    "NuGet.config"
)
TEST_PATH_HINTS=("test" "tests" "spec" "specs")
TEST_FILE_HINTS=("*Test*.cs" "*Tests*.cs" "*.spec.*" "*.test.*")
PRODUCTION_CODE_EXTENSIONS=("*.cs")

append_summary() {
    local message="$1"

    if [[ -n "${GITHUB_STEP_SUMMARY:-}" ]]; then
        printf '%s\n' "$message" >> "$GITHUB_STEP_SUMMARY"
    fi
}

emit_group() {
    local title="$1"
    shift
    local -a items=("$@")

    echo "$title"
    append_summary "$title"

    if [[ ${#items[@]} -eq 0 ]]; then
        echo "- none"
        append_summary "- none"
        return
    fi

    local item
    for item in "${items[@]}"; do
        echo "- $item"
        append_summary "- \`$item\`"
    done
}

emit_notice() {
    local title="$1"
    local message="$2"

    echo "::notice title=$title::$message"
}

matches_prefixes() {
    local path="$1"
    shift
    local prefix

    for prefix in "$@"; do
        if [[ "$path" == "$prefix"* ]]; then
            return 0
        fi
    done

    return 1
}

is_release_file() {
    local path="$1"
    local release_file

    for release_file in "${RELEASE_FILES[@]}"; do
        if [[ "$path" == "$release_file" ]]; then
            return 0
        fi
    done

    return 1
}

is_config_file() {
    local path="$1"
    local pattern

    for pattern in "${CONFIG_PATTERNS[@]}"; do
        if [[ "$pattern" == */ ]]; then
            if [[ "$path" == "$pattern"* ]]; then
                return 0
            fi
            continue
        fi

        if [[ "$path" == "$pattern" ]]; then
            return 0
        fi
    done

    case "$path" in
        *.json|*.jsonc|*.yml|*.yaml|*.toml|*.props|*.targets|*.config)
            return 0
            ;;
    esac

    return 1
}

is_test_file() {
    local path="$1"
    local hint

    for hint in "${TEST_PATH_HINTS[@]}"; do
        if [[ "$path" == *"/$hint/"* ]] || [[ "$path" == "$hint/"* ]] || [[ "$path" == *".$hint."* ]]; then
            return 0
        fi
    done

    for hint in "${TEST_FILE_HINTS[@]}"; do
        if [[ "$path" == $hint ]]; then
            return 0
        fi
    done

    return 1
}

is_production_code_file() {
    local path="$1"
    local extension_pattern

    if is_test_file "$path"; then
        return 1
    fi

    for extension_pattern in "${PRODUCTION_CODE_EXTENSIONS[@]}"; do
        if [[ "$path" == $extension_pattern ]]; then
            return 0
        fi
    done

    return 1
}

extract_csproj_version() {
    sed -nE 's|.*<Version>([^<]+)</Version>.*|\1|p' Bloodcraft.csproj | head -n 1
}

extract_thunderstore_version() {
    sed -nE 's|^versionNumber[[:space:]]*=[[:space:]]*"([^"]+)".*|\1|p' thunderstore.toml | head -n 1
}

extract_changelog_version() {
    awk 'match($0, /[0-9]+(\.[0-9]+){1,3}([-+][^] ]+)?/) { print substr($0, RSTART, RLENGTH); exit }' CHANGELOG.md
}

RANGE="${1:-}"
if [[ -z "$RANGE" ]]; then
    echo "Usage: $0 <git-diff-range>" >&2
    exit 1
fi

if ! changed_output="$(git diff --name-only --diff-filter=ACMRDT "$RANGE")"; then
    echo "Unable to inspect changed files for range: $RANGE" >&2
    exit 1
fi

mapfile -t CHANGED_PATHS < <(printf '%s\n' "$changed_output")

if [[ ${#CHANGED_PATHS[@]} -eq 0 || ( ${#CHANGED_PATHS[@]} -eq 1 && -z "${CHANGED_PATHS[0]}" ) ]]; then
    echo "No changed tracked files detected in $RANGE."
    append_summary "## PR changed-files summary"
    append_summary "No changed tracked files detected in \`$RANGE\`."
    exit 0
fi

WORKFLOW_FILES=()
CONFIG_FILES=()
LOCALIZATION_FILES=()
RELEASE_CHANGED=()
TEST_FILES=()
PRODUCTION_CODE_FILES=()
OTHER_FILES=()

for path in "${CHANGED_PATHS[@]}"; do
    if [[ "$path" == "$WORKFLOW_PREFIX"* ]]; then
        WORKFLOW_FILES+=("$path")
        continue
    fi

    if matches_prefixes "$path" "${LOCALIZATION_PREFIXES[@]}"; then
        LOCALIZATION_FILES+=("$path")
        continue
    fi

    if is_release_file "$path"; then
        RELEASE_CHANGED+=("$path")
        continue
    fi

    if is_test_file "$path"; then
        TEST_FILES+=("$path")
        continue
    fi

    if is_production_code_file "$path"; then
        PRODUCTION_CODE_FILES+=("$path")
        continue
    fi

    if is_config_file "$path"; then
        CONFIG_FILES+=("$path")
        continue
    fi

    OTHER_FILES+=("$path")
done

TOTAL_CHANGED=${#CHANGED_PATHS[@]}
TEST_SIGNAL="No obvious test files were touched."
RELEASE_METADATA_SIGNAL="No production C# files were touched, so no release-metadata expectation is implied."
RELEASE_SYNC_SIGNAL="No release metadata update detected."
RELEASE_SYNC_NOTICE_TITLE="Release metadata unchanged"
RELEASE_SYNC_NOTICE_MESSAGE="No release metadata update detected in Bloodcraft.csproj, CHANGELOG.md, or thunderstore.toml."

if [[ ${#TEST_FILES[@]} -gt 0 ]]; then
    TEST_SIGNAL="Yes — test-related files appear to be touched (${#TEST_FILES[@]})."
fi

if [[ ${#PRODUCTION_CODE_FILES[@]} -gt 0 ]]; then
    if [[ ${#RELEASE_CHANGED[@]} -gt 0 ]]; then
        RELEASE_METADATA_SIGNAL="Yes — production C# files changed and release-facing metadata was updated (${#RELEASE_CHANGED[@]} file(s))."
    else
        RELEASE_METADATA_SIGNAL="No — production C# files changed without touching release-facing metadata. Reviewers should confirm that any version/changelog update is intentionally deferred."
    fi
fi

if [[ ${#RELEASE_CHANGED[@]} -eq 0 ]]; then
    RELEASE_SYNC_SIGNAL="No release metadata update detected."
elif [[ ${#RELEASE_CHANGED[@]} -ne ${#RELEASE_FILES[@]} ]]; then
    RELEASE_SYNC_SIGNAL="Release metadata updated in only ${#RELEASE_CHANGED[@]} of ${#RELEASE_FILES[@]} expected files. Version-facing updates should keep Bloodcraft.csproj, CHANGELOG.md, and thunderstore.toml synchronized."
    RELEASE_SYNC_NOTICE_TITLE="Release metadata partially updated"
    RELEASE_SYNC_NOTICE_MESSAGE="Release-facing version updates are expected to update Bloodcraft.csproj, CHANGELOG.md, and thunderstore.toml together."
else
    csproj_version="$(extract_csproj_version)"
    thunderstore_version="$(extract_thunderstore_version)"
    changelog_version="$(extract_changelog_version)"

    if [[ -z "$csproj_version" || -z "$thunderstore_version" || -z "$changelog_version" ]]; then
        RELEASE_SYNC_SIGNAL="Release metadata files changed together, but at least one version value could not be parsed. Parsed values — csproj: '${csproj_version:-missing}', thunderstore: '${thunderstore_version:-missing}', changelog: '${changelog_version:-missing}'."
        RELEASE_SYNC_NOTICE_TITLE="Release metadata parse warning"
        RELEASE_SYNC_NOTICE_MESSAGE="Release metadata files changed together, but one or more version values could not be parsed for synchronization review."
    elif [[ "$csproj_version" == "$thunderstore_version" && "$csproj_version" == "$changelog_version" ]]; then
        RELEASE_SYNC_SIGNAL="Release metadata is synchronized at version $csproj_version across Bloodcraft.csproj, CHANGELOG.md, and thunderstore.toml."
        RELEASE_SYNC_NOTICE_TITLE="Release metadata synchronized"
        RELEASE_SYNC_NOTICE_MESSAGE="Release metadata is synchronized at version $csproj_version across Bloodcraft.csproj, CHANGELOG.md, and thunderstore.toml."
    else
        RELEASE_SYNC_SIGNAL="Release metadata versions do not match — Bloodcraft.csproj: $csproj_version; thunderstore.toml: $thunderstore_version; CHANGELOG.md: $changelog_version."
        RELEASE_SYNC_NOTICE_TITLE="Release metadata version mismatch"
        RELEASE_SYNC_NOTICE_MESSAGE="Release metadata versions do not match across Bloodcraft.csproj, CHANGELOG.md, and thunderstore.toml."
    fi
fi

append_summary "## PR changed-files summary"
append_summary "Changed files inspected: **$TOTAL_CHANGED**"
append_summary "Tests touched: **$TEST_SIGNAL**"
append_summary "Release metadata updated alongside production C# changes: **$RELEASE_METADATA_SIGNAL**"
append_summary "Release metadata synchronization review: **$RELEASE_SYNC_SIGNAL**"
append_summary ""

echo "Changed files inspected: $TOTAL_CHANGED"
echo "Tests touched: $TEST_SIGNAL"
echo "Release metadata updated alongside production C# changes: $RELEASE_METADATA_SIGNAL"
echo "Release metadata synchronization review: $RELEASE_SYNC_SIGNAL"
emit_group "### Workflow changes" "${WORKFLOW_FILES[@]}"
emit_group "### Config changes" "${CONFIG_FILES[@]}"
emit_group "### Release/versioning changes" "${RELEASE_CHANGED[@]}"
emit_group "### Localization changes" "${LOCALIZATION_FILES[@]}"
emit_group "### Test-related changes" "${TEST_FILES[@]}"
emit_group "### Production C# changes" "${PRODUCTION_CODE_FILES[@]}"
emit_group "### Other changed files" "${OTHER_FILES[@]}"

if [[ ${#WORKFLOW_FILES[@]} -gt 0 ]]; then
    emit_notice "Workflow files changed" "Workflow definitions changed in this PR."
fi

if [[ ${#CONFIG_FILES[@]} -gt 0 ]]; then
    emit_notice "Config files changed" "Configuration-oriented files changed in this PR."
fi

if [[ ${#LOCALIZATION_FILES[@]} -gt 0 ]]; then
    emit_notice "Localization files changed" "Localization resources changed in this PR."
fi

if [[ ${#RELEASE_CHANGED[@]} -gt 0 ]]; then
    emit_notice "Release/versioning files changed" "Release metadata changed in this PR."
fi

if [[ ${#PRODUCTION_CODE_FILES[@]} -gt 0 ]]; then
    if [[ ${#RELEASE_CHANGED[@]} -gt 0 ]]; then
        emit_notice "Production C# changes include release metadata" "Production C# files changed and release-facing metadata files were updated in the same PR."
    else
        emit_notice "Production C# changes without release metadata" "Production C# files changed without updates to CHANGELOG.md, Bloodcraft.csproj, or thunderstore.toml. Reviewers should confirm whether release metadata is intentionally deferred."
    fi
fi

if [[ ${#TEST_FILES[@]} -eq 0 ]]; then
    emit_notice "Tests not obviously touched" "No obvious test files were detected in the PR diff."
else
    emit_notice "Tests appear touched" "Detected ${#TEST_FILES[@]} test-related file(s) in the PR diff."
fi

emit_notice "$RELEASE_SYNC_NOTICE_TITLE" "$RELEASE_SYNC_NOTICE_MESSAGE"

append_summary ""
append_summary "### Release metadata review cue"
append_summary "- Production C# files changed: **${#PRODUCTION_CODE_FILES[@]}**"
append_summary "- Release-facing metadata files changed: **${#RELEASE_CHANGED[@]}**"
append_summary "- Release metadata status: **$RELEASE_SYNC_SIGNAL**"
append_summary "- Reviewer prompt: confirm whether release metadata updates are intentionally included or intentionally deferred."
append_summary "- Messaging prompt: workflow-only, process-only, or docs-only PR titles/descriptions should not claim a version bump unless all release metadata files changed together."
append_summary ""
append_summary "_This workflow is informational only and does not block the pull request._"
