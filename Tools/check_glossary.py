#!/usr/bin/env python3
"""Verify translations match the compiled glossary."""

from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path
from typing import Any, Dict, Iterable

ROOT = Path(__file__).resolve().parent.parent
NATIVE_DIR = ROOT / "Resources" / "Localization" / "Native"
MESSAGES_DIR = ROOT / "Resources" / "Localization" / "Messages"


def load_json(path: Path) -> Any:
    with path.open(encoding="utf-8") as fp:
        return json.load(fp)


def nodes(data: Dict[str, Any]) -> Iterable[Dict[str, Any]]:
    return data.get("Nodes") or data.get("nodes") or []


def entry_guid(entry: Dict[str, Any]) -> str:
    return entry.get("Guid") or entry.get("guid")


def entry_text(entry: Dict[str, Any]) -> str:
    return entry.get("Text") or entry.get("text")


def extract_term_from_sentence(text: str) -> str:
    cleaned = re.sub(r"<[^>]+>", "", text).strip()
    tokens = cleaned.split()
    articles = {
        "a",
        "an",
        "the",
        "o",
        "a",
        "os",
        "as",
        "el",
        "la",
        "los",
        "las",
        "le",
        "les",
        "um",
        "uma",
        "un",
        "une",
    }
    while tokens and tokens[0].lower().strip(",:;.!?") in articles:
        tokens.pop(0)
    return tokens[0].strip(",:;.!?") if tokens else text


def check_glossary(glossary_path: Path) -> list[str]:
    glossary = load_json(glossary_path)
    errors: list[str] = []

    for term, info in glossary.items():
        source = info["source"]
        key = info["key"]
        for lang, expected in info["translations"].items():
            if source == "Native":
                path = NATIVE_DIR / f"{lang}.json"
                data = list(nodes(load_json(path)))
                text_val = next((entry_text(e) for e in data if entry_guid(e) == key), None)
                if text_val != expected:
                    errors.append(f"{term} [{lang}] expected '{expected}' but found '{text_val}'")
            else:
                path = MESSAGES_DIR / f"{lang}.json"
                data = load_json(path)["Messages"]
                text_val = data.get(key)
                found = extract_term_from_sentence(text_val) if text_val else None
                if found != expected:
                    errors.append(f"{term} [{lang}] expected '{expected}' but found '{found}'")
    return errors


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "glossary",
        type=Path,
        nargs="?",
        default=ROOT / "Resources" / "Localization" / "glossary.json",
        help="Path to glossary JSON",
    )
    args = parser.parse_args()

    errors = check_glossary(args.glossary)
    if errors:
        for err in errors:
            print(err, file=sys.stderr)
        raise SystemExit(1)
    print("Glossary check passed")


if __name__ == "__main__":
    main()
