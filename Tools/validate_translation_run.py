#!/usr/bin/env python3
"""Validate a translation run directory.

Counts `TRANSLATED`/`SKIPPED` entries in `translate.log` and
summarises categories from `skipped.csv`. Exits with a non-zero status
if any `token_mismatch` or `sentinel` issues remain so CI can fail
fast.
"""

from __future__ import annotations

import argparse
import csv
import re
import sys
from collections import Counter
from pathlib import Path

LOG_RE = re.compile(r": (TRANSLATED|SKIPPED)(?: \(([^)]+)\))?")

def summarize_log(path: Path) -> tuple[int, int, Counter[str]]:
    """Return translated/skipped counts and issue categories from `path`."""
    translated = skipped = 0
    issues: Counter[str] = Counter()
    try:
        with path.open(encoding="utf-8") as fp:
            for line in fp:
                m = LOG_RE.search(line)
                if not m:
                    continue
                status, reason = m.group(1), (m.group(2) or "").lower()
                if status == "TRANSLATED":
                    translated += 1
                else:
                    skipped += 1
                    if "token mismatch" in reason:
                        issues["token_mismatch"] += 1
                    if "sentinel" in reason:
                        issues["sentinel"] += 1
    except FileNotFoundError:
        pass
    return translated, skipped, issues

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
    translated, skipped, log_issues = summarize_log(run_dir / "translate.log")
    skip_counts = summarize_skipped(run_dir / "skipped.csv")

    print(f"Log results: {translated} TRANSLATED, {skipped} SKIPPED")
    if skip_counts:
        print("Skip report:")
        for category, count in sorted(skip_counts.items()):
            print(f"  {category}: {count}")

    exit_code = 0
    if log_issues.get("token_mismatch") or log_issues.get("sentinel"):
        exit_code = 1
    if skip_counts.get("token_mismatch") or skip_counts.get("sentinel"):
        exit_code = 1

    sys.exit(exit_code)

if __name__ == "__main__":
    main()
