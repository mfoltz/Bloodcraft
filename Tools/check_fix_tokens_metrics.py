#!/usr/bin/env python3
"""Fail when fix_tokens_metrics.json contains unresolved mismatches."""
from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

def main() -> None:
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument(
        "--file",
        default=Path(__file__).resolve().parents[1] / "fix_tokens_metrics.json",
        type=Path,
        help="Path to fix_tokens_metrics.json",
    )
    args = ap.parse_args()

    path = args.file
    try:
        data = json.loads(path.read_text(encoding="utf-8"))
    except FileNotFoundError:
        return
    except Exception as exc:
        print(f"Failed to read {path}: {exc}", file=sys.stderr)
        raise SystemExit(1) from exc

    entries = data if isinstance(data, list) else [data]
    unresolved = []
    for entry in entries:
        if not isinstance(entry, dict):
            continue
        mismatches = entry.get("token_mismatches") or 0
        mismatch_list = entry.get("mismatches") or []
        if mismatches or mismatch_list:
            unresolved.append(
                {
                    "timestamp": entry.get("timestamp"),
                    "token_mismatches": mismatches,
                }
            )
    if unresolved:
        print("Unresolved token mismatches detected:")
        for item in unresolved:
            print(
                f"  {item.get('timestamp')}: {item.get('token_mismatches')} mismatches"
            )
        raise SystemExit(1)

if __name__ == "__main__":
    main()
