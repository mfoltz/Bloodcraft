#!/usr/bin/env python3
"""Print a summary of translation runs from translations/run_index.json."""

import argparse
import json
from pathlib import Path


def main() -> None:
    ap = argparse.ArgumentParser(description="List past translation runs")
    ap.add_argument(
        "--root",
        default=".",
        help="Repository root containing translations/run_index.json",
    )
    args = ap.parse_args()

    index_path = Path(args.root) / "translations" / "run_index.json"
    try:
        with index_path.open(encoding="utf-8") as fp:
            entries = json.load(fp)
    except FileNotFoundError:
        print(f"No run index found at {index_path}")
        return
    except json.JSONDecodeError as e:
        print(f"Failed to parse {index_path}: {e}")
        return

    if not entries:
        print("No translation runs recorded.")
        return

    header = (
        "Run ID                               Timestamp               Lang  Success%  "
        "Log File                        Report File                     Metrics File"
    )
    print(header)
    for entry in entries:
        run_id = entry.get("run_id", "")
        ts = entry.get("timestamp", "")
        lang = entry.get("language", "")
        rate = entry.get("success_rate", 0)
        log_file = entry.get("log_file", "")
        report_file = entry.get("report_file", "")
        metrics_file = entry.get("metrics_file", "")
        print(
            f"{run_id:36} {ts:20} {lang:>4}  {rate*100:7.2f}%  "
            f"{log_file}  {report_file}  {metrics_file}"
        )


if __name__ == "__main__":
    main()
