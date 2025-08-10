#!/usr/bin/env bash
set -e

# Directory of this script
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"

DOTNET_VERSION="8.0"


if ! command -v dotnet >/dev/null; then
    echo "Installing .NET $DOTNET_VERSION SDK..."
    curl -sSL https://dot.net/v1/dotnet-install.sh -o "$SCRIPT_DIR/dotnet-install.sh"
    bash "$SCRIPT_DIR/dotnet-install.sh" --channel "$DOTNET_VERSION" --install-dir "$HOME/.dotnet"
fi

# Install .NET 6 targeting pack if not already present
if ! dotnet --list-sdks 2>/dev/null | grep -q '^6\.'; then
    echo "Installing .NET 6 targeting pack..."
    bash "$SCRIPT_DIR/dotnet-install.sh" --channel "6.0" --install-dir "$HOME/.dotnet" --no-path
fi

export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$DOTNET_ROOT:$PATH:$HOME/.local/bin"

# Persist environment variables for future sessions
if ! grep -q "DOTNET_ROOT" "$HOME/.bashrc"; then
    echo "export DOTNET_ROOT=$DOTNET_ROOT" >> "$HOME/.bashrc"
    echo "export PATH=\$DOTNET_ROOT:\$PATH:\$HOME/.local/bin" >> "$HOME/.bashrc"
fi

# Install Argos Translate and expose CLI
python3 -m pip install --user --no-cache-dir argostranslate==1.8.0
mkdir -p "$HOME/.local/bin"
cat >"$HOME/.local/bin/argos-translate" <<'EOF'
#!/usr/bin/env bash
python3 -m argostranslate.cli "$@"
EOF
chmod +x "$HOME/.local/bin/argos-translate"

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

# Install local Argos Translate models if segmented archives are present
MODEL_DIR="$ROOT_DIR/Resources/Localization/Models"
MODEL_INSTALLED=false
for d in "$MODEL_DIR"/EN_*; do
    [ -d "$d" ] || continue
    if compgen -G "$d"/translate-*.z[0-9][0-9] >/dev/null; then
        echo "Installing local Argos Translate model in $(basename "$d")..."
        if cat "$d"/translate-*.z[0-9][0-9] "$d"/translate-*.zip > "$d/model.zip"; then
            unzip -o "$d/model.zip" -d "$d" >/dev/null || true
            if compgen -G "$d"/translate-*.argosmodel >/dev/null; then
                if argos-translate install "$d"/translate-*.argosmodel; then
                    MODEL_INSTALLED=true
                else
                    echo "Failed to install local model in $(basename "$d"); will attempt remote download if configured."
                fi
                rm -f "$d/model.zip" "$d"/translate-*.argosmodel
            else
                echo "Failed to unzip local model in $(basename "$d"); will attempt remote download if configured."
            fi
        else
            echo "Failed to combine local model segments in $(basename "$d"); will attempt remote download if configured."
        fi
    fi
done

# Optional: download an Argos Translate language pair if requested
FROM_LANG="${FROM_LANG:-}"
TO_LANG="${TO_LANG:-}"

if [ -n "${ARGOS_LANGUAGE_PAIR:-}" ]; then
    IFS=':' read -r FROM_LANG TO_LANG <<<"$ARGOS_LANGUAGE_PAIR"
fi

if [ "$MODEL_INSTALLED" = false ] && [ -n "$FROM_LANG" ] && [ -n "$TO_LANG" ]; then
    echo "Downloading Argos Translate model from $FROM_LANG to $TO_LANG"
    python3 -m argostranslate.cli download --from "$FROM_LANG" --to "$TO_LANG"
fi
