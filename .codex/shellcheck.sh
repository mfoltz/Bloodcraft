#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(dirname "$SCRIPT_DIR")"

SHELL_SCRIPTS=(
    "$REPO_ROOT/.codex/install.sh"
    "$REPO_ROOT/.codex/shellcheck.sh"
)

if ! command -v shellcheck >/dev/null 2>&1; then
    echo "shellcheck is required but was not found on PATH. Run 'bash .codex/install.sh' first to install repository tooling." >&2
    exit 1
fi

echo "Linting shell scripts with shellcheck:"
printf ' - %s\n' "${SHELL_SCRIPTS[@]#$REPO_ROOT/}"

shellcheck "${SHELL_SCRIPTS[@]}"
