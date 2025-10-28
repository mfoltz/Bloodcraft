#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(dirname "$SCRIPT_DIR")"
TEST_PROJECT="$SCRIPT_DIR/Bloodcraft.Tests.csproj"

# Ensure the SDK, project dependencies, and test assets are restored before running tests.
bash "$REPO_ROOT/install.sh"

# Rehydrate the dotnet CLI context in case the install script installed a local SDK.
export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
export PATH="$DOTNET_ROOT:$DOTNET_ROOT/tools:$PATH"

if ! command -v dotnet >/dev/null 2>&1; then
    echo "dotnet CLI not found on PATH after running install.sh" >&2
    exit 1
fi

dotnet test "$TEST_PROJECT" "$@"
