#!/usr/bin/env python3
"""Automate the localization workflow.

Runs message generation, propagates hashes, translates, fixes tokens,
and verifies translations. Optionally limit the run to specific languages
by passing their names as arguments (e.g. ``python Tools/localization_pipeline.py French German``).
If no languages are provided, all available languages are processed.
"""

from __future__ import annotations

import argparse
import csv
import json
import logging
import subprocess
import sys
from datetime import datetime, timezone
from pathlib import Path
from typing import Dict

ROOT = Path(__file__).resolve().parents[1]
MESSAGES_DIR = ROOT / "Resources" / "Localization" / "Messages"
ENGLISH_PATH = MESSAGES_DIR / "English.json"

LANGUAGE_CODES: Dict[str, str] = {
    "Brazilian": "pb",
    "French": "fr",
    "German": "de",
    "Hungarian": "hu",
    "Italian": "it",
    "Japanese": "ja",
    "Korean": "ko",
    "Latam": "es",
    "Polish": "pl",
    "Russian": "ru",
    "SChinese": "zh",
    "Spanish": "es",
    "TChinese": "zt",
    "Thai": "th",
    "Turkish": "tr",
    "Ukrainian": "uk",
    "Vietnamese": "vi",
}

def setup_logging(debug: bool) -> logging.Logger:
    """Configure and return a logger for the script."""
    level = logging.DEBUG if debug else logging.INFO
    logging.basicConfig(level=level, format="%(asctime)s [%(levelname)s] %(message)s")
    return logging.getLogger("localization_pipeline")

def timestamp() -> str:
    """Return the current UTC time in ISO format."""
    return datetime.now(timezone.utc).isoformat()

def run(cmd: list[str], *, check: bool = True, logger: logging.Logger) -> subprocess.CompletedProcess:
    """Run a subprocess, returning the completed process."""
    logger.debug("+ %s", " ".join(str(c) for c in cmd))
    return subprocess.run(cmd, check=check, cwd=ROOT)

def propagate_hashes(target: Path) -> None:
    """Copy new hashes from English into ``target`` while preserving translations."""
    with ENGLISH_PATH.open("r", encoding="utf-8") as f:
        english = json.load(f)["Messages"]

    if target.exists():
        with target.open("r", encoding="utf-8") as f:
            data = json.load(f)
    else:
        data = {"Messages": {}}

    messages = data.get("Messages", {})
    merged = {key: messages.get(key, text) for key, text in english.items()}

    if merged != messages:
        data["Messages"] = merged
        with target.open("w", encoding="utf-8") as f:
            json.dump(data, f, indent=2, ensure_ascii=False)

def main() -> None:
    ap = argparse.ArgumentParser(
        description="Run the full localization pipeline",
        epilog="Languages default to all available message files",
    )
    ap.add_argument("--debug", action="store_true", help="Enable debug logging")
    ap.add_argument(
        "languages",
        nargs="*",
        help="Languages to process (e.g. French German). Default: all",
    )
    args = ap.parse_args()

    logger = setup_logging(args.debug)

    metrics: Dict[str, Dict] = {"steps": {}, "languages": {}}

    available = {p.stem: p for p in MESSAGES_DIR.glob("*.json") if p.name != "English.json"}
    if args.languages:
        targets = {}
        for name in args.languages:
            if name not in available:
                raise SystemExit(f"Unknown language: {name}")
            targets[name] = available[name]
    else:
        targets = available

    logger.info("Generating English messages")
    metrics["steps"]["generation"] = {"start": timestamp()}
    run(
        [
            "dotnet",
            "run",
            "--project",
            "Bloodcraft.csproj",
            "-p:RunGenerateREADME=false",
            "--",
            "generate-messages",
        ],
        logger=logger,
    )
    metrics["steps"]["generation"]["end"] = timestamp()

    logger.info("Propagating hashes")
    metrics["steps"]["propagation"] = {"start": timestamp()}
    for path in targets.values():
        propagate_hashes(path)
    metrics["steps"]["propagation"]["end"] = timestamp()

    logger.info("Translating messages")
    metrics["steps"]["translation"] = {"start": timestamp()}
    for name, path in targets.items():
        lang_metrics = metrics["languages"].setdefault(name, {"skipped_hashes": {}})
        code = LANGUAGE_CODES.get(name)
        if not code:
            logger.warning("Skipping %s: no translation code configured", name)
            lang_metrics["skipped"] = True
            continue
        lang_metrics["skipped"] = False
        t_start = timestamp()
        result = run(
            [
                sys.executable,
                "Tools/translate_argos.py",
                str(path.relative_to(ROOT)),
                "--to",
                code,
                "--batch-size",
                "100",
                "--max-retries",
                "3",
                "--verbose",
                "--log-file",
                "translate.log",
                "--report-file",
                "skipped.csv",
                "--overwrite",
            ],
            check=False,
            logger=logger,
        )
        t_end = timestamp()
        lang_metrics["translation"] = {
            "start": t_start,
            "end": t_end,
            "returncode": result.returncode,
        }
        report = ROOT / "skipped.csv"
        skipped_counts: Dict[str, int] = {}
        if report.is_file():
            with report.open("r", encoding="utf-8") as fp:
                reader = csv.DictReader(fp)
                for row in reader:
                    reason = row.get("reason", "")
                    skipped_counts[reason] = skipped_counts.get(reason, 0) + 1
            report.unlink()
        lang_metrics["skipped_hashes"] = skipped_counts
    metrics["steps"]["translation"]["end"] = timestamp()

    logger.info("Fixing tokens")
    metrics["steps"]["token_fix"] = {"start": timestamp()}
    for name, path in targets.items():
        lang_metrics = metrics["languages"].get(name)
        if not lang_metrics or lang_metrics.get("skipped"):
            continue
        t_start = timestamp()
        result = run(
            [sys.executable, "Tools/fix_tokens.py", str(path.relative_to(ROOT))],
            check=False,
            logger=logger,
        )
        t_end = timestamp()
        lang_metrics["token_fix"] = {
            "start": t_start,
            "end": t_end,
            "returncode": result.returncode,
        }
    metrics["steps"]["token_fix"]["end"] = timestamp()

    overall_ok = True
    for lang_metrics in metrics["languages"].values():
        if lang_metrics.get("skipped"):
            continue
        translation_ok = lang_metrics["translation"]["returncode"] == 0
        token_ok = lang_metrics["token_fix"]["returncode"] == 0
        skipped_total = sum(lang_metrics["skipped_hashes"].values())
        lang_metrics["skipped_hash_count"] = skipped_total
        success = translation_ok and token_ok and skipped_total == 0
        lang_metrics["success"] = success
        overall_ok &= success

    logger.info("Verifying translations")
    metrics["steps"]["verification"] = {"start": timestamp()}
    verify_proc = run(
        [
            "dotnet",
            "run",
            "--project",
            "Bloodcraft.csproj",
            "-p:RunGenerateREADME=false",
            "--",
            "check-translations",
        ],
        check=False,
        logger=logger,
    )
    metrics["steps"]["verification"].update(
        {"end": timestamp(), "returncode": verify_proc.returncode}
    )
    overall_ok &= verify_proc.returncode == 0

    metrics_path = ROOT / "localization_metrics.json"
    with metrics_path.open("w", encoding="utf-8") as f:
        json.dump(metrics, f, indent=2, ensure_ascii=False)
    logger.info("Wrote metrics to %s", metrics_path)
    print(metrics_path)

    if not overall_ok:
        raise SystemExit(1)

if __name__ == "__main__":
    main()
