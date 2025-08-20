#!/usr/bin/env python3
"""Extract hash IDs for token mismatch skips from translation logs."""

from __future__ import annotations

import argparse
import csv
import re
from pathlib import Path
from typing import Iterable, Set

ROOT = Path(__file__).resolve().parent.parent

PATTERN = re.compile(r"^(\d+): SKIPPED \(token mismatch")


def parse_hashes(path: Path) -> Set[str]:
    hashes: Set[str] = set()
    if not path.exists():
        return hashes
    with path.open(encoding="utf-8") as fp:
        for line in fp:
            match = PATTERN.search(line)
            if match:
                hashes.add(match.group(1))
    return hashes


def write_csv(hashes: Iterable[str], path: Path) -> None:
    with path.open("w", newline="", encoding="utf-8") as fp:
        writer = csv.writer(fp)
        writer.writerow(["hash"])
        for hash_id in sorted(hashes):
            writer.writerow([hash_id])


def main() -> None:
    parser = argparse.ArgumentParser(
        description="Collect hash IDs from lines marked 'SKIPPED (token mismatch)'"
    )
    parser.add_argument(
        "--log-file",
        type=Path,
        help="Path to translate.log",
    )
    parser.add_argument(
        "--run-dir",
        type=Path,
        help="Directory containing translate.log (defaults to current run dir)",
    )
    parser.add_argument(
        "--csv",
        type=Path,
        help="Optional CSV output file; hashes are printed to stdout when omitted",
    )
    args = parser.parse_args()

    if not args.log_file:
        if not args.run_dir:
            parser.error("--log-file or --run-dir is required")
        args.log_file = args.run_dir / "translate.log"

    hashes = parse_hashes(args.log_file)
    if args.csv:
        write_csv(hashes, args.csv)
    else:
        for hash_id in sorted(hashes):
            print(hash_id)


if __name__ == "__main__":
    main()
