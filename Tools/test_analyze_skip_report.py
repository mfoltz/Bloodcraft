import csv
from collections import Counter
import sys
from pathlib import Path

import analyze_skip_report as asr
import pytest


def test_summarize_report(tmp_path):
    path = tmp_path / "skipped.csv"
    with path.open("w", newline="", encoding="utf-8") as fp:
        writer = csv.DictWriter(fp, fieldnames=["hash", "english", "reason", "category"])
        writer.writeheader()
        writer.writerow({"hash": "1", "english": "a", "reason": "r", "category": "identical"})
        writer.writerow({"hash": "2", "english": "a", "reason": "r", "category": "identical"})
        writer.writerow({"hash": "3", "english": "a", "reason": "r", "category": "token_mismatch"})
        writer.writerow({"hash": "4", "english": "a", "reason": "r", "category": ""})
    counts = asr.summarize_report(path)
    assert counts == Counter({"identical": 2, "token_mismatch": 1, "unknown": 1})


def test_main_cli(tmp_path, monkeypatch, capsys):
    report = tmp_path / "skipped.csv"
    report.write_text("hash,english,reason,category\n1,a,r,identical\n", encoding="utf-8")
    monkeypatch.setattr(sys, "argv", ["analyze_skip_report.py", str(report)])
    with pytest.raises(SystemExit) as exc:
        asr.main()
    assert exc.value.code == 0
    captured = capsys.readouterr().out.strip().splitlines()
    assert captured == ["identical: 1"]

