import json
import sys
from pathlib import Path
from types import SimpleNamespace

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
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


def test_token_metrics_collected(tmp_path, monkeypatch):
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
            target = cmd[cmd.index("Tools/translate_argos.py") + 1]
            (run_dir / "metrics.json").write_text(json.dumps({"file": target}))
        elif any("fix_tokens.py" in c for c in cmd):
            if "--metrics-file" in cmd:
                metrics_file = Path(cmd[cmd.index("--metrics-file") + 1])
                metrics_file.write_text(
                    json.dumps(
                        {
                            "tokens_restored": 1,
                            "tokens_reordered": 2,
                            "token_mismatches": 0,
                        }
                    )
                )
            return SimpleNamespace(returncode=0), 0.0
        return SimpleNamespace(returncode=0), 0.0

    monkeypatch.setattr(localization_pipeline, "run", fake_run)

    localization_pipeline.main()

    metrics = json.loads((root / "localization_metrics.json").read_text())
    lang = metrics["languages"]["French"]
    assert lang["token_check"]["returncode"] == 0
    assert lang["token_fix"]["tokens_restored"] == 1
    assert lang["token_fix"]["tokens_reordered"] == 2
    totals = metrics["steps"]["token_fix"]["totals"]
    assert totals["tokens_restored"] == 1
    assert totals["tokens_reordered"] == 2


def test_validate_only_skips_argos_and_records_metrics(tmp_path, monkeypatch):
    root, messages_dir, english_path = _setup_repo(tmp_path)
    monkeypatch.setattr(localization_pipeline, "ROOT", root)
    monkeypatch.setattr(localization_pipeline, "MESSAGES_DIR", messages_dir)
    monkeypatch.setattr(localization_pipeline, "ENGLISH_PATH", english_path)
    monkeypatch.setattr(localization_pipeline, "LANGUAGE_CODES", {"French": "fr"})
    monkeypatch.setattr(
        sys,
        "argv",
        ["localization_pipeline.py", "--validate-only", "--skipped-file", "combined.csv"],
    )

    calls = []

    def fake_run(cmd, *, check=True, logger):
        calls.append(cmd)
        if any("translate_argos.py" in c for c in cmd):
            raise AssertionError("translate_argos.py should not be called in --validate-only mode")
        if any("fix_tokens.py" in c for c in cmd) and "--metrics-file" in cmd:
            metrics_file = Path(cmd[cmd.index("--metrics-file") + 1])
            metrics_file.write_text(
                json.dumps(
                    {
                        "tokens_restored": 0,
                        "tokens_reordered": 0,
                        "token_mismatches": 0,
                        "tokens_normalized": 0,
                    }
                )
            )
        return SimpleNamespace(returncode=0), 0.0

    monkeypatch.setattr(localization_pipeline, "run", fake_run)

    localization_pipeline.main()

    metrics = json.loads((root / "localization_metrics.json").read_text())
    lang = metrics["languages"]["French"]
    assert lang["translation"]["skipped"] is True
    assert lang["token_autofix"]["skipped"] is True
    assert lang["validation"]["skipped"] is True
    assert lang["success"] is True
    assert metrics["steps"]["translation"]["skipped"] is True
    assert metrics["steps"]["token_fix"]["skipped"] is True
    assert metrics["steps"]["strict_retry"]["skipped"] is True

    combined = root / "combined.csv"
    assert combined.exists()
    assert combined.read_text(encoding="utf-8").splitlines() == ["hash,english,reason,category"]

    assert not any(any("translate_argos.py" in c for c in cmd) for cmd in calls)
