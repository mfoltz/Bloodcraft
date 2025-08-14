#!/usr/bin/env python3
"""Generate a catalog of message keys from English.json.

Regenerates Docs/MessageKeys.md when Resources/Localization/Messages/English.json
is newer. Run this after modifying English.json to keep the catalog current.
"""
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ENGLISH_PATH = ROOT / "Resources" / "Localization" / "Messages" / "English.json"
DOC_PATH = ROOT / "Docs" / "MessageKeys.md"


def summarize(text: str, max_words: int = 8) -> str:
    # Strip color tags and other markup
    text = re.sub(r"<[^>]+>", "", text)
    # Replace placeholders with ellipsis
    text = re.sub(r"\{\d+\}", "…", text)
    words = text.split()
    snippet = " ".join(words[:max_words])
    if len(words) > max_words:
        snippet += "..."
    return snippet


def main() -> int:
    if not ENGLISH_PATH.exists():
        print(f"Missing {ENGLISH_PATH}")
        return 1

    english_mtime = ENGLISH_PATH.stat().st_mtime
    doc_mtime = DOC_PATH.stat().st_mtime if DOC_PATH.exists() else 0
    if english_mtime <= doc_mtime:
        return 0  # Up to date

    data = json.loads(ENGLISH_PATH.read_text())
    messages = data.get("Messages", {})

    lines = [
        "# Message Key Catalog",
        "",
        "Auto-generated from `Resources/Localization/Messages/English.json`.",
        "",
        "| Key | Description |",
        "| --- | --- |",
    ]

    for key in sorted(messages):
        desc = summarize(messages[key])
        desc = desc.replace("|", "\\|")  # escape pipes
        lines.append(f"| `{key}` | {desc} |")

    DOC_PATH.write_text("\n".join(lines) + "\n")
    print(f"Wrote {DOC_PATH}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
