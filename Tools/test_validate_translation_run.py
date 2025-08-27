import csv
import csv
import json
import subprocess
import sys
from pathlib import Path

SCRIPT = Path(__file__).resolve().with_name("validate_translation_run.py")


def _write_csv(path: Path, rows: list[dict[str, str]]) -> None:
    with path.open("w", newline="", encoding="utf-8") as fp:
        writer = csv.DictWriter(fp, fieldnames=["hash", "english", "reason", "category"])
        writer.writeheader()
        for row in rows:
            writer.writerow(row)


def test_exit_code_on_issues(tmp_path: Path) -> None:
    run_dir = tmp_path
    (run_dir / "translate.log").write_text(
        "hash1: TRANSLATED\n" "hash2: SKIPPED (token mismatch)\n", encoding="utf-8"
    )
    _write_csv(
        run_dir / "skipped.csv",
        [{"hash": "h2", "english": "e", "reason": "r", "category": "token_mismatch"}],
    )
    (run_dir / "token_mismatches.json").write_text(
        json.dumps([{"file": "f", "key": "k", "missing": ["t"], "extra": []}]),
        encoding="utf-8",
    )
    proc = subprocess.run(
        [sys.executable, str(SCRIPT), "--run-dir", str(run_dir)],
        capture_output=True,
        text=True,
    )
    assert proc.returncode == 1
    assert "Log results: 1 TRANSLATED, 1 SKIPPED" in proc.stdout
    assert "token_mismatch: 1" in proc.stdout
    assert "Token mismatch report: 1 entries" in proc.stdout
    summary = json.loads((run_dir / "token_mismatch_summary.json").read_text(encoding="utf-8"))
    assert summary == {"k": {"missing": ["t"], "extra": []}}


def test_success_without_issues(tmp_path: Path) -> None:
    run_dir = tmp_path
    (run_dir / "translate.log").write_text(
        "hash1: TRANSLATED\n", encoding="utf-8"
    )
    _write_csv(run_dir / "skipped.csv", [])
    (run_dir / "token_mismatches.json").write_text("[]", encoding="utf-8")
    proc = subprocess.run(
        [sys.executable, str(SCRIPT), "--run-dir", str(run_dir)],
        capture_output=True,
        text=True,
    )
    assert proc.returncode == 0
    assert "Log results: 1 TRANSLATED, 0 SKIPPED" in proc.stdout
    assert "Token mismatch report: 0 entries" in proc.stdout
    summary = json.loads((run_dir / "token_mismatch_summary.json").read_text(encoding="utf-8"))
    assert summary == {}
