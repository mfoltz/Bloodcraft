#!/usr/bin/env bash
set -e

# Directory of this script
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"

DOTNET_VERSION="6.0"


if ! command -v dotnet >/dev/null; then
    echo "Installing .NET $DOTNET_VERSION SDK..."
    curl -sSL https://dot.net/v1/dotnet-install.sh -o "$SCRIPT_DIR/dotnet-install.sh"
    bash "$SCRIPT_DIR/dotnet-install.sh" --channel "$DOTNET_VERSION"
    export PATH="$HOME/.dotnet:$PATH"
fi

# Ensure embedded secrets exist before restoring
SECRETS_FILE="$ROOT_DIR/Resources/secrets.json"
if [ ! -f "$SECRETS_FILE" ]; then
    mkdir -p "$(dirname "$SECRETS_FILE")"
    cat >"$SECRETS_FILE" <<'EOF'
{
  "NEW_SHARED_KEY": "3xUjC8aOZ9+RlWoCphxH5MvFOPkvIf2P/8b89GiQjbE="
}
EOF
fi

dotnet restore "$ROOT_DIR/Bloodcraft.csproj"
