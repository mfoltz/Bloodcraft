#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
CHECK_TAG=""

usage() {
  cat <<'USAGE'
Usage: .codex/scripts/version-metadata.sh [--check-tag <tag>]

Extracts and validates the canonical repository version metadata.
USAGE
}

while [ "$#" -gt 0 ]; do
  case "$1" in
    --check-tag)
      if [ "$#" -lt 2 ]; then
        echo "Error: --check-tag requires a tag value." >&2
        exit 1
      fi
      CHECK_TAG="$2"
      shift 2
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      echo "Error: Unknown argument '$1'." >&2
      usage >&2
      exit 1
      ;;
  esac
done

version_output="$(python3 - <<'PY' "$REPO_ROOT" "$CHECK_TAG"
import pathlib
import re
import sys
import xml.etree.ElementTree as ET

repo_root = pathlib.Path(sys.argv[1])
check_tag = sys.argv[2]

csproj_path = repo_root / "Bloodcraft.csproj"
toml_path = repo_root / "thunderstore.toml"
changelog_path = repo_root / "CHANGELOG.md"


def fail(message: str) -> None:
    print(message, file=sys.stderr)
    raise SystemExit(1)

try:
    csproj_root = ET.parse(csproj_path).getroot()
except FileNotFoundError:
    fail(f"Error: Missing project file: {csproj_path}")
except ET.ParseError as exc:
    fail(f"Error: Failed to parse {csproj_path.name}: {exc}")

version_text = None
for element in csproj_root.iter():
    if element.tag.endswith("Version") and element.text and element.text.strip():
        version_text = element.text.strip()
        break

if not version_text:
    fail(f"Error: Unable to locate a <Version> value in {csproj_path.name}.")

try:
    thunderstore_text = toml_path.read_text(encoding="utf-8")
except FileNotFoundError:
    fail(f"Error: Missing Thunderstore metadata file: {toml_path}")

package_match = re.search(r'^versionNumber\s*=\s*"([^"]+)"', thunderstore_text, re.MULTILINE)
if not package_match:
    fail(f"Error: Unable to locate package.versionNumber in {toml_path.name}.")

package_version = package_match.group(1).strip()
if not package_version:
    fail(f"Error: package.versionNumber in {toml_path.name} is empty.")

try:
    changelog_text = changelog_path.read_text(encoding="utf-8-sig")
except FileNotFoundError:
    fail(f"Error: Missing changelog file: {changelog_path}")

match = re.search(r"^`([^`]+)`", changelog_text, re.MULTILINE)
if not match:
    fail(f"Error: Unable to locate the first version heading in {changelog_path.name}.")

changelog_version = match.group(1).strip()
if not changelog_version:
    fail(f"Error: The first version heading in {changelog_path.name} is empty.")

if package_version != version_text:
    fail(
        f"Error: thunderstore.toml package.versionNumber '{package_version}' does not match "
        f"Bloodcraft.csproj version '{version_text}'."
    )

if changelog_version != version_text:
    fail(
        f"Error: CHANGELOG.md first version heading '{changelog_version}' does not match "
        f"Bloodcraft.csproj version '{version_text}'."
    )

normalized_tag = ""
if check_tag:
    normalized_tag = check_tag
    if normalized_tag.startswith("refs/tags/"):
        normalized_tag = normalized_tag[len("refs/tags/"):]
    if normalized_tag.startswith("v"):
        normalized_tag = normalized_tag[1:]
    normalized_tag = normalized_tag.split("-", 1)[0]
    if normalized_tag != version_text:
        fail(
            f"Error: Release tag '{check_tag}' resolves to version '{normalized_tag}', "
            f"which does not match canonical version '{version_text}'."
        )

print(f"version={version_text}")
print(f"csproj_version={version_text}")
print(f"thunderstore_version={package_version}")
print(f"changelog_version={changelog_version}")
if check_tag:
    print(f"tag={check_tag}")
    print(f"tag_version={normalized_tag}")
PY
)"

if [ -z "$version_output" ]; then
  echo "Error: Version metadata helper produced no output." >&2
  exit 1
fi

mapfile -t version_lines <<< "$version_output"

if ! printf '%s\n' "${version_lines[@]}" | grep -q '^version='; then
  echo "Error: Version metadata helper did not emit a version= entry." >&2
  exit 1
fi

printf '%s\n' "${version_lines[@]}"

if [ -n "${GITHUB_OUTPUT:-}" ]; then
  printf '%s\n' "${version_lines[@]}" >> "$GITHUB_OUTPUT"
fi
