#!/usr/bin/env python3
"""Summarize translation log failures.

This script reads ``translate_metrics.json`` files under ``translations`` and
``skipped.csv`` from the repository root and prints counts of failures grouped
by category. Metrics entries now include additional metadata such as
``run_id``, ``commit``, ``model_version``, and ``cli_args`` which are emitted at debug level.
The script exits with a non-zero status if any token mismatches or
placeholder-only failures remain so CI systems can fail fast. Categories
include ``english``, ``identical``, ``sentinel``, ``token_mismatch``, and
``placeholder``.
"""

from __future__ import annotations

import argparse
import csv
import json
import logging
import sys
from collections import Counter
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
SKIPPED_PATH = ROOT / "skipped.csv"
logger = logging.getLogger(__name__)


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
        failures = entry.get("failures", {})
        if failures:
            logger.debug(
                "Run %s commit %s model %s args %s had %d failures",
                entry.get("run_id"),
                entry.get("commit"),
                entry.get("model_version", entry.get("argos_version")),
                entry.get("cli_args"),
                len(failures),
            )
        for reason in failures.values():
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
    ap = argparse.ArgumentParser(description="Summarize translation log failures")
    ap.add_argument(
        "--log-level",
        default="INFO",
        help="Logging level (default: INFO)",
    )
    args = ap.parse_args()

    logging.basicConfig(
        level=getattr(logging, args.log_level.upper(), logging.INFO),
        format="%(asctime)s %(levelname)s %(message)s",
        stream=sys.stdout,
    )

    metric_counts: Counter[str] = Counter()
    for path in (ROOT / "translations").glob("*/*/translate_metrics.json"):
        metric_counts.update(_summarize_metrics(path))
    skipped_counts = _summarize_skipped(SKIPPED_PATH)

    if metric_counts:
        logger.info("Metric failures:")
        for category, count in metric_counts.items():
            logger.info("  %s: %s", category, count)
    if skipped_counts:
        logger.info("Skipped translations:")
        for category, count in skipped_counts.items():
            logger.info("  %s: %s", category, count)

    exit_code = 0
    if metric_counts.get("token_mismatch") or skipped_counts.get("token_mismatch"):
        exit_code = 1
    if skipped_counts.get("placeholder"):
        exit_code = 1

    sys.exit(exit_code)

if __name__ == "__main__":
    main()
