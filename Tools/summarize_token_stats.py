"""Aggregate translation metrics and token mismatch data.

This script traverses ``translations/<lang>/<run>/`` directories looking for
``metrics.json`` and ``token_mismatch_summary.json`` files.  For each language
it aggregates processed lines, successes, timeouts and token mismatches.  It
also counts how frequently each hash appears in mismatch reports so languages
and hashes can be ranked by problem frequency.

Two CSV files and a JSON report are produced for downstream dashboards or
manual analysis.
"""

from __future__ import annotations

import argparse
import csv
import json
from collections import Counter, defaultdict
from pathlib import Path
from typing import Dict, Iterable, Tuple


ROOT = Path(__file__).resolve().parent.parent


def _read_metrics(path: Path) -> Tuple[int, int, int]:
    """Return processed, successes and timeouts for a metrics file."""

    try:
        data = json.loads(path.read_text(encoding="utf-8"))
    except (FileNotFoundError, json.JSONDecodeError):
        return 0, 0, 0

    processed = sum(entry.get("processed", 0) for entry in data)
    successes = sum(entry.get("successes", 0) for entry in data)
    timeouts = sum(entry.get("timeouts", 0) for entry in data)
    return processed, successes, timeouts


def _read_mismatch_summary(path: Path) -> Tuple[int, Iterable[str]]:
    """Return total mismatch count and iterable of hash ids."""

    try:
        data = json.loads(path.read_text(encoding="utf-8"))
    except (FileNotFoundError, json.JSONDecodeError):
        return 0, []

    total = 0
    hashes: list[str] = []

    if isinstance(data, dict):
        # ``token_mismatch_summary.json`` has appeared in a few different
        # shapes.  Earlier versions keyed the file by hash with the value being
        # the mismatch details, while later versions stored statistics such as
        # ``token_mismatches`` alongside a ``mismatches`` list.  Support both
        # layouts so older archives remain readable.
        if "token_mismatches" in data or "mismatches" in data:
            total += int(data.get("token_mismatches", 0))
            for mismatch in data.get("mismatches", []) or []:
                key = mismatch.get("key") or mismatch.get("hash")
                if key is not None:
                    hashes.append(str(key))
        else:
            total = len(data)
            hashes.extend(str(k) for k in data.keys())
    else:
        # Newer summaries may be a list of report entries.  Each entry contains
        # aggregate fields as above.
        for entry in data:
            if not isinstance(entry, dict):
                continue
            total += int(entry.get("token_mismatches", 0))
            for mismatch in entry.get("mismatches", []) or []:
                key = mismatch.get("key") or mismatch.get("hash")
                if key is not None:
                    hashes.append(str(key))
    return total, hashes


def collect_stats(translations_root: Path) -> Tuple[Dict[str, Dict[str, float]], Counter[str]]:
    """Gather language stats and hash mismatch counts."""

    language_stats: Dict[str, Dict[str, float]] = defaultdict(
        lambda: {"processed": 0, "successes": 0, "timeouts": 0, "token_mismatches": 0}
    )
    hash_counter: Counter[str] = Counter()

    for lang_dir in translations_root.iterdir():
        if not lang_dir.is_dir():
            continue
        lang = lang_dir.name
        for run_dir in lang_dir.iterdir():
            if not run_dir.is_dir():
                continue
            processed, successes, timeouts = _read_metrics(run_dir / "metrics.json")
            mismatches, mismatch_hashes = _read_mismatch_summary(
                run_dir / "token_mismatch_summary.json"
            )
            stats = language_stats[lang]
            stats["processed"] += processed
            stats["successes"] += successes
            stats["timeouts"] += timeouts
            stats["token_mismatches"] += mismatches
            for hash_id in mismatch_hashes:
                hash_counter[hash_id] += 1

    for stats in language_stats.values():
        processed = stats.get("processed", 0)
        stats["success_rate"] = (stats["successes"] / processed) if processed else 0.0

    return language_stats, hash_counter


def _write_language_csv(rows: Iterable[Tuple[str, Dict[str, float]]], path: Path) -> None:
    with path.open("w", newline="", encoding="utf-8") as fp:
        writer = csv.writer(fp)
        writer.writerow(
            ["language", "processed", "successes", "timeouts", "token_mismatches", "success_rate"]
        )
        for lang, stats in rows:
            writer.writerow(
                [
                    lang,
                    int(stats["processed"]),
                    int(stats["successes"]),
                    int(stats["timeouts"]),
                    int(stats["token_mismatches"]),
                    f"{stats['success_rate']:.4f}",
                ]
            )


def _write_hash_csv(rows: Iterable[Tuple[str, int]], path: Path) -> None:
    with path.open("w", newline="", encoding="utf-8") as fp:
        writer = csv.writer(fp)
        writer.writerow(["hash", "mismatch_count"])
        for hash_id, count in rows:
            writer.writerow([hash_id, count])


def _write_json(
    language_rows: Iterable[Tuple[str, Dict[str, float]]], hash_rows: Iterable[Tuple[str, int]], path: Path
) -> None:
    out = {
        "languages": [
            {
                "language": lang,
                **stats,
            }
            for lang, stats in language_rows
        ],
        "hashes": [
            {"hash": hash_id, "mismatch_count": count} for hash_id, count in hash_rows
        ],
    }
    path.write_text(json.dumps(out, indent=2), encoding="utf-8")


def main() -> None:
    ap = argparse.ArgumentParser(description="Summarize token mismatch data")
    ap.add_argument(
        "--translations-root",
        type=Path,
        default=ROOT / "translations",
        help="Root directory containing language run folders",
    )
    ap.add_argument("--json", type=Path, help="Optional JSON output file")
    ap.add_argument("--languages-csv", type=Path, help="Optional language CSV output")
    ap.add_argument("--hashes-csv", type=Path, help="Optional hash CSV output")
    args = ap.parse_args()

    language_stats, hash_counter = collect_stats(args.translations_root)
    language_rows = sorted(
        language_stats.items(), key=lambda kv: kv[1]["token_mismatches"], reverse=True
    )
    hash_rows = hash_counter.most_common()

    if args.languages_csv:
        _write_language_csv(language_rows, args.languages_csv)
    if args.hashes_csv:
        _write_hash_csv(hash_rows, args.hashes_csv)
    if args.json:
        _write_json(language_rows, hash_rows, args.json)


if __name__ == "__main__":
    main()

