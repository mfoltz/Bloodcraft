#!/usr/bin/env python3
"""Summarize translation log failures.

This script reads ``translate_metrics.json`` and ``skipped.csv`` and prints counts
of failures grouped by category. By default the files are looked up under the
repository root, but ``--run-dir`` can point to a translation run directory.
Paths may also be overridden individually via ``--metrics-file`` and
``--skipped-file``. Metrics entries include metadata such as ``run_id``,
``git_commit``, ``python_version``, ``argos_version``, ``model_version``, ``run_dir``,
``log_file``, ``report_file``, ``metrics_file``, and ``cli_args`` which are emitted
at debug level. The script exits with a non-zero status if any token mismatches
or placeholder-only failures remain so CI systems can fail fast. Categories
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
        if failures or entry.get("error"):
            logger.debug(
                (
                    "Run %s commit %s py %s argos %s model %s dir %s log %s "
                    "report %s metrics %s args %s error %s had %d failures"
                ),
                entry.get("run_id"),
                entry.get("git_commit"),
                entry.get("python_version"),
                entry.get("argos_version"),
                entry.get("model_version"),
                entry.get("run_dir"),
                entry.get("log_file"),
                entry.get("report_file"),
                entry.get("metrics_file"),
                entry.get("cli_args"),
                entry.get("error"),
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
    ap.add_argument(
        "--run-dir",
        help="Translation run directory containing translate_metrics.json and skipped.csv",
    )
    ap.add_argument(
        "--metrics-file",
        help="Path to translate_metrics.json (overrides --run-dir)",
    )
    ap.add_argument(
        "--skipped-file",
        help="Path to skipped.csv (overrides --run-dir)",
    )
    args = ap.parse_args()

    logging.basicConfig(
        level=getattr(logging, args.log_level.upper(), logging.INFO),
        format="%(asctime)s %(levelname)s %(message)s",
        stream=sys.stdout,
    )

    run_dir = Path(args.run_dir) if args.run_dir else None
    metrics_path = Path(args.metrics_file) if args.metrics_file else None
    skipped_path = Path(args.skipped_file) if args.skipped_file else None

    if run_dir:
        if metrics_path is None:
            metrics_path = run_dir / "translate_metrics.json"
        if skipped_path is None:
            skipped_path = run_dir / "skipped.csv"

    if metrics_path is None:
        metrics_path = ROOT / "translate_metrics.json"
    elif not metrics_path.is_absolute():
        metrics_path = ROOT / metrics_path
    metric_counts: Counter[str] = _summarize_metrics(metrics_path)

    if skipped_path is None:
        skipped_path = ROOT / "skipped.csv"
    elif not skipped_path.is_absolute():
        skipped_path = ROOT / skipped_path
    skipped_counts = _summarize_skipped(skipped_path)

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
