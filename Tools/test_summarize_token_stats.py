import json
import sys
from pathlib import Path

import summarize_token_stats as sts


def _make_run(tmp: Path, lang: str, run: str, metrics: dict, summary: dict) -> None:
    run_dir = tmp / lang / run
    run_dir.mkdir(parents=True)
    (run_dir / "metrics.json").write_text(json.dumps([metrics]), encoding="utf-8")
    (run_dir / "token_mismatch_summary.json").write_text(
        json.dumps([summary]), encoding="utf-8"
    )


def test_collect_stats(tmp_path):
    _make_run(
        tmp_path,
        "fr",
        "run1",
        {"processed": 10, "successes": 8, "timeouts": 1},
        {
            "token_mismatches": 2,
            "mismatches": [
                {"key": "1", "missing": [], "extra": ["{0}"]},
                {"key": "2", "missing": [], "extra": []},
            ],
        },
    )
    _make_run(
        tmp_path,
        "de",
        "runA",
        {"processed": 5, "successes": 5, "timeouts": 0},
        {"token_mismatches": 1, "mismatches": [{"key": "1", "missing": [], "extra": []}]},
    )

    lang_stats, hash_counts = sts.collect_stats(tmp_path)
    assert lang_stats["fr"]["processed"] == 10
    assert lang_stats["fr"]["token_mismatches"] == 2
    assert lang_stats["de"]["success_rate"] == 1.0
    assert hash_counts["1"] == 2
    assert hash_counts["2"] == 1


def test_cli_outputs(tmp_path, monkeypatch):
    _make_run(
        tmp_path,
        "es",
        "run2",
        {"processed": 1, "successes": 1, "timeouts": 0},
        {"token_mismatches": 0, "mismatches": []},
    )
    json_out = tmp_path / "out.json"
    lang_csv = tmp_path / "langs.csv"
    hash_csv = tmp_path / "hashes.csv"

    monkeypatch.setattr(
        sys,
        "argv",
        [
            "summarize_token_stats.py",
            "--translations-root",
            str(tmp_path),
            "--json",
            str(json_out),
            "--languages-csv",
            str(lang_csv),
            "--hashes-csv",
            str(hash_csv),
        ],
    )

    sts.main()

    assert json_out.exists()
    data = json.loads(json_out.read_text(encoding="utf-8"))
    assert data["languages"][0]["language"] == "es"
    assert lang_csv.exists() and hash_csv.exists()
