#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(dirname "$SCRIPT_DIR")"
PROJECT_PATH="$REPO_ROOT/Bloodcraft.csproj"
TEST_PROJECT_PATH="$REPO_ROOT/.codex/tests/Bloodcraft.Tests.csproj"
INSTALL_DIR="${DOTNET_INSTALL_DIR:-$HOME/.dotnet}"
CHANNEL="${DOTNET_INSTALL_CHANNEL:-6.0}"
BEPINEX_PLUGIN_DIR="${BEPINEX_PLUGIN_DIR:-}"
DOTNET_INSTALLED=0
REQUIRED_RUNTIME="Microsoft.NETCore.App 6.0"

install_dotnet() {
    mkdir -p "$INSTALL_DIR"
    local install_script
    install_script="$(mktemp)"

    curl -sSL https://dot.net/v1/dotnet-install.sh -o "$install_script"

    bash "$install_script" --install-dir "$INSTALL_DIR" --channel "$CHANNEL"

    rm -f "$install_script"

    export DOTNET_ROOT="$INSTALL_DIR"
    export PATH="$INSTALL_DIR:$INSTALL_DIR/tools:$PATH"
    hash -r

    if command -v dotnet >/dev/null 2>&1; then
        echo "Installed .NET SDK $(dotnet --version) to $INSTALL_DIR"
        echo "Add the following to your shell profile to use it outside this script:"
        echo "export DOTNET_ROOT=\"$INSTALL_DIR\""
        echo "export PATH=\"$INSTALL_DIR:$INSTALL_DIR/tools:\$PATH\""
    else
        echo "Installation completed but dotnet is not on PATH. Add $INSTALL_DIR to PATH manually." >&2
        exit 1
    fi
}

if command -v dotnet >/dev/null 2>&1; then
    echo ".NET SDK already installed: $(dotnet --version)"
    # Inspect the installed runtimes to ensure the required Microsoft.NETCore.App 6.0 runtime is available.
    if dotnet --list-runtimes 2>/dev/null | grep -q "^Microsoft.NETCore.App 6\\.0"; then
        DOTNET_INSTALLED=1
    else
        echo "Microsoft.NETCore.App 6.0 runtime not found; installing local runtime into $INSTALL_DIR"
        install_dotnet
    fi
else
    install_dotnet
fi

if [ ! -f "$PROJECT_PATH" ]; then
    echo "Project file not found at $PROJECT_PATH" >&2
    exit 1
fi

echo "Restoring Bloodcraft project dependencies..."
dotnet restore "$PROJECT_PATH"

if [ -f "$TEST_PROJECT_PATH" ]; then
    echo "Restoring test project dependencies..."
    dotnet restore "$TEST_PROJECT_PATH"
fi

echo "Building Bloodcraft project..."
dotnet build "$PROJECT_PATH" --configuration Release --no-restore -p:RunGenerateREADME=false

DLL_PATH="$REPO_ROOT/bin/Release/net6.0/Bloodcraft.dll"
if [ ! -f "$DLL_PATH" ]; then
    echo "Build failed: $DLL_PATH not found." >&2
    exit 1
fi

echo "Build succeeded: $DLL_PATH"

if [ -n "$BEPINEX_PLUGIN_DIR" ]; then
    if [ ! -d "$BEPINEX_PLUGIN_DIR" ]; then
        echo "BEPINEX_PLUGIN_DIR does not exist: $BEPINEX_PLUGIN_DIR" >&2
        exit 1
    fi

    cp "$DLL_PATH" "$BEPINEX_PLUGIN_DIR"
    echo "Copied $(basename "$DLL_PATH") to $BEPINEX_PLUGIN_DIR"
else
    echo "Set BEPINEX_PLUGIN_DIR to copy the built DLL into your BepInEx plugins directory."
fi

if [ "$DOTNET_INSTALLED" -eq 1 ]; then
    exit 0
fi
