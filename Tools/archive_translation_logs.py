#!/usr/bin/env python3
"""Archive translation log directories for later inspection.

Moves a translation run directory into ``TranslationLogs/<commit>/<timestamp>``
relative to the repository root. The git commit hash is included so log sets
can be traced back to the code that produced them.
"""

from __future__ import annotations

import argparse
import datetime as _dt
import shutil
import subprocess
from pathlib import Path


def archive_logs(run_dir: str | Path) -> Path:
    """Move ``run_dir`` into a commit-scoped archive directory.

    Parameters
    ----------
    run_dir:
        Directory containing translation artifacts such as ``translate.log``
        and ``skipped.csv``.

    Returns
    -------
    Path
        Destination directory under ``TranslationLogs/<commit>/<timestamp>``.
    """

    root = Path(__file__).resolve().parent.parent
    run_path = Path(run_dir)
    timestamp = run_path.name or _dt.datetime.now().strftime("%Y%m%d-%H%M%S")
    try:
        commit = (
            subprocess.check_output(
                ["git", "rev-parse", "--short", "HEAD"], cwd=root
            )
            .decode()
            .strip()
        )
    except Exception:
        commit = "unknown"

    destination = root / "TranslationLogs" / commit / timestamp
    destination.parent.mkdir(parents=True, exist_ok=True)

    if run_path.exists():
        shutil.move(str(run_path), destination)
    else:
        destination.mkdir(parents=True, exist_ok=True)

    info = destination / "archive.info"
    info.write_text(f"Archived: {timestamp}\nCommit: {commit}\n")

    return destination


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Archive translation logs")
    parser.add_argument("run_dir", help="Translation run directory")
    archive_logs(parser.parse_args().run_dir)

