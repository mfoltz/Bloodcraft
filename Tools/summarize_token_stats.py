#!/usr/bin/env python3
"""Summarize per-hash token mismatches and reorders.

The metrics file may include run metadata (e.g., ``run_id``, ``git_commit``,
``python_version``) and paths to log, report, and metrics files. These fields are
ignored; only ``file``, ``failures``, and ``hash_stats`` are used."""

from __future__ import annotations

import argparse
import csv
import json
from collections import Counter
from pathlib import Path
from typing import Dict, List, Tuple

ROOT = Path(__file__).resolve().parent.parent


def _load_metrics(path: Path) -> List[dict]:
    try:
        with path.open(encoding="utf-8") as fp:
            return json.load(fp)
    except FileNotFoundError:
        return []
    except json.JSONDecodeError:
        return []


def _aggregate(entries: List[dict]) -> Tuple[Dict[str, Dict[str, int | str]], Dict[str, int]]:
    stats: Dict[str, Dict[str, int | str]] = {}
    token_counts: Counter[str] = Counter()
    for entry in entries:
        file = entry.get("file", "")
        failures = entry.get("failures", {})
        mismatch_details = entry.get("token_mismatch_details", {})
        for hash_id, info in entry.get("hash_stats", {}).items():
            mismatch = info.get("original_tokens") != info.get("translated_tokens")
            if hash_id in failures:
                mismatch = True
            details = mismatch_details.get(hash_id, {})
            missing = details.get("missing") or info.get("missing") or []
            extra = details.get("extra") or info.get("extra") or []
            if missing or extra:
                mismatch = True
            reordered = bool(info.get("reordered"))
            if mismatch or reordered:
                record = stats.setdefault(hash_id, {"mismatches": 0, "reorders": 0, "file": file})
                if mismatch:
                    record["mismatches"] += 1
                if reordered:
                    record["reorders"] += 1
                record["file"] = file
            for token in missing:
                token_counts[str(token)] += 1
    return stats, dict(token_counts)


def _sort_rows(stats: Dict[str, Dict[str, int | str]]) -> List[Tuple[str, int, int, str]]:
    rows = [
        (hash_id, data["mismatches"], data["reorders"], str(data.get("file", "")))
        for hash_id, data in stats.items()
    ]
    rows.sort(key=lambda r: (r[1] + r[2], r[0]), reverse=True)
    return rows


def _print_table(rows: List[Tuple[str, int, int, str]]) -> None:
    if not rows:
        return
    header = ("hash", "mismatches", "reorders", "file")
    widths = [max(len(str(x)) for x in col) for col in zip(*(rows + [header]))]
    fmt = "{:<%d} {:>%d} {:>%d} {:<%d}" % tuple(widths)
    print(fmt.format(*header))
    for row in rows:
        print(fmt.format(*row))


def _write_csv(rows: List[Tuple[str, int, int, str]], path: Path) -> None:
    with path.open("w", newline="", encoding="utf-8") as fp:
        writer = csv.writer(fp)
        writer.writerow(["hash", "mismatches", "reorders", "file"])
        for row in rows:
            writer.writerow(row)


def _sort_token_rows(token_counts: Dict[str, int]) -> List[Tuple[str, int]]:
    rows = sorted(token_counts.items(), key=lambda r: (-r[1], r[0]))
    return rows


def _print_token_table(rows: List[Tuple[str, int]]) -> None:
    if not rows:
        return
    header = ("token", "count")
    widths = [max(len(str(x)) for x in col) for col in zip(*(rows + [header]))]
    fmt = "{:<%d} {:>%d}" % tuple(widths)
    print(fmt.format(*header))
    for row in rows:
        print(fmt.format(*row))


def _write_token_csv(rows: List[Tuple[str, int]], path: Path) -> None:
    with path.open("w", newline="", encoding="utf-8") as fp:
        writer = csv.writer(fp)
        writer.writerow(["token", "count"])
        for row in rows:
            writer.writerow(row)


def main() -> None:
    ap = argparse.ArgumentParser(description="Summarize token stats from metrics.json")
    ap.add_argument(
        "--run-dir",
        type=Path,
        help="Translation run directory containing metrics.json",
    )
    ap.add_argument(
        "--metrics-file",
        type=Path,
        help="Path to metrics.json (overrides --run-dir)",
    )
    ap.add_argument(
        "--csv",
        type=Path,
        help="Optional CSV output file",
    )
    ap.add_argument(
        "--token-csv",
        type=Path,
        help="Optional CSV output for top missing tokens",
    )
    ap.add_argument(
        "--top",
        type=int,
        default=0,
        help="Limit results to top N hashes",
    )
    args = ap.parse_args()

    metrics_path = args.metrics_file
    if args.run_dir and metrics_path is None:
        metrics_path = args.run_dir / "metrics.json"
    if metrics_path is None:
        metrics_path = ROOT / "metrics.json"
    entries = _load_metrics(metrics_path)
    stats, token_counts = _aggregate(entries)
    rows = _sort_rows(stats)
    token_rows = _sort_token_rows(token_counts)
    if args.top and args.top > 0:
        rows = rows[: args.top]
        token_rows = token_rows[: args.top]

    if args.csv:
        _write_csv(rows, args.csv)
    if args.token_csv:
        _write_token_csv(token_rows, args.token_csv)
    if token_rows:
        print("Top missing tokens:")
        _print_token_table(token_rows)
        print()
    _print_table(rows)


if __name__ == "__main__":
    main()
