#!/usr/bin/env bash
set -euo pipefail

INSTALL_DIR="${DOTNET_INSTALL_DIR:-$HOME/.dotnet}"
CHANNEL="${DOTNET_INSTALL_CHANNEL:-6.0}"

if command -v dotnet >/dev/null 2>&1; then
    echo ".NET SDK already installed: $(dotnet --version)"
    exit 0
fi

mkdir -p "$INSTALL_DIR"
INSTALL_SCRIPT="$(mktemp)"
trap 'rm -f "$INSTALL_SCRIPT"' EXIT

curl -sSL https://dot.net/v1/dotnet-install.sh -o "$INSTALL_SCRIPT"

bash "$INSTALL_SCRIPT" --install-dir "$INSTALL_DIR" --channel "$CHANNEL"

export DOTNET_ROOT="$INSTALL_DIR"
PATH="$INSTALL_DIR:$INSTALL_DIR/tools:$PATH"

if command -v dotnet >/dev/null 2>&1; then
    echo "Installed .NET SDK $(dotnet --version) to $INSTALL_DIR"
    echo "Add the following to your shell profile to use it outside this script:"
    echo "export DOTNET_ROOT=\"$INSTALL_DIR\""
    echo "export PATH=\"$INSTALL_DIR:$INSTALL_DIR/tools:\$PATH\""
else
    echo "Installation completed but dotnet is not on PATH. Add $INSTALL_DIR to PATH manually." >&2
    exit 1
fi
