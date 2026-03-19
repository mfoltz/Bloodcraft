#!/usr/bin/env bash
set -euo pipefail

WORKFLOW_PREFIX=".github/workflows/"
LOCALIZATION_PREFIX="Resources/Localization/"
RELEASE_FILES=("Bloodcraft.csproj" "CHANGELOG.md" "thunderstore.toml")

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
    append_summary "## PR scope check"
    append_summary "No changed tracked files detected in \`$RANGE\`."
    exit 0
fi

WORKFLOW_FILES=()
LOCALIZATION_FILES=()
RELEASE_CHANGED=()
OTHER_FILES=()
HIGH_RISK_GROUPS=()

for path in "${CHANGED_PATHS[@]}"; do
    if [[ "$path" == "$WORKFLOW_PREFIX"* ]]; then
        WORKFLOW_FILES+=("$path")
        continue
    fi

    if [[ "$path" == "$LOCALIZATION_PREFIX"* ]]; then
        LOCALIZATION_FILES+=("$path")
        continue
    fi

    if is_release_file "$path"; then
        RELEASE_CHANGED+=("$path")
        continue
    fi

    OTHER_FILES+=("$path")
done

if [[ ${#WORKFLOW_FILES[@]} -gt 0 ]]; then
    HIGH_RISK_GROUPS+=("workflow")
fi

if [[ ${#LOCALIZATION_FILES[@]} -gt 0 ]]; then
    HIGH_RISK_GROUPS+=("localization")
fi

if [[ ${#RELEASE_CHANGED[@]} -gt 0 ]]; then
    HIGH_RISK_GROUPS+=("release/versioning")
fi

TOTAL_CHANGED=${#CHANGED_PATHS[@]}
HIGH_RISK_COUNT=${#HIGH_RISK_GROUPS[@]}

append_summary "## PR scope check"
append_summary "Changed files inspected: **$TOTAL_CHANGED**"

echo "Changed files inspected: $TOTAL_CHANGED"
emit_group "### Workflow changes" "${WORKFLOW_FILES[@]}"
emit_group "### Localization changes" "${LOCALIZATION_FILES[@]}"
emit_group "### Release/versioning changes" "${RELEASE_CHANGED[@]}"
emit_group "### Other changed files" "${OTHER_FILES[@]}"

STATUS="pass"
REASON="No broad-scope path combination detected."

if [[ $HIGH_RISK_COUNT -gt 1 ]]; then
    STATUS="fail"
    REASON="Multiple high-risk path groups were modified together: ${HIGH_RISK_GROUPS[*]}."
elif [[ $HIGH_RISK_COUNT -eq 1 && ${#OTHER_FILES[@]} -gt 0 ]]; then
    STATUS="warn"
    REASON="A high-risk path group was modified alongside other files: ${HIGH_RISK_GROUPS[0]}."
fi

append_summary ""
append_summary "**Status:** $STATUS"
append_summary "$REASON"

echo "Status: $STATUS"
echo "$REASON"

if [[ ${#WORKFLOW_FILES[@]} -gt 0 ]]; then
    echo "::warning title=Workflow files changed::Workflow definitions changed in this PR. Review CI/CD impact carefully."
fi

if [[ ${#LOCALIZATION_FILES[@]} -gt 0 ]]; then
    echo "::warning title=Localization files changed::Localization resources changed in this PR. Confirm translation and fallback coverage."
fi

if [[ ${#RELEASE_CHANGED[@]} -gt 0 ]]; then
    echo "::warning title=Release/versioning files changed::Release metadata changed in this PR. Confirm versioning and changelog intent."
fi

if [[ "$STATUS" == "warn" ]]; then
    echo "::warning title=Potentially broad PR scope::$REASON"
    exit 0
fi

if [[ "$STATUS" == "fail" ]]; then
    echo "::error title=Unexpectedly broad PR scope::$REASON"
    exit 1
fi
