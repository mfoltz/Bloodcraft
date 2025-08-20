#!/usr/bin/env python3
"""Summarize per-hash token mismatches and reorders."""

from __future__ import annotations

import argparse
import csv
import json
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


def _aggregate(entries: List[dict]) -> Dict[str, Dict[str, int | str]]:
    stats: Dict[str, Dict[str, int | str]] = {}
    for entry in entries:
        file = entry.get("file", "")
        failures = entry.get("failures", {})
        for hash_id, info in entry.get("hash_stats", {}).items():
            mismatch = info.get("original_tokens") != info.get("translated_tokens")
            if hash_id in failures:
                mismatch = True
            reordered = bool(info.get("reordered"))
            if mismatch or reordered:
                record = stats.setdefault(hash_id, {"mismatches": 0, "reorders": 0, "file": file})
                if mismatch:
                    record["mismatches"] += 1
                if reordered:
                    record["reorders"] += 1
                record["file"] = file
    return stats


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


def main() -> None:
    ap = argparse.ArgumentParser(description="Summarize token stats from translate_metrics.json")
    ap.add_argument(
        "--metrics-file",
        type=Path,
        default=ROOT / "translate_metrics.json",
        help="Path to translate_metrics.json",
    )
    ap.add_argument(
        "--csv",
        type=Path,
        help="Optional CSV output file",
    )
    ap.add_argument(
        "--top",
        type=int,
        default=0,
        help="Limit results to top N hashes",
    )
    args = ap.parse_args()

    entries = _load_metrics(args.metrics_file)
    stats = _aggregate(entries)
    rows = _sort_rows(stats)
    if args.top and args.top > 0:
        rows = rows[: args.top]

    if args.csv:
        _write_csv(rows, args.csv)
    _print_table(rows)


if __name__ == "__main__":
    main()
