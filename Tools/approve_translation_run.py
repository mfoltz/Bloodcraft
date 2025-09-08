#!/usr/bin/env python3
"""Mark a translation run as approved in its language run_index.json."""
from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

def main() -> None:
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--language", required=True, help="Language code (e.g. 'fr')")
    ap.add_argument("--run-id", required=True, help="Run identifier to approve")
    ap.add_argument(
        "--root",
        default=Path(__file__).resolve().parents[1],
        type=Path,
        help="Repository root",
    )
    args = ap.parse_args()

    index_file = args.root / "translations" / args.language / "run_index.json"
    try:
        data = json.loads(index_file.read_text(encoding="utf-8"))
    except FileNotFoundError:
        raise SystemExit(f"Run index not found: {index_file}")
    except Exception as exc:
        raise SystemExit(f"Failed to read {index_file}: {exc}") from exc

    updated = False
    if isinstance(data, list):
        for entry in data:
            if isinstance(entry, dict) and entry.get("run_id") == args.run_id:
                entry["approved"] = True
                updated = True
                break
    if not updated:
        raise SystemExit(f"Run id {args.run_id} not found in {index_file}")

    index_file.write_text(json.dumps(data, indent=2))

if __name__ == "__main__":
    main()
