#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(dirname "$(dirname "$SCRIPT_DIR")")"
CSPROJ_PATH="$REPO_ROOT/Bloodcraft.csproj"
THUNDERSTORE_PATH="$REPO_ROOT/thunderstore.toml"
CHANGELOG_PATH="$REPO_ROOT/CHANGELOG.md"
CANONICAL_VERSION_PATTERN='^[0-9]+\.[0-9]+\.[0-9]+$'

fail() {
    local message="$1"
    echo "Error: $message" >&2
    exit 1
}

read_csproj_version() {
    local version
    version=$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' "$CSPROJ_PATH" | head -n 1 | tr -d '[:space:]')

    if [ -z "$version" ]; then
        fail "Unable to determine canonical version from $CSPROJ_PATH."
    fi

    printf '%s\n' "$version"
}

read_thunderstore_version() {
    local version
    version=$(sed -n 's/^versionNumber = "\([^"]*\)"$/\1/p' "$THUNDERSTORE_PATH" | head -n 1 | tr -d '[:space:]')

    if [ -z "$version" ]; then
        fail "Unable to determine Thunderstore version from $THUNDERSTORE_PATH."
    fi

    printf '%s\n' "$version"
}

read_latest_changelog_entry() {
    local entry
    entry=$(sed -n '/^`[^`][^`]*`$/ { s/^`//; s/`$//; p; q; }' "$CHANGELOG_PATH" | tr -d '\r')

    if [ -z "$entry" ]; then
        fail "Unable to determine the latest CHANGELOG entry from $CHANGELOG_PATH."
    fi

    printf '%s\n' "$entry"
}

validate_canonical_version_format() {
    local version="$1"
    local source_name="$2"

    if [[ ! "$version" =~ $CANONICAL_VERSION_PATTERN ]]; then
        fail "$source_name version '$version' must match canonical format X.Y.Z."
    fi
}

emit_canonical_version() {
    local canonical_version="$1"

    if [ -n "${GITHUB_OUTPUT:-}" ]; then
        echo "canonical_version=$canonical_version" >> "$GITHUB_OUTPUT"
    fi

    echo "canonical_version=$canonical_version"
}

canonical_version=$(read_csproj_version)
validate_canonical_version_format "$canonical_version" "$CSPROJ_PATH"

thunderstore_version=$(read_thunderstore_version)
validate_canonical_version_format "$thunderstore_version" "$THUNDERSTORE_PATH"

if [ "$thunderstore_version" != "$canonical_version" ]; then
    fail "$THUNDERSTORE_PATH versionNumber '$thunderstore_version' does not match canonical version '$canonical_version' from $CSPROJ_PATH."
fi

changelog_version=$(read_latest_changelog_entry)
validate_canonical_version_format "$changelog_version" "$CHANGELOG_PATH"

if [ "$changelog_version" != "$canonical_version" ]; then
    fail "$CHANGELOG_PATH latest entry '$changelog_version' does not match canonical version '$canonical_version' from $CSPROJ_PATH."
fi

emit_canonical_version "$canonical_version"
