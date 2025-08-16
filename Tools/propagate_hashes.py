#!/usr/bin/env python3
"""Propagate message hashes from English into target localization files."""

import argparse
import json
from pathlib import Path
from typing import Dict, Tuple


def load_messages(path: Path) -> Dict[str, str]:
    with open(path, "r", encoding="utf-8") as f:
        data = json.load(f)
    return data.get("Messages", {})


def write_messages(path: Path, messages: Dict[str, str]) -> None:
    with open(path, "w", encoding="utf-8") as f:
        json.dump({"Messages": messages}, f, ensure_ascii=False, indent=2)
        f.write("\n")


def propagate(
    english: Dict[str, str], target_path: Path, dry_run: bool = False
) -> Dict[str, int]:
    target_messages = load_messages(target_path)
    added = [k for k in english if k not in target_messages]
    removed = [k for k in target_messages if k not in english]
    unchanged = [k for k in english if k in target_messages]

    merged: Dict[str, str] = {}
    for key, value in english.items():
        merged[key] = target_messages.get(key, value)

    if not dry_run:
        write_messages(target_path, merged)

    print(f"{target_path}:")
    if added:
        print(f"  Added: {', '.join(added)}")
    if removed:
        print(f"  Removed: {', '.join(removed)}")
    if not added and not removed:
        print("  No changes")

    return {
        "added": len(added),
        "removed": len(removed),
        "unchanged": len(unchanged),
    }


def main(argv: Tuple[str, ...] | None = None) -> None:
    root = Path(__file__).resolve().parents[1]
    default_source = root / "Resources" / "Localization" / "Messages" / "English.json"

    ap = argparse.ArgumentParser(
        description="Propagate message hashes from English into target localization files",
    )
    ap.add_argument(
        "targets",
        nargs="+",
        help="One or more target localization JSON files to update",
    )
    ap.add_argument(
        "--source",
        default=default_source,
        help="Path to English messages JSON file",
    )
    ap.add_argument(
        "--json",
        action="store_true",
        help="Write per-file statistics to propagate_metrics.json",
    )
    ap.add_argument(
        "--dry-run",
        action="store_true",
        help="Preview changes without modifying files",
    )
    args = ap.parse_args(argv)

    source_path = Path(args.source)
    if not source_path.is_absolute():
        source_path = (root / source_path).resolve()
    if not source_path.is_file():
        raise SystemExit(f"Source file not found: {source_path}")

    english = load_messages(source_path)

    metrics: Dict[str, Dict[str, int]] = {}
    for target in args.targets:
        target_path = Path(target)
        if not target_path.is_absolute():
            target_path = (root / target_path).resolve()
        if not target_path.is_file():
            print(f"{target_path}: file not found, skipping")
            continue
        stats = propagate(english, target_path, dry_run=args.dry_run)
        metrics[str(target_path)] = stats

    if args.json:
        metrics_path = Path.cwd() / "propagate_metrics.json"
        with open(metrics_path, "w", encoding="utf-8") as f:
            json.dump(metrics, f, ensure_ascii=False, indent=2)
            f.write("\n")
        print(f"Metrics written to {metrics_path}")


if __name__ == "__main__":
    main()
