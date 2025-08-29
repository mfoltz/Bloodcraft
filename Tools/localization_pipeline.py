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
import signal
import time
from concurrent.futures import ThreadPoolExecutor, as_completed
import language_utils
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

def run(
    cmd: list[str], *, check: bool = True, logger: logging.Logger
) -> tuple[subprocess.CompletedProcess, float]:
    """Run a subprocess, returning the completed process and duration."""
    logger.debug("+ %s", " ".join(str(c) for c in cmd))
    start = time.monotonic()
    proc = subprocess.run(cmd, check=check, cwd=ROOT)
    duration = time.monotonic() - start
    logger.debug("Command finished in %.2f seconds", duration)
    return proc, duration

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


def process_language(name: str, path: Path, logger: logging.Logger) -> dict:
    """Translate, validate and fix tokens for a single language."""
    lang_metrics: Dict[str, Dict] = {"skipped_hashes": {}}
    code = LANGUAGE_CODES.get(name)
    if not code:
        logger.warning("Skipping %s: no translation code configured", name)
        lang_metrics["skipped"] = True
        return {"name": name, "lang_metrics": lang_metrics}

    lang_metrics["skipped"] = False
    t_start = timestamp()
    run_dir = ROOT / "translations" / code / datetime.now().strftime("%Y%m%d-%H%M%S-%f")
    result, duration = run(
        [
            sys.executable,
            "Tools/translate_argos.py",
            str(path.relative_to(ROOT)),
            "--to",
            code,
            "--run-dir",
            str(run_dir),
            "--batch-size",
            "100",
            "--max-retries",
            "3",
            "--log-level",
            "INFO",
            "--overwrite",
        ],
        check=False,
        logger=logger,
    )
    metrics_file_path = run_dir / "metrics.json"
    expected_file = str(path.relative_to(ROOT))
    if not metrics_file_path.is_file():
        raise SystemExit(f"Missing metrics file in {run_dir}")
    with metrics_file_path.open("r", encoding="utf-8") as mf:
        metrics_data = json.load(mf)
    if isinstance(metrics_data, list):
        metrics_entry = metrics_data[-1] if metrics_data else {}
    else:
        metrics_entry = metrics_data
    actual_file = metrics_entry.get("file")
    if actual_file != expected_file:
        raise SystemExit(
            f"Run directory {run_dir} metrics mismatch: {actual_file} != {expected_file}"
        )

    report = run_dir / "skipped.csv"
    t_end = timestamp()
    lang_metrics["translation"] = {
        "start": t_start,
        "end": t_end,
        "duration": duration,
        "returncode": result.returncode,
    }
    skipped_counts: Dict[str, int] = {}
    if report.is_file():
        with report.open("r", encoding="utf-8") as fp:
            reader = csv.DictReader(fp)
            for row in reader:
                reason = row.get("reason", "")
                skipped_counts[reason] = skipped_counts.get(reason, 0) + 1
    lang_metrics["skipped_hashes"] = skipped_counts

    mismatches = 0
    with path.open("r", encoding="utf-8") as fp:
        messages = json.load(fp).get("Messages", {})
    for txt in messages.values():
        if language_utils.has_words(txt) and not language_utils.contains_language_code(txt, code):
            mismatches += 1
    lang_metrics["language_mismatches"] = mismatches
    if mismatches:
        logger.error(
            "%s: detected %d strings that do not match language code %s",
            name,
            mismatches,
            code,
        )
    validate_proc, _ = run(
        [sys.executable, "Tools/validate_translation_run.py", "--run-dir", str(run_dir)],
        check=False,
        logger=logger,
    )
    lang_metrics["validation"] = {"returncode": validate_proc.returncode}

    logger.info("Fixing tokens for %s", name)
    t_fix_start = timestamp()
    metrics_file = ROOT / f"fix_tokens_{name}.json"
    fix_proc, _ = run(
        [
            sys.executable,
            "Tools/fix_tokens.py",
            str(path.relative_to(ROOT)),
            "--metrics-file",
            str(metrics_file),
            "--reorder",
        ],
        check=False,
        logger=logger,
    )
    t_fix_end = timestamp()
    token_data = {
        "tokens_restored": 0,
        "tokens_reordered": 0,
        "token_mismatches": 0,
    }
    if metrics_file.is_file():
        with metrics_file.open("r", encoding="utf-8") as fp:
            token_data.update(json.load(fp))
        metrics_file.unlink()
    lang_metrics["token_fix"] = {
        "start": t_fix_start,
        "end": t_fix_end,
        "returncode": fix_proc.returncode,
        **token_data,
    }

    return {
        "name": name,
        "lang_metrics": lang_metrics,
        "report": report,
        "run_dir": run_dir,
    }

def main() -> None:
    ap = argparse.ArgumentParser(
        description="Run the full localization pipeline",
        epilog="Languages default to all available message files",
    )
    ap.add_argument("--debug", action="store_true", help="Enable debug logging")
    ap.add_argument(
        "--archive-logs",
        action="store_true",
        help="Archive translation logs after the run",
    )
    ap.add_argument(
        "--metrics-file",
        default="localization_metrics.json",
        help="Write JSON metrics to this path",
    )
    ap.add_argument(
        "--skipped-file",
        default="skipped.csv",
        help="Write combined skipped hashes to this CSV file",
    )
    ap.add_argument(
        "languages",
        nargs="*",
        help="Languages to process (e.g. French German). Default: all",
    )
    args = ap.parse_args()

    logger = setup_logging(args.debug)

    metrics: Dict[str, Dict] = {"steps": {}, "languages": {}}

    metrics_path = Path(args.metrics_file)
    if not metrics_path.is_absolute():
        metrics_path = ROOT / metrics_path
    metrics_path.parent.mkdir(parents=True, exist_ok=True)
    combined_report = Path(args.skipped_file)
    if not combined_report.is_absolute():
        combined_report = ROOT / combined_report
    combined_report.parent.mkdir(parents=True, exist_ok=True)

    signal.signal(signal.SIGTERM, signal.default_int_handler)

    available = {p.stem: p for p in MESSAGES_DIR.glob("*.json") if p.name != "English.json"}
    if args.languages:
        targets = {}
        for name in args.languages:
            if name not in available:
                raise SystemExit(f"Unknown language: {name}")
            targets[name] = available[name]
    else:
        targets = available

    run_dirs: list[Path] = []
    overall_ok = True
    try:
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
        metrics["steps"]["token_fix"] = {
            "start": timestamp(),
            "totals": {
                "tokens_restored": 0,
                "tokens_reordered": 0,
                "token_mismatches": 0,
            },
        }
        report_paths: list[Path] = []
        results = []
        with ThreadPoolExecutor() as executor:
            future_map = {
                executor.submit(process_language, name, path, logger): name
                for name, path in targets.items()
            }
            for future in as_completed(future_map):
                results.append(future.result())

        for res in results:
            name = res["name"]
            lang_metrics = res["lang_metrics"]
            metrics["languages"][name] = lang_metrics
            report = res.get("report")
            if report:
                report_paths.append(report)
            run_dir = res.get("run_dir")
            if run_dir:
                run_dirs.append(run_dir)
            token_data = lang_metrics.get("token_fix", {})
            totals = metrics["steps"]["token_fix"]["totals"]
            totals["tokens_restored"] += token_data.get("tokens_restored", 0)
            totals["tokens_reordered"] += token_data.get("tokens_reordered", 0)
            totals["token_mismatches"] += token_data.get("token_mismatches", 0)
        metrics["steps"]["translation"]["end"] = timestamp()
        metrics["steps"]["token_fix"]["end"] = timestamp()

        with combined_report.open("w", newline="", encoding="utf-8") as out_fp:
            writer = csv.writer(out_fp)
            writer.writerow(["hash", "english", "reason", "category"])
            for report in report_paths:
                if not report.is_file():
                    continue
                with report.open("r", encoding="utf-8") as in_fp:
                    reader = csv.reader(in_fp)
                    next(reader, None)
                    for row in reader:
                        writer.writerow(row)

        logger.info("Analyzing skipped translations")
        run(
            [
                sys.executable,
                "Tools/analyze_skip_report.py",
                str(combined_report.relative_to(ROOT)),
            ],
            check=False,
            logger=logger,
        )

        logger.info("Summarizing token statistics")
        for run_dir in run_dirs:
            run(
                [
                    sys.executable,
                    "Tools/summarize_token_stats.py",
                    "--run-dir",
                    str(run_dir),
                ],
                check=False,
                logger=logger,
            )

        overall_ok = True
        for lang_metrics in metrics["languages"].values():
            if lang_metrics.get("skipped"):
                continue
            translation_ok = lang_metrics["translation"]["returncode"] == 0
            token_ok = lang_metrics["token_fix"]["returncode"] == 0
            mismatch_ok = lang_metrics["token_fix"]["token_mismatches"] == 0
            validation_ok = lang_metrics.get("validation", {}).get("returncode", 0) == 0
            language_ok = lang_metrics.get("language_mismatches", 0) == 0
            skipped_total = sum(lang_metrics["skipped_hashes"].values())
            lang_metrics["skipped_hash_count"] = skipped_total
            success = (
                translation_ok
                and token_ok
                and mismatch_ok
                and language_ok
                and skipped_total == 0
                and validation_ok
            )
            lang_metrics["success"] = success
            overall_ok &= success

        logger.info("Analyzing translation logs")
        analysis_proc, _ = run(
            [sys.executable, "Tools/analyze_translation_logs.py"],
            check=False,
            logger=logger,
        )
        if analysis_proc.returncode != 0:
            logger.error(
                "Translation log analysis found unresolved mismatches or placeholder-only rows"
            )
            overall_ok = False

        logger.info("Verifying translations")
        metrics["steps"]["verification"] = {"start": timestamp()}
        verify_proc, duration = run(
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
            {"end": timestamp(), "duration": duration, "returncode": verify_proc.returncode}
        )
        overall_ok &= verify_proc.returncode == 0

        with metrics_path.open("w", encoding="utf-8") as f:
            json.dump(metrics, f, indent=2, ensure_ascii=False)
        logger.info("Wrote metrics to %s", metrics_path)
        print(metrics_path)
    except KeyboardInterrupt:
        logger.warning("Pipeline interrupted")
        metrics["status"] = "interrupted"
        with metrics_path.open("w", encoding="utf-8") as f:
            json.dump(metrics, f, indent=2, ensure_ascii=False)
        overall_ok = False
        raise SystemExit(1)
    finally:
        if args.archive_logs:
            import archive_translation_logs

            for rd in run_dirs:
                try:
                    archive_translation_logs.archive_logs(rd)
                except Exception as exc:
                    logger.warning("Failed to archive logs for %s: %s", rd, exc)

    if not overall_ok:
        raise SystemExit(1)

if __name__ == "__main__":
    main()
