#!/usr/bin/env python3
"""Scan C# files for English strings not using localization helpers.

The script looks for string literals in ``.cs`` files that appear to contain
English words and are not wrapped in ``LocalizationService`` helpers. This helps
contributors spot untranslated messages.
"""
import argparse
import re
from pathlib import Path

from language_utils import contains_english

# Regex to capture C# string literals ("foo" or @"foo") without spanning lines
STRING_RE = re.compile(r'@?"(?:[^"\\]|\\.)+"')

def extract_strings(text: str):
    for match in STRING_RE.finditer(text):
        yield match

def should_skip(path: Path, args) -> bool:
    for d in args.whitelist_dir:
        d_path = Path(d)
        if d_path in path.parents:
            return True
    for pattern in args.whitelist_pattern:
        if re.search(pattern, str(path)):
            return True
    return False

def in_localization_context(text: str, start: int) -> bool:
    context_start = max(0, start - 100)
    context = text[context_start:start]
    return "LocalizationService" in context

def main() -> int:
    parser = argparse.ArgumentParser(description="Scan .cs files for English phrases not wrapped in localization helpers.")
    parser.add_argument("paths", nargs="*", default=["."], help="Paths to scan (default: current directory)")
    parser.add_argument("--whitelist-dir", action="append", default=[], help="Directories to skip (can be used multiple times)")
    parser.add_argument("--whitelist-pattern", action="append", default=[], help="Regex patterns to skip paths")
    args = parser.parse_args()

    for root in args.paths:
        for path in Path(root).rglob("*.cs"):
            if any(part in {"obj", "bin"} for part in path.parts):
                continue
            if should_skip(path, args):
                continue
            text = path.read_text(encoding="utf-8", errors="ignore")
            for match in extract_strings(text):
                literal = match.group(0)
                # strip leading @ and quotes
                stripped = literal.lstrip('@')[1:-1]
                try:
                    unescaped = bytes(stripped, "utf-8").decode("unicode_escape")
                except Exception:
                    unescaped = stripped
                if not contains_english(unescaped):
                    continue
                if in_localization_context(text, match.start()):
                    continue
                line_no = text.count("\n", 0, match.start()) + 1
                print(f"{path}:{line_no}: {unescaped}")
    return 0

if __name__ == "__main__":
    raise SystemExit(main())
