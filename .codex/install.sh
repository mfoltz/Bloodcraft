#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(dirname "$SCRIPT_DIR")"
PROJECT_PATH="$REPO_ROOT/Bloodcraft.csproj"
INSTALL_DIR="${DOTNET_INSTALL_DIR:-$HOME/.dotnet}"
REQUIRED_SDK_CHANNEL="${DOTNET_INSTALL_CHANNEL:-8.0}"
BEPINEX_PLUGIN_DIR="${BEPINEX_PLUGIN_DIR:-}"
REQUIRED_TARGET_RUNTIME="Microsoft.NETCore.App 6.0"
REQUIRED_SDK_VERSION="$(awk -F '"' '/"version"/ { print $4; exit }' "$REPO_ROOT/global.json")"
REQUIRED_SDK_MAJOR="${REQUIRED_SDK_VERSION%%.*}"
SDK_BOOTSTRAPPED=0

if [ -z "$REQUIRED_SDK_VERSION" ]; then
    echo "Unable to determine the required .NET SDK version from $REPO_ROOT/global.json" >&2
    exit 1
fi

get_dotnet_sdk_version() {
    local dotnet_cmd="${1:-dotnet}"

    if ! command -v "$dotnet_cmd" >/dev/null 2>&1; then
        return 1
    fi

    "$dotnet_cmd" --version 2>/dev/null
}

is_sdk_version_adequate() {
    local sdk_version="$1"
    local sdk_major="${sdk_version%%.*}"

    [[ -n "$sdk_version" && "$sdk_major" =~ ^[0-9]+$ && "$sdk_major" -ge "$REQUIRED_SDK_MAJOR" ]]
}

log_sdk_requirement() {
    local detected_sdk_version="${1:-not detected}"
    echo "Detected .NET SDK version: $detected_sdk_version | Required SDK channel: $REQUIRED_SDK_CHANNEL | Required SDK version: >= $REQUIRED_SDK_VERSION | Target runtime: $REQUIRED_TARGET_RUNTIME"
}

ensure_python_yaml() {
    if ! command -v python3 >/dev/null 2>&1; then
        echo "python3 not found; skipping PyYAML installation." >&2
        return
    fi

    if python3 - <<'PY' >/dev/null 2>&1
import yaml
PY
    then
        echo "PyYAML already installed for python3"
        return
    fi

    echo "Installing PyYAML for python3 user site-packages..."
    python3 -m ensurepip --upgrade >/dev/null 2>&1 || true
    python3 -m pip install --user PyYAML
}

ensure_shellcheck() {
    if command -v shellcheck >/dev/null 2>&1; then
        echo "shellcheck already installed"
        return
    fi

    echo "shellcheck not found; attempting to install shell lint tooling..."

    if command -v apt-get >/dev/null 2>&1; then
        apt-get update && apt-get install -y shellcheck
        return
    fi

    if command -v brew >/dev/null 2>&1; then
        brew install shellcheck
        return
    fi

    if command -v dnf >/dev/null 2>&1; then
        dnf install -y shellcheck
        return
    fi

    if command -v yum >/dev/null 2>&1; then
        yum install -y shellcheck
        return
    fi

    echo "shellcheck could not be installed automatically because no supported package manager was found. Shell linting is unavailable in this environment." >&2
}

install_dotnet() {
    mkdir -p "$INSTALL_DIR"
    local install_script
    install_script="$(mktemp)"

    curl -sSL https://dot.net/v1/dotnet-install.sh -o "$install_script"

    bash "$install_script" --install-dir "$INSTALL_DIR" --channel "$REQUIRED_SDK_CHANNEL" --version "$REQUIRED_SDK_VERSION"

    rm -f "$install_script"

    export DOTNET_ROOT="$INSTALL_DIR"
    export PATH="$INSTALL_DIR:$INSTALL_DIR/tools:$PATH"
    hash -r

    local installed_sdk_version
    installed_sdk_version="$(get_dotnet_sdk_version dotnet || true)"

    if is_sdk_version_adequate "$installed_sdk_version"; then
        echo "Installed .NET SDK $installed_sdk_version to $INSTALL_DIR"
        echo "Add the following to your shell profile to use it outside this script:"
        echo "export DOTNET_ROOT=\"$INSTALL_DIR\""
        echo "export PATH=\"$INSTALL_DIR:$INSTALL_DIR/tools:\$PATH\""
    else
        echo "Installation completed but an adequate dotnet SDK is not on PATH. Expected >= $REQUIRED_SDK_VERSION, found ${installed_sdk_version:-none}." >&2
        exit 1
    fi
}

DETECTED_SDK_VERSION="$(get_dotnet_sdk_version dotnet || true)"
log_sdk_requirement "${DETECTED_SDK_VERSION:-not detected}"

if is_sdk_version_adequate "$DETECTED_SDK_VERSION"; then
    echo ".NET SDK already installed and meets repository requirements: $DETECTED_SDK_VERSION"
    SDK_BOOTSTRAPPED=1
else
    if [ -n "$DETECTED_SDK_VERSION" ]; then
        echo "Installed .NET SDK $DETECTED_SDK_VERSION does not meet repository requirements; installing repo-managed SDK into $INSTALL_DIR"
    else
        echo "dotnet SDK not found; installing repo-managed SDK into $INSTALL_DIR"
    fi

    install_dotnet
    DETECTED_SDK_VERSION="$(get_dotnet_sdk_version dotnet || true)"
    log_sdk_requirement "${DETECTED_SDK_VERSION:-not detected}"
fi

ensure_python_yaml
ensure_shellcheck

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

if [ "$SDK_BOOTSTRAPPED" -eq 1 ]; then
    exit 0
fi
