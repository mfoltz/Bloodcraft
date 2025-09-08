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
            target = cmd[cmd.index("Tools/translate_argos.py") + 1]
            (run_dir / "metrics.json").write_text(json.dumps({"file": target}))
        elif any("fix_tokens.py" in c for c in cmd):
            if "--metrics-file" in cmd:
                metrics_path = Path(cmd[cmd.index("--metrics-file") + 1])
                metrics_path.parent.mkdir(parents=True, exist_ok=True)
                metrics_path.write_text(
                    json.dumps(
                        {
                            "tokens_restored": 0,
                            "tokens_reordered": 0,
                            "tokens_removed": 0,
                            "token_mismatches": 0,
                            "tokens_normalized": 0,
                        }
                    )
                )
            return SimpleNamespace(returncode=0), 0.0
        elif any("check_fix_tokens_metrics.py" in c for c in cmd):
            return SimpleNamespace(returncode=0), 0.0
        return SimpleNamespace(returncode=0), 0.0

    monkeypatch.setattr(localization_pipeline, "run", fake_run)

    localization_pipeline.main()

    metrics = json.loads((root / "localization_metrics.json").read_text())
    assert set(metrics["steps"].keys()) == {
        "generation",
        "propagation",
        "token_check",
        "translation",
        "token_fix",
        "strict_retry",
        "verification",
        "token_metrics",
    }
    assert "French" in metrics["languages"]
    lang = metrics["languages"]["French"]
    assert lang["token_check"]["returncode"] == 0
    assert lang["translation"]["returncode"] == 0
    assert lang["translation"]["duration"] == 0.0
    assert lang["token_autofix"]["returncode"] == 0
    assert lang["token_autofix"]["tokens_restored"] == 0
    assert lang["token_autofix"]["tokens_reordered"] == 0
    assert lang["token_autofix"]["tokens_removed"] == 0
    assert lang["token_autofix"]["token_mismatches"] == 0
    assert lang["token_autofix"]["tokens_normalized"] == 0
    assert lang["token_fix"]["returncode"] == 0
    assert lang["token_fix"]["token_mismatches"] == 0
    assert lang["skipped_hash_count"] == 0
    assert lang["success"] is True
    assert metrics["steps"]["token_fix"]["totals"] == {
        "tokens_restored": 0,
        "tokens_reordered": 0,
        "tokens_removed": 0,
        "token_mismatches": 0,
        "tokens_normalized": 0,
    }
    assert metrics["steps"]["strict_retry"] == {
        "retried": 0,
        "manual_review": 0,
    }
    assert metrics["steps"]["verification"]["duration"] == 0.0
    assert metrics["steps"]["token_metrics"]["returncode"] == 0


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
            target = cmd[cmd.index("Tools/translate_argos.py") + 1]
            (run_dir / "metrics.json").write_text(json.dumps({"file": target}))
        elif any("fix_tokens.py" in c for c in cmd):
            if "--metrics-file" in cmd:
                metrics_path = Path(cmd[cmd.index("--metrics-file") + 1])
                metrics_path.parent.mkdir(parents=True, exist_ok=True)
                metrics_path.write_text(
                    json.dumps(
                        {
                            "tokens_restored": 0,
                            "tokens_reordered": 0,
                            "tokens_removed": 0,
                            "token_mismatches": 0,
                            "tokens_normalized": 0,
                        }
                    )
                )
            return SimpleNamespace(returncode=0), 0.0
        elif any("check_fix_tokens_metrics.py" in c for c in cmd):
            return SimpleNamespace(returncode=0), 0.0
        return SimpleNamespace(returncode=0), 0.0

    monkeypatch.setattr(localization_pipeline, "run", fake_run)

    with pytest.raises(SystemExit) as exc:
        localization_pipeline.main()
    assert exc.value.code == 1

    metrics = json.loads((root / "localization_metrics.json").read_text())
    lang = metrics["languages"]["French"]
    assert lang["token_check"]["returncode"] == 0
    assert lang["skipped_hashes"] == {"timeout": 1}
    assert lang["token_fix"]["tokens_restored"] == 0
    assert lang["token_fix"]["token_mismatches"] == 0
    assert lang["token_fix"]["tokens_normalized"] == 0
    assert lang["success"] is False


def test_strict_retry_metrics(tmp_path, monkeypatch):
    root, messages_dir, english_path = _setup_repo(tmp_path)
    monkeypatch.setattr(localization_pipeline, "ROOT", root)
    monkeypatch.setattr(localization_pipeline, "MESSAGES_DIR", messages_dir)
    monkeypatch.setattr(localization_pipeline, "ENGLISH_PATH", english_path)
    monkeypatch.setattr(localization_pipeline, "LANGUAGE_CODES", {"French": "fr"})
    monkeypatch.setattr(sys, "argv", ["localization_pipeline.py"])

    calls = {"translate": 0, "review": 0}
    run_path: list[Path] = []

    def fake_run(cmd, *, check=True, logger):
        if any("translate_argos.py" in c for c in cmd):
            calls["translate"] += 1
            run_dir = Path(cmd[cmd.index("--run-dir") + 1])
            if not run_path:
                run_path.append(run_dir)
            run_dir.mkdir(parents=True, exist_ok=True)
            with (run_dir / "skipped.csv").open("w", newline="", encoding="utf-8") as fp:
                writer = csv.DictWriter(fp, fieldnames=["hash", "english", "reason", "category"])
                writer.writeheader()
                writer.writerow({
                    "hash": "1",
                    "english": "Hello",
                    "reason": "identical to source",
                    "category": "other",
                })
            target = cmd[cmd.index("Tools/translate_argos.py") + 1]
            (run_dir / "metrics.json").write_text(json.dumps({"file": target}))
            (run_dir / "translate.log").write_text(
                "2024 INFO 1: SKIPPED (identical to source)\n"
            )
        elif any("review_skipped.py" in c for c in cmd):
            calls["review"] += 1
        elif any("fix_tokens.py" in c for c in cmd):
            if "--metrics-file" in cmd:
                metrics_path = Path(cmd[cmd.index("--metrics-file") + 1])
                metrics_path.parent.mkdir(parents=True, exist_ok=True)
                metrics_path.write_text(
                    json.dumps(
                        {
                            "tokens_restored": 0,
                            "tokens_reordered": 0,
                            "tokens_removed": 0,
                            "token_mismatches": 0,
                        }
                    )
                )
            return SimpleNamespace(returncode=0), 0.0
        elif any("check_fix_tokens_metrics.py" in c for c in cmd):
            return SimpleNamespace(returncode=0), 0.0
        return SimpleNamespace(returncode=0), 0.0

    monkeypatch.setattr(localization_pipeline, "run", fake_run)

    with pytest.raises(SystemExit):
        localization_pipeline.main()

    metrics = json.loads((root / "localization_metrics.json").read_text())
    lang = metrics["languages"]["French"]
    assert lang["strict_retry"] == {"retried": 1, "manual_review": 1}
    assert metrics["steps"]["strict_retry"] == {
        "retried": 1,
        "manual_review": 1,
    }
    assert calls["translate"] == 2
    assert calls["review"] == 1
    ident = run_path[0] / "identical.csv"
    assert ident.is_file()
    rows = list(csv.DictReader(ident.open("r", encoding="utf-8")))
    assert rows[0]["hash"] == "1"


def test_custom_output_paths(tmp_path, monkeypatch):
    root, messages_dir, english_path = _setup_repo(tmp_path)
    monkeypatch.setattr(localization_pipeline, "ROOT", root)
    monkeypatch.setattr(localization_pipeline, "MESSAGES_DIR", messages_dir)
    monkeypatch.setattr(localization_pipeline, "ENGLISH_PATH", english_path)
    monkeypatch.setattr(localization_pipeline, "LANGUAGE_CODES", {"French": "fr"})
    monkeypatch.setattr(
        sys,
        "argv",
        [
            "localization_pipeline.py",
            "--metrics-file",
            "metrics.json",
            "--skipped-file",
            "skipped_all.csv",
        ],
    )

    def fake_run(cmd, *, check=True, logger):
        if any("translate_argos.py" in c for c in cmd):
            run_dir = Path(cmd[cmd.index("--run-dir") + 1])
            run_dir.mkdir(parents=True, exist_ok=True)
            (run_dir / "skipped.csv").write_text("hash,english,reason,category\n")
            target = cmd[cmd.index("Tools/translate_argos.py") + 1]
            (run_dir / "metrics.json").write_text(json.dumps({"file": target}))
        elif any("fix_tokens.py" in c for c in cmd):
            if "--metrics-file" in cmd:
                metrics_path = Path(cmd[cmd.index("--metrics-file") + 1])
                metrics_path.parent.mkdir(parents=True, exist_ok=True)
                metrics_path.write_text(
                    json.dumps(
                        {
                            "tokens_restored": 0,
                            "tokens_reordered": 0,
                            "tokens_removed": 0,
                            "token_mismatches": 0,
                        }
                    )
                )
            return SimpleNamespace(returncode=0), 0.0
        elif any("check_fix_tokens_metrics.py" in c for c in cmd):
            return SimpleNamespace(returncode=0), 0.0
        return SimpleNamespace(returncode=0), 0.0

    monkeypatch.setattr(localization_pipeline, "run", fake_run)

    localization_pipeline.main()

    assert (root / "metrics.json").is_file()
    assert (root / "skipped_all.csv").is_file()


def test_language_mismatch_detection(tmp_path, monkeypatch):
    root, messages_dir, english_path = _setup_repo(tmp_path)
    (messages_dir / "Spanish.json").write_text(
        json.dumps({"Messages": {"hash": "Hello"}})
    )
    monkeypatch.setattr(localization_pipeline, "ROOT", root)
    monkeypatch.setattr(localization_pipeline, "MESSAGES_DIR", messages_dir)
    monkeypatch.setattr(localization_pipeline, "ENGLISH_PATH", english_path)
    monkeypatch.setattr(localization_pipeline, "LANGUAGE_CODES", {"Spanish": "es"})
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
                metrics_path = Path(cmd[cmd.index("--metrics-file") + 1])
                metrics_path.parent.mkdir(parents=True, exist_ok=True)
                metrics_path.write_text(
                    json.dumps(
                        {
                            "tokens_restored": 0,
                            "tokens_reordered": 0,
                            "tokens_removed": 0,
                            "token_mismatches": 0,
                        }
                    )
                )
            return SimpleNamespace(returncode=0), 0.0
        elif any("check_fix_tokens_metrics.py" in c for c in cmd):
            return SimpleNamespace(returncode=0), 0.0
        return SimpleNamespace(returncode=0), 0.0

    monkeypatch.setattr(localization_pipeline, "run", fake_run)

    with pytest.raises(SystemExit) as exc:
        localization_pipeline.main()
    assert exc.value.code == 1

    metrics = json.loads((root / "localization_metrics.json").read_text())
    lang = metrics["languages"]["Spanish"]
    assert lang["token_check"]["returncode"] == 0
    assert lang["language_mismatches"] == 1
    assert lang["success"] is False


def test_wrong_language_detected(tmp_path, monkeypatch):
    root, messages_dir, english_path = _setup_repo(tmp_path)
    (messages_dir / "German.json").write_text(
        json.dumps({"Messages": {"hash": "Hola amigo"}})
    )
    monkeypatch.setattr(localization_pipeline, "ROOT", root)
    monkeypatch.setattr(localization_pipeline, "MESSAGES_DIR", messages_dir)
    monkeypatch.setattr(localization_pipeline, "ENGLISH_PATH", english_path)
    monkeypatch.setattr(localization_pipeline, "LANGUAGE_CODES", {"German": "de"})
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
                metrics_path = Path(cmd[cmd.index("--metrics-file") + 1])
                metrics_path.parent.mkdir(parents=True, exist_ok=True)
                metrics_path.write_text(
                    json.dumps(
                        {
                            "tokens_restored": 0,
                            "tokens_reordered": 0,
                            "tokens_removed": 0,
                            "token_mismatches": 0,
                        }
                    )
                )
            return SimpleNamespace(returncode=0), 0.0
        elif any("check_fix_tokens_metrics.py" in c for c in cmd):
            return SimpleNamespace(returncode=0), 0.0
        return SimpleNamespace(returncode=0), 0.0

    monkeypatch.setattr(localization_pipeline, "run", fake_run)

    with pytest.raises(SystemExit) as exc:
        localization_pipeline.main()
    assert exc.value.code == 1

    metrics = json.loads((root / "localization_metrics.json").read_text())
    lang = metrics["languages"]["German"]
    assert lang["token_check"]["returncode"] == 0
    assert lang["language_mismatches"] == 1
    assert lang["success"] is False


def test_abort_on_token_check_failure(tmp_path, monkeypatch):
    root, messages_dir, english_path = _setup_repo(tmp_path)
    monkeypatch.setattr(localization_pipeline, "ROOT", root)
    monkeypatch.setattr(localization_pipeline, "MESSAGES_DIR", messages_dir)
    monkeypatch.setattr(localization_pipeline, "ENGLISH_PATH", english_path)
    monkeypatch.setattr(localization_pipeline, "LANGUAGE_CODES", {"French": "fr"})
    monkeypatch.setattr(sys, "argv", ["localization_pipeline.py"])

    def fake_run(cmd, *, check=True, logger):
        if any("fix_tokens.py" in c for c in cmd):
            return SimpleNamespace(returncode=1), 0.0
        elif any("check_fix_tokens_metrics.py" in c for c in cmd):
            return SimpleNamespace(returncode=0), 0.0
        return SimpleNamespace(returncode=0), 0.0

    monkeypatch.setattr(localization_pipeline, "run", fake_run)

    with pytest.raises(SystemExit) as exc:
        localization_pipeline.main()
    assert exc.value.code == 1

    metrics = json.loads((root / "localization_metrics.json").read_text())
    lang = metrics["languages"]["French"]
    assert lang["token_check"]["returncode"] == 1
    assert "translation" not in lang
