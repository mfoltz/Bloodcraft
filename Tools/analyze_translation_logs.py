#!/usr/bin/env python3
"""Summarize translation log failures.

This script reads ``translate_metrics.json`` and ``skipped.csv`` from the
repository root and prints counts of failures grouped by category. It exits
with a non-zero status if any token mismatches or placeholder-only failures
remain so CI systems can fail fast. Categories include ``english``,
``identical``, ``sentinel``, ``token_mismatch``, and ``placeholder``.
"""

from __future__ import annotations

import csv
import json
import sys
from collections import Counter
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
METRICS_PATH = ROOT / "translate_metrics.json"
SKIPPED_PATH = ROOT / "skipped.csv"


def _categorize(reason: str | None) -> str:
    if not reason:
        return "unknown"
    r = reason.lower()
    if "token mismatch" in r:
        return "token_mismatch"
    if "sentinel" in r:
        return "sentinel"
    if "identical" in r:
        return "identical"
    if "placeholder" in r or "tokens only" in r or "token only" in r:
        return "placeholder"
    if "untranslated" in r or "english" in r:
        return "english"
    return r

def _summarize_metrics(path: Path) -> Counter:
    counts: Counter[str] = Counter()
    try:
        entries = json.loads(path.read_text(encoding="utf-8"))
    except FileNotFoundError:
        return counts
    except json.JSONDecodeError:
        return counts
    for entry in entries:
        for reason in entry.get("failures", {}).values():
            counts[_categorize(reason)] += 1
    return counts

def _summarize_skipped(path: Path) -> Counter:
    counts: Counter[str] = Counter()
    try:
        with path.open(encoding="utf-8") as fp:
            reader = csv.DictReader(fp)
            for row in reader:
                cat = row.get("category") or "unknown"
                if cat == "untranslated":
                    cat = "english"
                counts[cat] += 1
    except FileNotFoundError:
        return counts
    return counts

def main() -> None:
    metric_counts = _summarize_metrics(METRICS_PATH)
    skipped_counts = _summarize_skipped(SKIPPED_PATH)

    if metric_counts:
        print("Metric failures:")
        for category, count in metric_counts.items():
            print(f"  {category}: {count}")
    if skipped_counts:
        print("Skipped translations:")
        for category, count in skipped_counts.items():
            print(f"  {category}: {count}")

    exit_code = 0
    if metric_counts.get("token_mismatch") or skipped_counts.get("token_mismatch"):
        exit_code = 1
    if skipped_counts.get("placeholder"):
        exit_code = 1

    sys.exit(exit_code)

if __name__ == "__main__":
    main()
