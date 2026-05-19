#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(dirname "$(dirname "$SCRIPT_DIR")")"

CHANGELOG_PATH="$REPO_ROOT/CHANGELOG.md"
VERSION=""
TAG=""
BRANCH=""
COMMIT=""
RUN_ID=""
OUTPUT_PATH=""
CHECK_ONLY="false"

fail() {
    echo "Error: $1" >&2
    exit 1
}

usage() {
    cat >&2 <<'EOF'
Usage: prerelease-notes.sh --version X.Y.Z [options]

Options:
  --changelog PATH   CHANGELOG.md path to read.
  --tag TAG          GitHub Release tag. Defaults to vX.Y.Z-pre.
  --branch NAME      Source branch name for the notes card.
  --commit SHA       Source commit SHA for the notes card.
  --run-id ID        GitHub Actions run id for the notes card.
  --output PATH      Markdown file to write.
  --check-only       Validate changelog turnover without writing notes.
EOF
}

while [ "$#" -gt 0 ]; do
    case "$1" in
        --changelog)
            CHANGELOG_PATH="${2:-}"
            shift 2
            ;;
        --version)
            VERSION="${2:-}"
            shift 2
            ;;
        --tag)
            TAG="${2:-}"
            shift 2
            ;;
        --branch)
            BRANCH="${2:-}"
            shift 2
            ;;
        --commit)
            COMMIT="${2:-}"
            shift 2
            ;;
        --run-id)
            RUN_ID="${2:-}"
            shift 2
            ;;
        --output)
            OUTPUT_PATH="${2:-}"
            shift 2
            ;;
        --check-only)
            CHECK_ONLY="true"
            shift
            ;;
        -h|--help)
            usage
            exit 0
            ;;
        *)
            usage
            fail "Unknown argument '$1'."
            ;;
    esac
done

if [[ ! "$VERSION" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
    fail "--version must use canonical X.Y.Z format."
fi

if [ -z "$TAG" ]; then
    TAG="v${VERSION}-pre"
fi

if [ ! -f "$CHANGELOG_PATH" ]; then
    fail "Unable to locate changelog at '$CHANGELOG_PATH'."
fi

if ! grep -Eq '^##[[:space:]]*Unreleased[[:space:]]*$' "$CHANGELOG_PATH"; then
    fail "CHANGELOG.md must contain a ## Unreleased section before creating or publishing a prerelease."
fi

unreleased_body=$(
    awk '
        /^##[[:space:]]*Unreleased[[:space:]]*$/ { in_unreleased = 1; next }
        in_unreleased && /^## / { exit }
        in_unreleased { print }
    ' "$CHANGELOG_PATH"
)

if printf '%s' "$unreleased_body" | grep -q '[^[:space:]]'; then
    fail "CHANGELOG.md ## Unreleased must be empty before creating or publishing a prerelease."
fi

version_body=$(
    awk -v version="$VERSION" '
        $0 == "## v" version { in_version = 1; next }
        in_version && /^## / { exit }
        in_version { print }
    ' "$CHANGELOG_PATH"
)

if ! printf '%s' "$version_body" | grep -q '[^[:space:]]'; then
    fail "CHANGELOG.md does not contain notes for '$VERSION'."
fi

if [ "$CHECK_ONLY" = "true" ]; then
    echo "prerelease-notes: changelog turnover validated for $VERSION."
    exit 0
fi

if [ -z "$OUTPUT_PATH" ]; then
    fail "--output is required unless --check-only is used."
fi

BRANCH="${BRANCH:-unknown}"
COMMIT="${COMMIT:-unknown}"
RUN_ID="${RUN_ID:-unknown}"
short_commit="$COMMIT"
if [ ${#short_commit} -gt 12 ]; then
    short_commit="${short_commit:0:12}"
fi

run_detail="\`$RUN_ID\`"
if [ -n "${GITHUB_REPOSITORY:-}" ] && [ "$RUN_ID" != "unknown" ]; then
    run_detail="[${RUN_ID}](https://github.com/${GITHUB_REPOSITORY}/actions/runs/${RUN_ID})"
fi

handoff_heading="### Thunderstore handoff"
if [ -n "${GITHUB_REPOSITORY:-}" ] && [ "$COMMIT" != "unknown" ]; then
    handoff_heading="<p><img src=\"https://raw.githubusercontent.com/${GITHUB_REPOSITORY}/${COMMIT}/.github/assets/ts_badge.png\" alt=\"Thunderstore handoff\" width=\"96\" /></p>"
fi
mkdir -p "$(dirname "$OUTPUT_PATH")"
cat > "$OUTPUT_PATH" <<EOF
## Bloodcraft ${VERSION} pre-release

> [!NOTE]
> This GitHub pre-release is the source artifact for the matching Thunderstore publish. Thunderstore receives package version \`${VERSION}\` from tag \`${TAG}\`.

${handoff_heading}

| Signal | Detail |
| --- | --- |
| 📝 Changelog | \`## Unreleased\` is empty; notes below come from \`v${VERSION}\`. |
| 🌿 Branch | \`${BRANCH}\` |
| 🔖 Commit | \`${short_commit}\` |
| ▶️ Run | ${run_detail} |
| 🏷️ Tag | \`${TAG}\` |
| 📦 Package | \`${VERSION}\` |

### ✨ Changes

${version_body}
EOF

echo "prerelease-notes: wrote $OUTPUT_PATH"
