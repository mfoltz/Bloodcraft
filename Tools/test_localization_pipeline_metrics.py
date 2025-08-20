import csv
import json
import sys
from pathlib import Path
from types import SimpleNamespace

import pytest

import localization_pipeline


def _setup_repo(tmp_path):
    root = tmp_path
    messages_dir = root / "Resources" / "Localization" / "Messages"
    messages_dir.mkdir(parents=True)
    english_path = messages_dir / "English.json"
    english_path.write_text(json.dumps({"Messages": {"hash": "Hello"}}))
    (messages_dir / "French.json").write_text(
        json.dumps({"Messages": {"hash": "Bonjour"}})
    )
    return root, messages_dir, english_path


def test_metrics_written(tmp_path, monkeypatch):
    root, messages_dir, english_path = _setup_repo(tmp_path)
    monkeypatch.setattr(localization_pipeline, "ROOT", root)
    monkeypatch.setattr(localization_pipeline, "MESSAGES_DIR", messages_dir)
    monkeypatch.setattr(localization_pipeline, "ENGLISH_PATH", english_path)
    monkeypatch.setattr(localization_pipeline, "LANGUAGE_CODES", {"French": "fr"})
    monkeypatch.setattr(sys, "argv", ["localization_pipeline.py"])

    def fake_run(cmd, *, check=True, logger):
        if any("translate_argos.py" in c for c in cmd):
            run_dir = Path(cmd[cmd.index("--run-dir") + 1])
            run_dir.mkdir(parents=True, exist_ok=True)
            (run_dir / "skipped.csv").write_text("hash,english,reason,category\n")
        return SimpleNamespace(returncode=0)

    monkeypatch.setattr(localization_pipeline, "run", fake_run)

    localization_pipeline.main()

    metrics = json.loads((root / "localization_metrics.json").read_text())
    assert set(metrics["steps"].keys()) == {
        "generation",
        "propagation",
        "translation",
        "token_fix",
        "verification",
    }
    assert "French" in metrics["languages"]
    lang = metrics["languages"]["French"]
    assert lang["translation"]["returncode"] == 0
    assert lang["token_fix"]["returncode"] == 0
    assert lang["token_fix"]["tokens_restored"] == 0
    assert lang["token_fix"]["tokens_reordered"] == 0
    assert lang["token_fix"]["token_mismatches"] == 0
    assert lang["skipped_hash_count"] == 0
    assert lang["success"] is True
    assert metrics["steps"]["token_fix"]["totals"] == {
        "tokens_restored": 0,
        "tokens_reordered": 0,
        "token_mismatches": 0,
    }


def test_exit_code_on_skipped(tmp_path, monkeypatch):
    root, messages_dir, english_path = _setup_repo(tmp_path)
    monkeypatch.setattr(localization_pipeline, "ROOT", root)
    monkeypatch.setattr(localization_pipeline, "MESSAGES_DIR", messages_dir)
    monkeypatch.setattr(localization_pipeline, "ENGLISH_PATH", english_path)
    monkeypatch.setattr(localization_pipeline, "LANGUAGE_CODES", {"French": "fr"})
    monkeypatch.setattr(sys, "argv", ["localization_pipeline.py"])

    def fake_run(cmd, *, check=True, logger):
        if any("translate_argos.py" in c for c in cmd):
            run_dir = Path(cmd[cmd.index("--run-dir") + 1])
            run_dir.mkdir(parents=True, exist_ok=True)
            with (run_dir / "skipped.csv").open("w", newline="", encoding="utf-8") as fp:
                writer = csv.DictWriter(fp, fieldnames=["hash", "english", "reason"])
                writer.writeheader()
                writer.writerow({"hash": "h", "english": "Hello", "reason": "timeout"})
        return SimpleNamespace(returncode=0)

    monkeypatch.setattr(localization_pipeline, "run", fake_run)

    with pytest.raises(SystemExit) as exc:
        localization_pipeline.main()
    assert exc.value.code == 1

    metrics = json.loads((root / "localization_metrics.json").read_text())
    lang = metrics["languages"]["French"]
    assert lang["skipped_hashes"] == {"timeout": 1}
    assert lang["token_fix"]["tokens_restored"] == 0
    assert lang["token_fix"]["token_mismatches"] == 0
    assert lang["success"] is False
