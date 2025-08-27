#!/usr/bin/env python3
"""Summarize skipped translation categories."""

from __future__ import annotations

import argparse
import csv
import json
import sys
from collections import Counter
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent


def summarize_report(path: Path) -> Counter[str]:
    """Return counts grouped by `category` from `path`."""
    counts: Counter[str] = Counter()
    with path.open(encoding="utf-8") as fp:
        reader = csv.DictReader(fp)
        for row in reader:
            cat = (row.get("category") or "unknown").strip() or "unknown"
            counts[cat] += 1
    return counts


def summarize_mismatches(path: Path) -> Counter[str]:
    """Return counts of mismatch patterns from `path` if it exists."""
    counts: Counter[str] = Counter()
    try:
        data = json.loads(path.read_text(encoding="utf-8"))
    except FileNotFoundError:
        return counts
    for entry in data:
        missing = ",".join(sorted(entry.get("missing") or [])) or "-"
        extra = ",".join(sorted(entry.get("extra") or [])) or "-"
        key = f"missing:{missing}|extra:{extra}"
        counts[key] += 1
    return counts


def main() -> None:
    ap = argparse.ArgumentParser(
        description="Group skipped rows by category and print counts",
    )
    ap.add_argument("report", type=Path, help="Path to skipped.csv report")
    ap.add_argument(
        "--mismatches",
        type=Path,
        help="Path to token_mismatches.json (defaults to sibling of report)",
    )
    args = ap.parse_args()

    report_path = args.report
    if not report_path.is_absolute():
        report_path = ROOT / report_path

    try:
        counts = summarize_report(report_path)
    except FileNotFoundError:
        print(f"Report not found: {report_path}", file=sys.stderr)
        sys.exit(1)

    mismatch_path = args.mismatches or report_path.with_name("token_mismatches.json")
    mismatch_counts = summarize_mismatches(mismatch_path)

    for category, count in sorted(counts.items()):
        print(f"{category}: {count}")
    if mismatch_counts:
        print("Token mismatch patterns:")
        for pattern, count in sorted(mismatch_counts.items()):
            print(f"  {pattern}: {count}")

    sys.exit(0)


if __name__ == "__main__":
    main()
