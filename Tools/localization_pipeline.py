#!/usr/bin/env python3
"""Automate the localization workflow.

Runs message generation, propagates hashes, translates, fixes tokens,
and verifies translations. Optionally limit the run to specific languages
by passing their names as arguments (e.g. ``python Tools/localization_pipeline.py French German``).
If no languages are provided, all available languages are processed.
"""

from __future__ import annotations

import argparse
import json
import subprocess
import sys
from pathlib import Path
from typing import Dict
import csv
from collections import Counter

ROOT = Path(__file__).resolve().parents[1]
MESSAGES_DIR = ROOT / "Resources" / "Localization" / "Messages"
ENGLISH_PATH = MESSAGES_DIR / "English.json"

# Mapping of message file names to Argos Translate codes
LANGUAGE_CODES: Dict[str, str] = {
    "Brazilian": "pb",
    "French": "fr",
    "German": "de",
    "Hungarian": "hu",
    "Italian": "it",
    "Japanese": "ja",
    "Korean": "ko",
    "Latam": "es",
    "Polish": "pl",
    "Russian": "ru",
    "SChinese": "zh",
    "Spanish": "es",
    "TChinese": "zt",
    "Thai": "th",
    "Turkish": "tr",
    "Ukrainian": "uk",
    "Vietnamese": "vi",
}


def run(cmd: list[str], *, check: bool = True) -> subprocess.CompletedProcess:
    """Run a subprocess, returning the completed process."""
    print("+", " ".join(str(c) for c in cmd))
    return subprocess.run(cmd, check=check, cwd=ROOT)


def propagate_hashes(target: Path) -> None:
    """Copy new hashes from English into ``target`` while preserving translations."""
    with ENGLISH_PATH.open("r", encoding="utf-8") as f:
        english = json.load(f)["Messages"]

    if target.exists():
        with target.open("r", encoding="utf-8") as f:
            data = json.load(f)
    else:
        data = {"Messages": {}}

    messages = data.get("Messages", {})
    merged = {key: messages.get(key, text) for key, text in english.items()}

    if merged != messages:
        data["Messages"] = merged
        with target.open("w", encoding="utf-8") as f:
            json.dump(data, f, indent=2, ensure_ascii=False)


def main() -> None:
    ap = argparse.ArgumentParser(
        description="Run the full localization pipeline",
        epilog="Languages default to all available message files",
    )
    ap.add_argument(
        "languages",
        nargs="*",
        help="Languages to process (e.g. French German). Default: all",
    )
    args = ap.parse_args()

    available = {p.stem: p for p in MESSAGES_DIR.glob("*.json") if p.name != "English.json"}
    if args.languages:
        targets = {}
        for name in args.languages:
            if name not in available:
                raise SystemExit(f"Unknown language: {name}")
            targets[name] = available[name]
    else:
        targets = available

    # Step 1: regenerate English messages
    run(["dotnet", "run", "--project", "Bloodcraft.csproj", "-p:RunGenerateREADME=false", "--", "generate-messages"])

    # Step 2: propagate new hashes
    for path in targets.values():
        propagate_hashes(path)

    # Step 3-4: translate and fix tokens per language
    failure_counts: Counter[str] = Counter()
    overall_ok = True
    for name, path in targets.items():
        code = LANGUAGE_CODES.get(name)
        if not code:
            print(f"Skipping {name}: no translation code configured")
            continue
        result = run(
            [
                sys.executable,
                "Tools/translate_argos.py",
                str(path.relative_to(ROOT)),
                "--to",
                code,
                "--batch-size",
                "100",
                "--max-retries",
                "3",
                "--verbose",
                "--log-file",
                "translate.log",
                "--report-file",
                "skipped.csv",
                "--overwrite",
            ],
            check=False,
        )
        report = ROOT / "skipped.csv"
        if report.is_file():
            with report.open("r", encoding="utf-8") as fp:
                reader = csv.DictReader(fp)
                for row in reader:
                    failure_counts[row.get("reason", "")] += 1
            report.unlink()
        overall_ok &= result.returncode == 0
        run([sys.executable, "Tools/fix_tokens.py", str(path.relative_to(ROOT))])

    if failure_counts:
        print("Skipped translation summary:")
        for reason, count in failure_counts.most_common():
            print(f"  {reason}: {count}")
        raise SystemExit(1)
    if not overall_ok:
        raise SystemExit(1)

    # Step 5: verify translations
    run(["dotnet", "run", "--project", "Bloodcraft.csproj", "-p:RunGenerateREADME=false", "--", "check-translations"])


if __name__ == "__main__":
    main()
