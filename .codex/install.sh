#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(dirname "$SCRIPT_DIR")"
PROJECT_PATH="$REPO_ROOT/Bloodcraft.csproj"
INSTALL_DIR="${DOTNET_INSTALL_DIR:-$HOME/.dotnet}"
CHANNEL="${DOTNET_INSTALL_CHANNEL:-8.0}"
BEPINEX_PLUGIN_DIR="${BEPINEX_PLUGIN_DIR:-}"
DOTNET_INSTALLED=0
REQUIRED_SDK_MAJOR="${CHANNEL%%.*}"
PYTHON_COMMANDS=(python3 python)
PYTHON_DEPENDENCY="PyYAML"
PYTHON_DEPENDENCY_IMPORT="yaml"

sdk_meets_minimum_version() {
    if ! command -v dotnet >/dev/null 2>&1; then
        return 1
    fi

    local sdk_version
    sdk_version="$(dotnet --version 2>/dev/null || true)"

    if [ -z "$sdk_version" ]; then
        return 1
    fi

    local sdk_major
    sdk_major="${sdk_version%%.*}"

    [ "$sdk_major" -ge "$REQUIRED_SDK_MAJOR" ]
}

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

python_dependency_is_available() {
    local python_command="$1"
    "$python_command" - <<PY >/dev/null 2>&1
import importlib.util
import sys

sys.exit(0 if importlib.util.find_spec("${PYTHON_DEPENDENCY_IMPORT}") else 1)
PY
}

install_python_dependency() {
    local python_command="$1"

    echo "Ensuring $PYTHON_DEPENDENCY is available for $python_command..."

    if ! "$python_command" -m pip --version >/dev/null 2>&1; then
        "$python_command" -m ensurepip --upgrade >/dev/null 2>&1 || true
    fi

    "$python_command" -m pip install --user --upgrade "$PYTHON_DEPENDENCY"
}

ensure_python_dependency() {
    local available_python=0
    local python_command

    for python_command in "${PYTHON_COMMANDS[@]}"; do
        if ! command -v "$python_command" >/dev/null 2>&1; then
            continue
        fi

        available_python=1

        if python_dependency_is_available "$python_command"; then
            echo "$PYTHON_DEPENDENCY already available for $python_command"
            continue
        fi

        install_python_dependency "$python_command"

        if ! python_dependency_is_available "$python_command"; then
            echo "Failed to make $PYTHON_DEPENDENCY_IMPORT importable for $python_command" >&2
            exit 1
        fi
    done

    if [ "$available_python" -eq 0 ]; then
        echo "Python not found; skipping $PYTHON_DEPENDENCY setup." >&2
    fi
}

if command -v dotnet >/dev/null 2>&1; then
    echo ".NET SDK already installed: $(dotnet --version)"
    if sdk_meets_minimum_version; then
        DOTNET_INSTALLED=1
    else
        echo ".NET SDK $(dotnet --version) is older than the required major version $REQUIRED_SDK_MAJOR; installing channel $CHANNEL into $INSTALL_DIR"
        install_dotnet
    fi
else
    install_dotnet
fi

ensure_python_dependency

if [ ! -f "$PROJECT_PATH" ]; then
    echo "Project file not found at $PROJECT_PATH" >&2
    exit 1
fi

echo "Restoring Bloodcraft project dependencies..."
dotnet restore "$PROJECT_PATH"

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
