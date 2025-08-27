#!/usr/bin/env python3
"""Validate a translation run directory.

Counts ``TRANSLATED``/``SKIPPED`` entries in ``translate.log`` and
groups skip reasons from that log alongside categories from
``skipped.csv``. Exits with a non-zero status when any
``token_mismatch`` or ``sentinel`` issues remain so CI can fail fast.
"""

from __future__ import annotations

import argparse
import csv
import json
import re
import sys
from collections import Counter
from pathlib import Path

LOG_RE = re.compile(r": (TRANSLATED|SKIPPED)(?: \(([^)]+)\))?")

def summarize_log(path: Path) -> tuple[int, int, Counter[str]]:
    """Return counts for translations and skip reasons from ``path``."""

    translated = skipped = 0
    reasons: Counter[str] = Counter()

    try:
        with path.open(encoding="utf-8") as fp:
            for line in fp:
                m = LOG_RE.search(line)
                if not m:
                    continue
                status, reason = m.group(1), (m.group(2) or "")
                if status == "TRANSLATED":
                    translated += 1
                else:
                    skipped += 1
                    cleaned = reason.strip().lower().replace(" ", "_")
                    if cleaned:
                        reasons[cleaned] += 1
    except FileNotFoundError:
        pass
    return translated, skipped, reasons

def summarize_skipped(path: Path) -> Counter[str]:
    """Return category counts from `path` if it exists."""
    counts: Counter[str] = Counter()
    try:
        with path.open(encoding="utf-8") as fp:
            reader = csv.DictReader(fp)
            for row in reader:
                cat = (row.get("category") or "unknown").strip() or "unknown"
                counts[cat] += 1
    except FileNotFoundError:
        pass
    return counts

def main() -> None:
    ap = argparse.ArgumentParser(description="Validate translation run output")
    ap.add_argument(
        "--run-dir",
        required=True,
        help="Directory containing translate.log and skipped.csv",
    )
    args = ap.parse_args()

    run_dir = Path(args.run_dir)
    translated, skipped, log_reasons = summarize_log(run_dir / "translate.log")
    skip_counts = summarize_skipped(run_dir / "skipped.csv")
    mismatch_path = run_dir / "token_mismatches.json"
    mismatch_count = 0
    try:
        data = json.loads(mismatch_path.read_text(encoding="utf-8"))
        if isinstance(data, list):
            mismatch_count = len(data)
    except FileNotFoundError:
        pass

    print(f"Log results: {translated} TRANSLATED, {skipped} SKIPPED")
    if log_reasons:
        print("Skip reasons:")
        for reason, count in sorted(log_reasons.items()):
            print(f"  {reason}: {count}")
    if skip_counts:
        print("Skip report:")
        for category, count in sorted(skip_counts.items()):
            print(f"  {category}: {count}")
    if mismatch_path.exists():
        print(f"Token mismatch report: {mismatch_count} entries")

    def has_mismatch(counter: Counter[str]) -> bool:
        return any("token_mismatch" in k or "sentinel" in k for k in counter)

    exit_code = 1 if mismatch_count or has_mismatch(log_reasons) or has_mismatch(skip_counts) else 0

    sys.exit(exit_code)

if __name__ == "__main__":
    main()
