#!/usr/bin/env python3
"""Generate term glossaries from official localization files."""

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
    """Return the likely term from the start of a sentence."""
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


def build_glossary(terms_path: Path) -> Dict[str, Dict[str, Any]]:
    terms = [line.strip() for line in terms_path.read_text(encoding="utf-8").splitlines() if line.strip()]

    native_en = list(nodes(load_json(NATIVE_DIR / "English.json")))
    messages_en = load_json(MESSAGES_DIR / "English.json")["Messages"]

    glossary: Dict[str, Dict[str, Any]] = {}

    for term in terms:
        entry = next((e for e in native_en if entry_text(e) == term), None)
        if entry:
            key = entry_guid(entry)
            translations = {}
            for lang_file in NATIVE_DIR.glob("*.json"):
                data = list(nodes(load_json(lang_file)))
                t = next((entry_text(e) for e in data if entry_guid(e) == key), None)
                translations[lang_file.stem] = t
            glossary[term] = {"source": "Native", "key": key, "translations": translations}
            continue

        pattern = re.compile(rf"^{re.escape(term)}\b")
        key = None
        for k, v in messages_en.items():
            if pattern.search(v):
                key = k
                break
        if not key:
            print(f"Warning: term '{term}' not found", file=sys.stderr)
            continue

        translations = {}
        for lang_file in MESSAGES_DIR.glob("*.json"):
            data = load_json(lang_file)["Messages"]
            value = data.get(key)
            if value is None:
                continue
            translations[lang_file.stem] = extract_term_from_sentence(value)
        glossary[term] = {"source": "Messages", "key": key, "translations": translations}

    return glossary


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--terms",
        type=Path,
        default=ROOT / "Resources" / "Localization" / "glossary_terms.txt",
        help="Path to glossary terms list",
    )
    parser.add_argument(
        "--output",
        type=Path,
        default=ROOT / "Resources" / "Localization" / "glossary.json",
        help="Output glossary file",
    )
    args = parser.parse_args()

    glossary = build_glossary(args.terms)
    with args.output.open("w", encoding="utf-8") as fp:
        json.dump(glossary, fp, ensure_ascii=False, indent=2, sort_keys=True)


if __name__ == "__main__":
    main()
