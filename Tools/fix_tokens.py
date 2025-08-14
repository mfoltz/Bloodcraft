#!/usr/bin/env python3
"""Replace placeholder tokens in localization files with English originals."""

import argparse
import json
import re
from pathlib import Path
from typing import Dict, List, Tuple

TOKEN_PLACEHOLDER = re.compile(r"\[\[[^\]]+\]\]")
TOKEN_PATTERN = re.compile(r"<[^>]+>|\{[^{}]+\}|\$\{[^{}]+\}")


def extract_tokens(text: str) -> List[str]:
    return TOKEN_PATTERN.findall(text or "")


def replace_placeholders(value: str, tokens: List[str]) -> Tuple[str, bool, bool]:
    placeholders = TOKEN_PLACEHOLDER.findall(value)
    if not placeholders:
        return value, False, False
    mismatch = len(placeholders) != len(tokens)
    for ph, token in zip(placeholders, tokens):
        value = value.replace(ph, token, 1)
    return value, True, mismatch


def reorder_tokens_in_text(value: str, tokens: List[str]) -> Tuple[str, bool]:
    found = TOKEN_PATTERN.findall(value or "")
    if len(found) != len(tokens):
        return value, False
    if found == tokens:
        return value, False
    token_iter = iter(tokens)
    def repl(_m: re.Match) -> str:
        return next(token_iter)
    return TOKEN_PATTERN.sub(repl, value, count=len(tokens)), True


def load_english_messages(root: Path) -> Dict[str, List[str]]:
    path = root / "Resources" / "Localization" / "Messages" / "English.json"
    with open(path, "r", encoding="utf-8") as f:
        english = json.load(f)["Messages"]
    return {k: extract_tokens(v) for k, v in english.items()}


def load_english_nodes(root: Path) -> Dict[str, List[str]]:
    path = root / "Resources" / "Localization" / "English.json"
    with open(path, "r", encoding="utf-8") as f:
        data = json.load(f)
    nodes = data.get("nodes") or data.get("Nodes") or []
    mapping: Dict[str, List[str]] = {}
    for node in nodes:
        guid = node.get("guid") or node.get("Guid")
        text = node.get("text") or node.get("Text")
        if guid and isinstance(text, str):
            mapping[guid] = extract_tokens(text)
    return mapping


def process_messages_file(
    path: Path, baseline: Dict[str, List[str]], check_only: bool, reorder: bool
) -> bool:
    with open(path, "r", encoding="utf-8") as f:
        data = json.load(f)
    messages = data.get("Messages", {})
    changed = False
    mismatch = False
    for key, text in list(messages.items()):
        new_text, replaced, bad = replace_placeholders(text, baseline.get(key, []))
        reordered = False
        if reorder and not bad:
            new_text, reordered = reorder_tokens_in_text(new_text, baseline.get(key, []))
        if replaced or reordered:
            if bad:
                print(f"{path}: {key} token count mismatch")
            elif replaced and reordered:
                print(f"{path}: {key} tokens restored and reordered")
            elif replaced:
                print(f"{path}: {key} tokens restored")
            else:
                print(f"{path}: {key} tokens reordered")
            if not check_only:
                messages[key] = new_text
            changed = True
            mismatch = mismatch or bad
    if changed and not check_only:
        with open(path, "w", encoding="utf-8") as f:
            json.dump(data, f, indent=2, ensure_ascii=False)
    return changed or mismatch


def process_root_file(
    path: Path, baseline: Dict[str, List[str]], check_only: bool, reorder: bool
) -> bool:
    with open(path, "r", encoding="utf-8") as f:
        data = json.load(f)
    nodes = data.get("nodes") or data.get("Nodes") or []
    changed = False
    mismatch = False
    for node in nodes:
        guid = node.get("guid") or node.get("Guid")
        text_key = "text" if "text" in node else "Text" if "Text" in node else None
        if not guid or not text_key:
            continue
        text = node[text_key]
        if not isinstance(text, str):
            continue
        new_text, replaced, bad = replace_placeholders(text, baseline.get(guid, []))
        reordered = False
        if reorder and not bad:
            new_text, reordered = reorder_tokens_in_text(new_text, baseline.get(guid, []))
        if replaced or reordered:
            if bad:
                print(f"{path}: {guid} token count mismatch")
            elif replaced and reordered:
                print(f"{path}: {guid} tokens restored and reordered")
            elif replaced:
                print(f"{path}: {guid} tokens restored")
            else:
                print(f"{path}: {guid} tokens reordered")
            if not check_only:
                node[text_key] = new_text
            changed = True
            mismatch = mismatch or bad
    if changed and not check_only:
        with open(path, "w", encoding="utf-8") as f:
            json.dump(data, f, indent=2, ensure_ascii=False)
    return changed or mismatch


def main() -> None:
    ap = argparse.ArgumentParser(description="Fix token placeholders in localization JSON files")
    ap.add_argument("--root", default=Path(__file__).resolve().parents[1], help="Repo root")
    ap.add_argument("--check-only", action="store_true", help="Report issues without modifying files")
    ap.add_argument("--reorder", action="store_true", help="Reorder tokens when counts match")
    ap.add_argument("paths", nargs="*", help="Specific localization JSON files to process")
    args = ap.parse_args()

    root = Path(args.root).resolve()
    messages_tokens = load_english_messages(root)
    node_tokens = load_english_nodes(root)

    if args.paths:
        files = []
        for p in args.paths:
            path = Path(p)
            if not path.is_absolute():
                path = (root / path).resolve()
            files.append(path)
    else:
        files = list((root / "Resources" / "Localization" / "Messages").glob("*.json"))
        files = [p for p in files if p.name != "English.json"]
        files += [p for p in (root / "Resources" / "Localization").glob("*.json") if p.name != "English.json"]

    issues = False
    for path in files:
        if path.parent.name == "Messages":
            issues |= process_messages_file(path, messages_tokens, args.check_only, args.reorder)
        else:
            issues |= process_root_file(path, node_tokens, args.check_only, args.reorder)

    if args.check_only and issues:
        raise SystemExit(1)


if __name__ == "__main__":
    main()
