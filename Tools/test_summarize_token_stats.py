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
            "token_mismatch_details": {"1": {"missing": ["4"], "extra": []}},
        },
        {
            "file": "b.json",
            "failures": {},
            "hash_stats": {
                "2": {"original_tokens": 1, "translated_tokens": 1, "reordered": False}
            },
            "token_mismatch_details": {"2": {"missing": ["4", "5"], "extra": []}},
        },
    ]
    stats, token_counts = sts._aggregate(entries)
    assert stats == {
        "1": {"mismatches": 1, "reorders": 0, "file": "a.json"},
        "2": {"mismatches": 1, "reorders": 1, "file": "b.json"},
    }
    assert token_counts == {"4": 2, "5": 1}


def test_run_dir_cli(tmp_path, monkeypatch, capsys):
    metrics = tmp_path / "metrics.json"
    metrics.write_text(
        json.dumps(
            [
                {
                    "file": "a.json",
                    "failures": {},
                    "hash_stats": {
                        "1": {
                            "original_tokens": 1,
                            "translated_tokens": 0,
                            "reordered": False,
                        }
                    },
                    "token_mismatch_details": {
                        "1": {"missing": ["4"], "extra": []}
                    },
                }
            ]
        )
    )
    token_csv = tmp_path / "tokens.csv"
    monkeypatch.setattr(
        sys,
        "argv",
        [
            "summarize_token_stats.py",
            "--run-dir",
            str(tmp_path),
            "--token-csv",
            str(token_csv),
        ],
    )
    sts.main()
    out = capsys.readouterr().out
    assert out.splitlines()[0] == "Top missing tokens:"
    assert "hash" in out
    assert token_csv.exists()
    assert token_csv.read_text().strip().splitlines()[1] == "4,1"
