#!/usr/bin/env python3
"""Archive translation log files with timestamped directories.

This script moves ``translate.log`` and ``skipped.csv`` into a directory
named ``TranslationLogs/<timestamp>`` relative to the repository root. For
each archived file an accompanying ``.info`` file is written containing the
timestamp of the run, providing a clear indicator of when the log was
generated.
"""

from __future__ import annotations

import datetime as _dt
import shutil
from pathlib import Path


def archive_logs() -> Path:
    """Move translation logs into a timestamped directory.

    Returns the directory containing the archived logs. If no logs are
    present the directory is still created so callers can attach additional
    artifacts if desired.
    """

    root = Path(__file__).resolve().parent.parent
    timestamp = _dt.datetime.now().strftime("%Y%m%d-%H%M%S")
    destination = root / "TranslationLogs" / timestamp
    destination.mkdir(parents=True, exist_ok=True)

    for name in ("translate.log", "skipped.csv"):
        source = root / name
        if source.exists():
            target = destination / name
            shutil.move(str(source), target)
            info = destination / f"{name}.info"
            info.write_text(f"Archived: {timestamp}\n")

    return destination


if __name__ == "__main__":
    archive_logs()

