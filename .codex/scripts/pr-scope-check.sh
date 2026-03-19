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

    if is_config_file "$path"; then
        CONFIG_FILES+=("$path")
        continue
    fi

    OTHER_FILES+=("$path")
done

TOTAL_CHANGED=${#CHANGED_PATHS[@]}
TEST_SIGNAL="No obvious test files were touched."
if [[ ${#TEST_FILES[@]} -gt 0 ]]; then
    TEST_SIGNAL="Yes — test-related files appear to be touched (${#TEST_FILES[@]})."
fi

append_summary "## PR changed-files summary"
append_summary "Changed files inspected: **$TOTAL_CHANGED**"
append_summary "Tests touched: **$TEST_SIGNAL**"
append_summary ""

echo "Changed files inspected: $TOTAL_CHANGED"
echo "Tests touched: $TEST_SIGNAL"
emit_group "### Workflow changes" "${WORKFLOW_FILES[@]}"
emit_group "### Config changes" "${CONFIG_FILES[@]}"
emit_group "### Release/versioning changes" "${RELEASE_CHANGED[@]}"
emit_group "### Localization changes" "${LOCALIZATION_FILES[@]}"
emit_group "### Test-related changes" "${TEST_FILES[@]}"
emit_group "### Other changed files" "${OTHER_FILES[@]}"

if [[ ${#WORKFLOW_FILES[@]} -gt 0 ]]; then
    echo "::notice title=Workflow files changed::Workflow definitions changed in this PR."
fi

if [[ ${#CONFIG_FILES[@]} -gt 0 ]]; then
    echo "::notice title=Config files changed::Configuration-oriented files changed in this PR."
fi

if [[ ${#LOCALIZATION_FILES[@]} -gt 0 ]]; then
    echo "::notice title=Localization files changed::Localization resources changed in this PR."
fi

if [[ ${#RELEASE_CHANGED[@]} -gt 0 ]]; then
    echo "::notice title=Release/versioning files changed::Release metadata changed in this PR."
fi

if [[ ${#TEST_FILES[@]} -eq 0 ]]; then
    echo "::notice title=Tests not obviously touched::No obvious test files were detected in the PR diff."
else
    echo "::notice title=Tests appear touched::Detected ${#TEST_FILES[@]} test-related file(s) in the PR diff."
fi

append_summary ""
append_summary "_This workflow is informational only and does not block the pull request._"
