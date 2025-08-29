#!/usr/bin/env python3
"""Display skipped translation entries for manual review."""

from __future__ import annotations

import argparse
import csv
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent

def load_messages(path: Path) -> dict:
    with path.open(encoding="utf-8") as fp:
        data = json.load(fp)
    return data.get("Messages", {})

def main() -> None:
    ap = argparse.ArgumentParser(
        description="List skipped hashes and current translations to aid manual fixes",
    )
    ap.add_argument(
        "csv",
        type=Path,
        help="Path to skipped_<Language>.csv",
    )
    ap.add_argument(
        "--language-file",
        type=Path,
        default=ROOT / "Resources/Localization/Messages/Spanish.json",
        help="JSON file containing translations; defaults to Spanish",
    )
    args = ap.parse_args()

    messages = load_messages(args.language_file)

    with args.csv.open(encoding="utf-8") as fp:
        reader = csv.DictReader(fp)
        for row in reader:
            hash_id = row.get("hash", "")
            english = row.get("english", "")
            current = messages.get(hash_id, "")
            print(f"{hash_id}\n  English : {english}\n  Current : {current}\n")

    print(
        "Edit the language file to supply manual Spanish strings, then run:\n"
        f"python Tools/fix_tokens.py {args.language_file}\n"
        f"python Tools/translate_argos.py {args.language_file} --to es --overwrite\n"
        "After translations are updated, verify with:\n"
        "dotnet run --project Bloodcraft.csproj -p:RunGenerateREADME=false -- check-translations",
    )

if __name__ == "__main__":
    main()
