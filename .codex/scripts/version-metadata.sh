#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(dirname "$(dirname "$SCRIPT_DIR")")"
CSPROJ_PATH="$REPO_ROOT/Bloodcraft.csproj"
THUNDERSTORE_PATH="$REPO_ROOT/thunderstore.toml"
CHANGELOG_PATH="$REPO_ROOT/CHANGELOG.md"

read_csproj_version() {
    local version
    version=$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' "$CSPROJ_PATH" | head -n 1 | tr -d '[:space:]')

    if [ -z "$version" ]; then
        echo "Unable to determine canonical version from $CSPROJ_PATH." >&2
        exit 1
    fi

    printf '%s\n' "$version"
}

read_thunderstore_version() {
    local version
    version=$(sed -n 's/^versionNumber = "\([^"]*\)"$/\1/p' "$THUNDERSTORE_PATH" | head -n 1 | tr -d '[:space:]')

    if [ -z "$version" ]; then
        echo "Unable to determine thunderstore version from $THUNDERSTORE_PATH." >&2
        exit 1
    fi

    printf '%s\n' "$version"
}

validate_changelog_version() {
    local expected_version="$1"
    local first_line expected_line

    first_line=$(sed -n '1p' "$CHANGELOG_PATH" | tr -d '\r')
    expected_line="\`$expected_version\`"

    if [ "$first_line" != "$expected_line" ]; then
        echo "CHANGELOG version mismatch: expected first line '$expected_line' in $CHANGELOG_PATH but found '$first_line'." >&2
        exit 1
    fi
}

canonical_version=$(read_csproj_version)
thunderstore_version=$(read_thunderstore_version)

if [ "$thunderstore_version" != "$canonical_version" ]; then
    echo "Version mismatch: $THUNDERSTORE_PATH has $thunderstore_version but $CSPROJ_PATH has canonical version $canonical_version." >&2
    exit 1
fi

validate_changelog_version "$canonical_version"

if [ -n "${GITHUB_OUTPUT:-}" ]; then
    echo "canonical_version=$canonical_version" >> "$GITHUB_OUTPUT"
fi

echo "canonical_version=$canonical_version"
