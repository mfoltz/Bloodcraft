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
    ap = argparse.ArgumentParser(
        description=(
            "Validate translation run output and write a token mismatch summary "
            "to token_mismatch_summary.json."
        )
    )
    ap.add_argument(
        "--run-dir",
        required=True,
        help=(
            "Directory containing translate.log and skipped.csv; "
            "token_mismatch_summary.json will be written here"
        ),
    )
    args = ap.parse_args()

    run_dir = Path(args.run_dir)
    translated, skipped, log_reasons = summarize_log(run_dir / "translate.log")
    skip_counts = summarize_skipped(run_dir / "skipped.csv")
    mismatch_path = run_dir / "token_mismatches.json"
    summary_path = run_dir / "token_mismatch_summary.json"
    mismatch_count = 0
    summary_data: dict[str, dict[str, list[str]]] = {}
    try:
        data = json.loads(mismatch_path.read_text(encoding="utf-8"))
        if isinstance(data, list):
            mismatch_count = len(data)
            for entry in data:
                if not isinstance(entry, dict):
                    continue
                key = entry.get("key") or entry.get("hash")
                if not isinstance(key, str):
                    continue
                summary_data[key] = {
                    "missing": entry.get("missing", []),
                    "extra": entry.get("extra", []),
                }
    except FileNotFoundError:
        pass

    with summary_path.open("w", encoding="utf-8") as fp:
        json.dump(summary_data, fp, indent=2, ensure_ascii=False)

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

    metrics_path = run_dir / "metrics.json"
    try:
        metrics = json.loads(metrics_path.read_text(encoding="utf-8"))
    except FileNotFoundError:
        metrics = []
    if metrics:
        entry = metrics[-1] if isinstance(metrics, list) else metrics
        token_reorders = entry.get("token_reorders", 0)
        token_mismatches = entry.get("token_mismatches", 0)
        retry_attempts = entry.get("retry_attempts", 0)
        retry_successes = entry.get("retry_successes", 0)
        print(
            "Metrics summary: "
            f"{token_reorders} token reorders, {token_mismatches} token mismatches, "
            f"{retry_attempts} retry attempts, {retry_successes} retry successes"
        )

    def has_mismatch(counter: Counter[str]) -> bool:
        return any("token_mismatch" in k or "sentinel" in k for k in counter)

    exit_code = 1 if mismatch_count or has_mismatch(log_reasons) or has_mismatch(skip_counts) else 0

    sys.exit(exit_code)

if __name__ == "__main__":
    main()
