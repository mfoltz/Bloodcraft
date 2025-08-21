import json
import sys

import summarize_token_stats as sts


def test_aggregate():
    entries = [
        {
            "file": "a.json",
            "failures": {"1": "token mismatch"},
            "hash_stats": {
                "1": {"original_tokens": 1, "translated_tokens": 0, "reordered": False},
                "2": {"original_tokens": 1, "translated_tokens": 1, "reordered": True},
            },
            "run_id": "1",
            "log_file": "log",
            "report_file": "report",
            "metrics_file": "metrics",
            "python_version": "3",
            "argos_version": "1",
        },
        {
            "file": "b.json",
            "failures": {},
            "hash_stats": {
                "2": {"original_tokens": 1, "translated_tokens": 1, "reordered": False}
            },
            "run_id": "2",
            "python_version": "3",
        },
    ]
    stats = sts._aggregate(entries)
    assert stats == {
        "1": {"mismatches": 1, "reorders": 0, "file": "a.json"},
        "2": {"mismatches": 0, "reorders": 1, "file": "a.json"},
    }


def test_run_dir_cli(tmp_path, monkeypatch, capsys):
    metrics = tmp_path / "translate_metrics.json"
    metrics.write_text(
        json.dumps([
            {
                "file": "a.json",
                "failures": {"1": "token mismatch"},
                "hash_stats": {
                    "1": {
                        "original_tokens": 1,
                        "translated_tokens": 0,
                        "reordered": False,
                    }
                },
            }
        ])
    )
    monkeypatch.setattr(
        sys, "argv", ["summarize_token_stats.py", "--run-dir", str(tmp_path)]
    )
    sts.main()
    out = capsys.readouterr().out
    assert "hash" in out
