import csv
import json
import logging
import os
import re
import subprocess
import sys
import tempfile
from pathlib import Path
import platform

import pytest

import translate_argos
from translate_argos import ensure_model_installed
import token_patterns


@pytest.fixture(autouse=True)
def _stub_check_output(monkeypatch):
    def fake_check_output(cmd, *a, **k):
        if cmd and cmd[0] == "git":
            raise subprocess.CalledProcessError(1, cmd)
        return b""

    monkeypatch.setattr(subprocess, "check_output", fake_check_output)


def test_raises_when_segments_present_without_model():
    with tempfile.TemporaryDirectory() as root:
        model_dir = os.path.join(
            root, "Resources", "Localization", "Models", "xx"
        )
        os.makedirs(model_dir)
        open(os.path.join(model_dir, "translate-test.z01"), "wb").close()
        with pytest.raises(RuntimeError) as err:
            ensure_model_installed(root, "xx")
        assert "cd Resources/Localization/Models/xx" in str(err.value)


def test_run_dir_creates_outputs(tmp_path, monkeypatch):
    root = tmp_path
    messages_dir = root / "Resources" / "Localization" / "Messages"
    messages_dir.mkdir(parents=True)
    (messages_dir / "English.json").write_text(
        json.dumps({"Messages": {"hash": "Hello"}})
    )

    target_rel = "Resources/Localization/Messages/Test.json"
    run_dir = root / "logs" / "nested" / "run"

    class DummyTranslator:
        def translate(self, text):
            return "Bonjour"

    class DummyCompleted:
        def __init__(self, code=0):
            self.returncode = code

    monkeypatch.setattr(
        translate_argos.argos_translate,
        "get_translation_from_codes",
        lambda src, dst: DummyTranslator(),
    )
    monkeypatch.setattr(
        translate_argos.argos_translate, "load_installed_languages", lambda: None
    )
    monkeypatch.setattr(translate_argos, "contains_english", lambda s: False)
    monkeypatch.setattr(subprocess, "run", lambda *a, **k: DummyCompleted())
    monkeypatch.setattr(
        sys,
        "argv",
        [
            "translate_argos.py",
            target_rel,
            "--to",
            "xx",
            "--root",
            str(root),
            "--run-dir",
            str(run_dir),
            "--overwrite",
        ],
    )

    translate_argos.main()

    assert (run_dir / "translate.log").is_file()
    assert (run_dir / "skipped.csv").is_file()
    assert (run_dir / "metrics.json").is_file()


def test_run_dir_redirects_paths(tmp_path, monkeypatch):
    root = tmp_path
    messages_dir = root / "Resources" / "Localization" / "Messages"
    messages_dir.mkdir(parents=True)
    (messages_dir / "English.json").write_text(
        json.dumps({"Messages": {"hash": "Hello"}})
    )

    target_rel = "Resources/Localization/Messages/Test.json"
    run_dir = root / "logs"
    external = root / "external"
    external.mkdir()

    class DummyTranslator:
        def translate(self, text):
            return "Bonjour"

    class DummyCompleted:
        def __init__(self, code=0):
            self.returncode = code

    monkeypatch.setattr(
        translate_argos.argos_translate,
        "get_translation_from_codes",
        lambda src, dst: DummyTranslator(),
    )
    monkeypatch.setattr(
        translate_argos.argos_translate, "load_installed_languages", lambda: None
    )
    monkeypatch.setattr(translate_argos, "contains_english", lambda s: False)
    monkeypatch.setattr(subprocess, "run", lambda *a, **k: DummyCompleted())
    monkeypatch.setattr(
        sys,
        "argv",
        [
            "translate_argos.py",
            target_rel,
            "--to",
            "xx",
            "--root",
            str(root),
            "--run-dir",
            str(run_dir),
            "--log-file",
            str(external / "custom.log"),
            "--report-file",
            str(external / "custom.csv"),
            "--metrics-file",
            str(external / "custom_metrics.json"),
            "--overwrite",
        ],
    )

    translate_argos.main()

    # All outputs should reside in run_dir regardless of the explicit paths
    assert (run_dir / "custom.log").is_file()
    assert not (external / "custom.log").exists()
    assert (run_dir / "custom.csv").is_file()
    assert not (external / "custom.csv").exists()
    assert (run_dir / "custom_metrics.json").is_file()
    assert not (external / "custom_metrics.json").exists()


def test_summary_and_success_logged_at_info(tmp_path, monkeypatch, caplog):
    root = tmp_path
    messages_dir = root / "Resources" / "Localization" / "Messages"
    messages_dir.mkdir(parents=True)
    (messages_dir / "English.json").write_text(
        json.dumps({"Messages": {"hash": "Hello"}})
    )

    target_rel = "Resources/Localization/Messages/Test.json"

    class DummyTranslator:
        def translate(self, text):
            return "Bonjour"

    class DummyCompleted:
        def __init__(self, code=0):
            self.returncode = code

    monkeypatch.setattr(
        translate_argos.argos_translate,
        "get_translation_from_codes",
        lambda src, dst: DummyTranslator(),
    )
    monkeypatch.setattr(
        translate_argos.argos_translate, "load_installed_languages", lambda: None
    )
    monkeypatch.setattr(translate_argos, "contains_english", lambda s: False)
    monkeypatch.setattr(subprocess, "run", lambda *a, **k: DummyCompleted())
    monkeypatch.setattr(
        sys,
        "argv",
        [
            "translate_argos.py",
            target_rel,
            "--to",
            "xx",
            "--root",
            str(root),
            "--overwrite",
            "--log-level",
            "INFO",
        ],
    )

    with caplog.at_level(logging.INFO, logger="translate_argos"):
        translate_argos.main()

    info_msgs = [rec.message for rec in caplog.records if rec.levelname == "INFO"]
    assert any("Processed" in m for m in info_msgs)
    assert any("Report breakdown" in m for m in info_msgs)
    assert any("Summary:" in m for m in info_msgs)
    assert any("Wrote translations to" in m for m in info_msgs)

    warn_msgs = [rec.message for rec in caplog.records if rec.levelname == "WARNING"]
    assert not any(
        "Processed" in m
        or "Report breakdown" in m
        or "Summary:" in m
        or "Wrote translations to" in m
        for m in warn_msgs
    )


def test_dry_run_reports_mismatches(tmp_path, monkeypatch):
    root = tmp_path
    messages_dir = root / "Resources" / "Localization" / "Messages"
    messages_dir.mkdir(parents=True)
    english = {"Messages": {"h1": "Hello {0} {1}", "h2": "Hi"}}
    (messages_dir / "English.json").write_text(json.dumps(english))

    target_rel = "Resources/Localization/Messages/Test.json"
    target = {"Messages": {"h1": "Bonjour {1} {0}", "h2": "Salut"}}
    (root / target_rel).write_text(json.dumps(target))

    run_dir = root / "run"

    def fail_get_translation(*_a, **_k):  # pragma: no cover - ensure not called
        raise AssertionError("Argos should not be called in dry run")

    monkeypatch.setattr(
        translate_argos.argos_translate, "get_translation_from_codes", fail_get_translation
    )
    monkeypatch.setattr(translate_argos, "contains_english", lambda s: False)
    monkeypatch.setattr(
        sys,
        "argv",
        [
            "translate_argos.py",
            target_rel,
            "--to",
            "xx",
            "--root",
            str(root),
            "--run-dir",
            str(run_dir),
            "--dry-run",
        ],
    )

    translate_argos.main()

    report_rows = list(csv.DictReader((run_dir / "skipped.csv").open()))
    assert report_rows == []

    metrics = json.loads((run_dir / "metrics.json").read_text())
    entry = metrics[-1]
    assert entry["dry_run"] is True
    assert entry["processed"] == 2
    assert entry["token_reorders"] == 0
    assert entry["successes"] == 2


def test_exit_when_translation_engine_missing(tmp_path, monkeypatch, caplog):
    root = tmp_path
    messages_dir = root / "Resources" / "Localization" / "Messages"
    messages_dir.mkdir(parents=True)
    (messages_dir / "English.json").write_text(
        json.dumps({"Messages": {"hash": "Hello"}})
    )

    target_rel = "Resources/Localization/Messages/Test.json"

    class DummyCompleted:
        def __init__(self, code=0):
            self.returncode = code

    monkeypatch.setattr(
        translate_argos.argos_translate,
        "get_translation_from_codes",
        lambda src, dst: None,
    )
    monkeypatch.setattr(
        translate_argos.argos_translate, "load_installed_languages", lambda: None
    )
    monkeypatch.setattr(translate_argos, "contains_english", lambda s: False)
    monkeypatch.setattr(subprocess, "run", lambda *a, **k: DummyCompleted())
    monkeypatch.setattr(
        sys,
        "argv",
        [
            "translate_argos.py",
            target_rel,
            "--to",
            "xx",
            "--root",
            str(root),
            "--overwrite",
            "--lenient-tokens",
        ],
    )

    with caplog.at_level("ERROR"):
        with pytest.raises(SystemExit) as exc:
            translate_argos.main()
    msg = "No Argos translation model for en->xx"
    assert msg in str(exc.value)
    assert msg in caplog.text
    install_hint = "argospm install translate-en_xx"
    assert install_hint in str(exc.value)
    assert install_hint in caplog.text
    rebuild_hint = "cd Resources/Localization/Models/xx"
    assert rebuild_hint in str(exc.value)
    assert rebuild_hint in caplog.text


def test_exit_when_translation_engine_attribute_error(tmp_path, monkeypatch, caplog):
    root = tmp_path
    messages_dir = root / "Resources" / "Localization" / "Messages"
    messages_dir.mkdir(parents=True)
    (messages_dir / "English.json").write_text(
        json.dumps({"Messages": {"hash": "Hello"}})
    )

    target_rel = "Resources/Localization/Messages/Test.json"

    class DummyCompleted:
        def __init__(self, code=0):
            self.returncode = code

    def raiser(src, dst):
        raise AttributeError("engine missing")

    monkeypatch.setattr(
        translate_argos.argos_translate,
        "get_translation_from_codes",
        raiser,
    )
    monkeypatch.setattr(
        translate_argos.argos_translate, "load_installed_languages", lambda: None
    )
    monkeypatch.setattr(translate_argos, "contains_english", lambda s: False)
    monkeypatch.setattr(subprocess, "run", lambda *a, **k: DummyCompleted())
    monkeypatch.setattr(
        sys,
        "argv",
        [
            "translate_argos.py",
            target_rel,
            "--to",
            "xx",
            "--root",
            str(root),
            "--overwrite",
            "--lenient-tokens",
        ],
    )

    with caplog.at_level("ERROR"):
        with pytest.raises(SystemExit) as exc:
            translate_argos.main()
    msg = "No Argos translation model for en->xx"
    assert msg in str(exc.value)
    assert msg in caplog.text
    install_hint = "argospm install translate-en_xx"
    assert install_hint in str(exc.value)
    assert install_hint in caplog.text
    rebuild_hint = "cd Resources/Localization/Models/xx"
    assert rebuild_hint in str(exc.value)
    assert rebuild_hint in caplog.text


def test_missing_model_logs_install_hint(tmp_path, monkeypatch, caplog):
    root = tmp_path
    target_rel = "Resources/Localization/Messages/Spanish.json"

    monkeypatch.setattr(
        translate_argos.argos_translate,
        "get_translation_from_codes",
        lambda src, dst: None,
    )
    monkeypatch.setattr(
        translate_argos.argos_translate, "load_installed_languages", lambda: None
    )
    monkeypatch.setattr(
        sys,
        "argv",
        [
            "translate_argos.py",
            target_rel,
            "--to",
            "es",
            "--root",
            str(root),
        ],
    )

    with caplog.at_level("ERROR"):
        with pytest.raises(SystemExit) as exc:
            translate_argos.main()
    hint = "argospm install translate-en_es"
    assert hint in str(exc.value)
    assert hint in caplog.text
    rebuild_hint = "cd Resources/Localization/Models/es"
    assert rebuild_hint in str(exc.value)
    assert rebuild_hint in caplog.text


def test_translates_only_specified_hashes(tmp_path, monkeypatch):
    root = tmp_path
    messages_dir = root / "Resources" / "Localization" / "Messages"
    messages_dir.mkdir(parents=True)
    english = {"Messages": {"h1": "Hello", "h2": "World"}}
    (messages_dir / "English.json").write_text(json.dumps(english))

    target_rel = "Resources/Localization/Messages/Test.json"
    # existing translations to verify only h1 is replaced
    (root / target_rel).write_text(
        json.dumps({"Messages": {"h1": "Bonjour", "h2": "Monde"}})
    )

    class DummyTranslator:
        def translate(self, text):
            return "Hola" if text == "Hello" else "Mundo"

    class DummyCompleted:
        def __init__(self, code=0):
            self.returncode = code

    monkeypatch.setattr(
        translate_argos.argos_translate,
        "get_translation_from_codes",
        lambda src, dst: DummyTranslator(),
    )
    monkeypatch.setattr(
        translate_argos.argos_translate, "load_installed_languages", lambda: None
    )
    monkeypatch.setattr(translate_argos, "contains_english", lambda s: False)
    monkeypatch.setattr(subprocess, "run", lambda *a, **k: DummyCompleted())
    monkeypatch.setattr(
        sys,
        "argv",
        [
            "translate_argos.py",
            target_rel,
            "--to",
            "xx",
            "--root",
            str(root),
            "--hash",
            "h1",
        ],
    )

    translate_argos.main()

    data = json.loads((root / target_rel).read_text())
    assert data["Messages"]["h1"] == "Hola"
    assert data["Messages"]["h2"] == "Monde"


def test_exit_on_fix_tokens_failure(tmp_path, monkeypatch):
    root = tmp_path
    messages_dir = root / "Resources" / "Localization" / "Messages"
    messages_dir.mkdir(parents=True)
    (messages_dir / "English.json").write_text(
        json.dumps({"Messages": {"hash": "Hello"}})
    )

    target_rel = "Resources/Localization/Messages/Test.json"

    class DummyTranslator:
        def translate(self, text):
            return "Bonjour"

    class DummyCompleted:
        def __init__(self, code=1):
            self.returncode = code

    monkeypatch.setattr(
        translate_argos.argos_translate,
        "get_translation_from_codes",
        lambda src, dst: DummyTranslator(),
    )
    monkeypatch.setattr(
        translate_argos.argos_translate, "load_installed_languages", lambda: None
    )
    monkeypatch.setattr(translate_argos, "contains_english", lambda s: False)
    monkeypatch.setattr(subprocess, "run", lambda *a, **k: DummyCompleted())
    monkeypatch.setattr(
        sys,
        "argv",
        [
            "translate_argos.py",
            target_rel,
            "--to",
            "xx",
            "--root",
            str(root),
            "--overwrite",
            "--lenient-tokens",
        ],
    )

    with pytest.raises(SystemExit) as exc:
        translate_argos.main()
    assert int(exc.value.code) == 1


def test_fallback_to_english_and_reports(tmp_path, monkeypatch):
    root = tmp_path
    messages_dir = root / "Resources" / "Localization" / "Messages"
    messages_dir.mkdir(parents=True)
    (messages_dir / "English.json").write_text(
        json.dumps({"Messages": {"hash": "Hello"}})
    )

    target_rel = "Resources/Localization/Messages/Test.json"
    report_path = root / "skipped.csv"

    class DummyTranslator:
        def translate(self, text):
            return "Hello"

    class DummyCompleted:
        def __init__(self, code=0):
            self.returncode = code

    monkeypatch.setattr(
        translate_argos.argos_translate,
        "get_translation_from_codes",
        lambda src, dst: DummyTranslator(),
    )
    monkeypatch.setattr(
        translate_argos.argos_translate, "load_installed_languages", lambda: None
    )
    monkeypatch.setattr(translate_argos, "contains_english", lambda s: False)
    monkeypatch.setattr(subprocess, "run", lambda *a, **k: DummyCompleted())
    monkeypatch.setattr(
        sys,
        "argv",
        [
            "translate_argos.py",
            target_rel,
            "--to",
            "xx",
            "--root",
            str(root),
            "--report-file",
            str(report_path),
            "--overwrite",
            "--lenient-tokens",
            "--lenient-tokens",
        ],
    )

    with pytest.raises(SystemExit):
        translate_argos.main()
    assert report_path.is_file()
    import csv
    rows = list(csv.DictReader(report_path.open()))
    assert rows[0]["category"] == "identical"
    assert "identical" in rows[0]["reason"]


def test_sentinel_missing_repaired(tmp_path, monkeypatch, caplog):
    root = tmp_path
    messages_dir = root / "Resources" / "Localization" / "Messages"
    messages_dir.mkdir(parents=True)
    (messages_dir / "English.json").write_text(
        json.dumps({"Messages": {"hash": "{name}"}})
    )

    target_rel = "Resources/Localization/Messages/Test.json"
    report_path = root / "skipped.csv"

    class DummyTranslator:
        def translate(self, text):
            return text.replace(f" {token_patterns.TOKEN_SENTINEL}", "")

    class DummyCompleted:
        def __init__(self, code=0):
            self.returncode = code

    monkeypatch.setattr(
        translate_argos.argos_translate,
        "get_translation_from_codes",
        lambda src, dst: DummyTranslator(),
    )
    monkeypatch.setattr(
        translate_argos.argos_translate, "load_installed_languages", lambda: None
    )
    monkeypatch.setattr(translate_argos, "contains_english", lambda s: False)
    monkeypatch.setattr(subprocess, "run", lambda *a, **k: DummyCompleted())
    monkeypatch.setattr(
        translate_argos.logger,
        "setLevel",
        lambda level: logging.Logger.setLevel(translate_argos.logger, logging.DEBUG),
    )
    monkeypatch.setattr(
        sys,
        "argv",
        [
            "translate_argos.py",
            target_rel,
            "--to",
            "xx",
            "--root",
            str(root),
            "--report-file",
            str(report_path),
            "--overwrite",
            "--lenient-tokens",
        ],
    )

    with caplog.at_level(logging.DEBUG, logger="translate_argos"):
        translate_argos.main()
    rows = list(csv.DictReader(report_path.open()))
    assert rows == []
    data = json.loads((root / target_rel).read_text())
    assert data["Messages"]["hash"] == "{name}"
    assert "hash: sentinel missing, reinserting" in caplog.text


def test_sentinel_only_report(tmp_path, monkeypatch):
    root = tmp_path
    messages_dir = root / "Resources" / "Localization" / "Messages"
    messages_dir.mkdir(parents=True)
    (messages_dir / "English.json").write_text(
        json.dumps({"Messages": {"hash": "{name}"}})
    )

    target_rel = "Resources/Localization/Messages/Test.json"
    report_path = root / "skipped.csv"

    class DummyTranslator:
        def translate(self, text):
            return "[[TOKEN_SENTINEL]]"  # sentinel only

    class DummyCompleted:
        def __init__(self, code=0):
            self.returncode = code

    monkeypatch.setattr(
        translate_argos.argos_translate,
        "get_translation_from_codes",
        lambda src, dst: DummyTranslator(),
    )
    monkeypatch.setattr(
        translate_argos.argos_translate, "load_installed_languages", lambda: None
    )
    monkeypatch.setattr(translate_argos, "contains_english", lambda s: False)
    monkeypatch.setattr(subprocess, "run", lambda *a, **k: DummyCompleted())
    monkeypatch.setattr(
        sys,
        "argv",
        [
            "translate_argos.py",
            target_rel,
            "--to",
            "xx",
            "--root",
            str(root),
            "--report-file",
            str(report_path),
            "--overwrite",
            "--lenient-tokens",
        ],
    )

    with pytest.raises(SystemExit) as exc:
        translate_argos.main()
    assert exc.value.code == 1
    rows = list(csv.DictReader(report_path.open()))
    assert rows[0]["category"] == "sentinel"
    assert "sentinel only" in rows[0]["reason"]


def test_placeholder_only_report(tmp_path, monkeypatch):
    root = tmp_path
    messages_dir = root / "Resources" / "Localization" / "Messages"
    messages_dir.mkdir(parents=True)
    (messages_dir / "English.json").write_text(
        json.dumps({"Messages": {"hash": "Hello {0}"}})
    )

    target_rel = "Resources/Localization/Messages/Test.json"
    report_path = root / "skipped.csv"

    class DummyTranslator:
        def translate(self, text):
            return "[[TOKEN_0]]"

    class DummyCompleted:
        def __init__(self, code=0):
            self.returncode = code

    monkeypatch.setattr(
        translate_argos.argos_translate,
        "get_translation_from_codes",
        lambda src, dst: DummyTranslator(),
    )
    monkeypatch.setattr(
        translate_argos.argos_translate, "load_installed_languages", lambda: None
    )
    monkeypatch.setattr(translate_argos, "contains_english", lambda s: False)
    monkeypatch.setattr(subprocess, "run", lambda *a, **k: DummyCompleted())
    monkeypatch.setattr(
        sys,
        "argv",
        [
            "translate_argos.py",
            target_rel,
            "--to",
            "xx",
            "--root",
            str(root),
            "--report-file",
            str(report_path),
            "--overwrite",
            "--lenient-tokens",
        ],
    )

    with pytest.raises(SystemExit) as exc:
        translate_argos.main()
    assert exc.value.code == 1
    rows = list(csv.DictReader(report_path.open()))
    assert rows[0]["category"] == "placeholder"
    data = json.loads((root / target_rel).read_text())
    assert data["Messages"]["hash"] == "Hello {0}"


def test_token_mismatch_report(tmp_path, monkeypatch):
    root = tmp_path
    messages_dir = root / "Resources" / "Localization" / "Messages"
    messages_dir.mkdir(parents=True)
    (messages_dir / "English.json").write_text(
        json.dumps({"Messages": {"hash": "Hello {0} {1}"}})
    )

    target_rel = "Resources/Localization/Messages/Test.json"
    report_path = root / "skipped.csv"

    class DummyTranslator:
        def translate(self, text):
            parts = text.split()
            return f"Bonjour {parts[1]}"  # drop second token

    class DummyCompleted:
        def __init__(self, code=0):
            self.returncode = code

    monkeypatch.setattr(
        translate_argos.argos_translate,
        "get_translation_from_codes",
        lambda src, dst: DummyTranslator(),
    )
    monkeypatch.setattr(
        translate_argos.argos_translate, "load_installed_languages", lambda: None
    )
    monkeypatch.setattr(translate_argos, "contains_english", lambda s: False)
    monkeypatch.setattr(subprocess, "run", lambda *a, **k: DummyCompleted())
    monkeypatch.setattr(
        sys,
        "argv",
        [
            "translate_argos.py",
            target_rel,
            "--to",
            "xx",
            "--root",
            str(root),
            "--report-file",
            str(report_path),
            "--overwrite",
            "--lenient-tokens",
        ],
    )

    translate_argos.main()
    rows = list(csv.DictReader(report_path.open()))
    assert rows == []
    data = json.loads((root / target_rel).read_text())
    assert data["Messages"]["hash"] == "Bonjour {0} {1}"


def test_contains_english_report(tmp_path, monkeypatch):
    root = tmp_path
    messages_dir = root / "Resources" / "Localization" / "Messages"
    messages_dir.mkdir(parents=True)
    (messages_dir / "English.json").write_text(
        json.dumps({"Messages": {"hash": "Hello"}})
    )

    target_rel = "Resources/Localization/Messages/Test.json"
    report_path = root / "skipped.csv"

    class DummyTranslator:
        def translate(self, text):
            return "Bonjour Hello"

    class DummyCompleted:
        def __init__(self, code=0):
            self.returncode = code

    monkeypatch.setattr(
        translate_argos.argos_translate,
        "get_translation_from_codes",
        lambda src, dst: DummyTranslator(),
    )
    monkeypatch.setattr(
        translate_argos.argos_translate, "load_installed_languages", lambda: None
    )
    monkeypatch.setattr(translate_argos, "contains_english", lambda s: "Hello" in s)
    monkeypatch.setattr(subprocess, "run", lambda *a, **k: DummyCompleted())
    monkeypatch.setattr(
        sys,
        "argv",
        [
            "translate_argos.py",
            target_rel,
            "--to",
            "xx",
            "--root",
            str(root),
            "--report-file",
            str(report_path),
            "--overwrite",
        ],
    )

    with pytest.raises(SystemExit) as exc:
        translate_argos.main()
    assert exc.value.code == 1
    rows = list(csv.DictReader(report_path.open()))
    assert rows[0]["category"] == "english"
    assert "English" in rows[0]["reason"]


def test_missing_tokens_reinjected(tmp_path, monkeypatch):
    root = tmp_path
    messages_dir = root / "Resources" / "Localization" / "Messages"
    messages_dir.mkdir(parents=True)
    (messages_dir / "English.json").write_text(
        json.dumps({"Messages": {"hash": "Hello <b>world</b> {name}"}})
    )

    target_rel = "Resources/Localization/Messages/Test.json"

    class DummyTranslator:
        def translate(self, text):
            return "Bonjour"

    monkeypatch.setattr(
        translate_argos.argos_translate,
        "get_translation_from_codes",
        lambda src, dst: DummyTranslator(),
    )
    monkeypatch.setattr(
        translate_argos.argos_translate, "load_installed_languages", lambda: None
    )
    monkeypatch.setattr(translate_argos, "contains_english", lambda s: False)
    monkeypatch.setattr(
        sys,
        "argv",
        [
            "translate_argos.py",
            target_rel,
            "--to",
            "xx",
            "--root",
            str(root),
            "--overwrite",
            "--lenient-tokens",
        ],
    )

    translate_argos.main()

    data = json.loads((root / target_rel).read_text())
    value = data["Messages"]["hash"]
    assert "<b>" in value and "</b>" in value and "{name}" in value


def test_strict_retry_succeeds(tmp_path, monkeypatch):
    root = tmp_path
    messages_dir = root / "Resources" / "Localization" / "Messages"
    messages_dir.mkdir(parents=True)
    (messages_dir / "English.json").write_text(
        json.dumps({"Messages": {"hash": "<b>Hello</b>"}})
    )

    target_rel = "Resources/Localization/Messages/Test.json"
    report_path = root / "skipped.csv"

    class DummyTranslator:
        def __init__(self):
            self.calls = 0

        def translate(self, text):
            self.calls += 1
            ids = token_patterns.TOKEN_RE.findall(text)
            if self.calls == 1:
                return f"[[TOKEN_{ids[1]}]]Bonjour"
            return f"[[TOKEN_{ids[0]}]]Bonjour[[TOKEN_{ids[1]}]]"

    class DummyCompleted:
        def __init__(self, code=0):
            self.returncode = code

    monkeypatch.setattr(
        translate_argos.argos_translate,
        "get_translation_from_codes",
        lambda src, dst: DummyTranslator(),
    )
    monkeypatch.setattr(
        translate_argos.argos_translate, "load_installed_languages", lambda: None
    )
    monkeypatch.setattr(translate_argos, "contains_english", lambda s: False)
    monkeypatch.setattr(subprocess, "run", lambda *a, **k: DummyCompleted())
    monkeypatch.setattr(
        sys,
        "argv",
        [
            "translate_argos.py",
            target_rel,
            "--to",
            "xx",
            "--root",
            str(root),
            "--report-file",
            str(report_path),
            "--lenient-tokens",
            "--overwrite",
        ],
    )

    translate_argos.main()
    data = json.loads((root / target_rel).read_text())
    assert data["Messages"]["hash"] == "<b>Bonjour</b>"
    assert report_path.read_text().strip().splitlines() == [
        "hash,english,reason,category"
    ]


def test_sloppy_sentinels_cleaned(tmp_path, monkeypatch):
    root = tmp_path
    messages_dir = root / "Resources" / "Localization" / "Messages"
    messages_dir.mkdir(parents=True)
    (messages_dir / "English.json").write_text(
        json.dumps({"Messages": {"hash": "<b>Hello</b>"}})
    )

    target_rel = "Resources/Localization/Messages/Test.json"
    report_path = root / "skipped.csv"

    class DummyTranslator:
        def translate(self, text):
            ids = token_patterns.TOKEN_RE.findall(text)
            return f"[[ TOKEN_{ids[0]} ]]Bonjour[[ TOKEN_{ids[1]} ]]"

    class DummyCompleted:
        def __init__(self, code=0):
            self.returncode = code

    monkeypatch.setattr(
        translate_argos.argos_translate,
        "get_translation_from_codes",
        lambda src, dst: DummyTranslator(),
    )
    monkeypatch.setattr(
        translate_argos.argos_translate, "load_installed_languages", lambda: None
    )
    monkeypatch.setattr(translate_argos, "contains_english", lambda s: False)
    monkeypatch.setattr(subprocess, "run", lambda *a, **k: DummyCompleted())
    monkeypatch.setattr(
        sys,
        "argv",
        [
            "translate_argos.py",
            target_rel,
            "--to",
            "xx",
            "--root",
            str(root),
            "--report-file",
            str(report_path),
            "--overwrite",
        ],
    )

    translate_argos.main()

    data = json.loads((root / target_rel).read_text())
    assert data["Messages"]["hash"] == "<b>Bonjour</b>"
    assert report_path.read_text().strip().splitlines() == [
        "hash,english,reason,category"
    ]


def test_reorder_tokens_swapped():
    text, changed = translate_argos.reorder_tokens(
        "[[TOKEN_b]] then [[TOKEN_a]]", ["a", "b"]
    )
    assert text == "[[TOKEN_b]] then [[TOKEN_a]]"
    assert changed


def test_normalize_and_reorder_many_tokens():
    ids = [f"{i:x}" for i in range(12)]
    raw = " ".join(f"[[TOKEN_{id}]]" for id in ids)
    normalized = translate_argos.normalize_tokens(raw)
    assert normalized == " ".join(f"[[TOKEN_{id}]]" for id in ids)
    text, changed = translate_argos.reorder_tokens(
        " ".join(f"[[TOKEN_{id}]]" for id in reversed(ids)), ids
    )
    assert text == " ".join(f"[[TOKEN_{id}]]" for id in reversed(ids))
    assert changed


def test_protect_round_trip_with_placeholders():
    text = "Start {0} ${var} [[TOKEN_0]] {(a (b))} End"
    safe, tokens = translate_argos.protect_strict(text)
    normalized = translate_argos.normalize_tokens(safe)
    reordered, changed = translate_argos.reorder_tokens(normalized, list(tokens.keys()))
    assert not changed
    restored = translate_argos.unprotect(reordered, tokens)
    assert restored.replace(" ", "") == text.replace(" ", "")


def test_round_trip_with_reordered_tokens():
    text = "{0} ${var} [[TOKEN_0]] {(x (y))}"
    _, tokens = translate_argos.protect_strict(text)
    ids = list(tokens.keys())
    swapped = (
        f"[[TOKEN_{ids[2]}]] [[TOKEN_{ids[0]}]] [[TOKEN_{ids[1]}]] [[TOKEN_{ids[3]}]]"
    )
    normalized = translate_argos.normalize_tokens(swapped)
    reordered, changed = translate_argos.reorder_tokens(normalized, ids)
    assert changed
    restored = translate_argos.unprotect(reordered, tokens)
    expected = (
        f"{tokens[ids[2]]} {tokens[ids[0]]} {tokens[ids[1]]} {tokens[ids[3]]}"
    )
    assert restored.replace(" ", "") == expected.replace(" ", "")


@pytest.mark.parametrize(
    "text",
    [
        "<tag>",
        "{0}",
        "${value}",
        "[[TOKEN_0]]",
        "mix <b>{0}${val}[[TOKEN_0]]",
    ],
)
def test_tokens_round_trip_without_mismatch(text):
    safe, tokens = translate_argos.protect_strict(text)
    normalized = translate_argos.normalize_tokens(safe)
    reordered, _ = translate_argos.reorder_tokens(normalized, list(tokens.keys()))
    restored = translate_argos.unprotect(reordered, tokens)
    assert restored == text


def test_interpolation_block_translated(tmp_path, monkeypatch):
    root = tmp_path
    messages_dir = root / "Resources" / "Localization" / "Messages"
    messages_dir.mkdir(parents=True)
    msg = "before {(cond ? \"yes\" : \"no\")} after"
    (messages_dir / "English.json").write_text(
        json.dumps({"Messages": {"hash": msg}})
    )

    target_rel = "Resources/Localization/Messages/Test.json"

    class RecordingTranslator:
        def __init__(self):
            self.called = False
            self.seen = ""

        def translate(self, text):
            self.called = True
            self.seen = text
            return text.replace("before", "translated")

    class DummyCompleted:
        def __init__(self, code=0):
            self.returncode = code

    translator = RecordingTranslator()

    monkeypatch.setattr(
        translate_argos.argos_translate,
        "get_translation_from_codes",
        lambda src, dst: translator,
    )
    monkeypatch.setattr(
        translate_argos.argos_translate, "load_installed_languages", lambda: None
    )
    monkeypatch.setattr(translate_argos, "contains_english", lambda s: False)
    monkeypatch.setattr(subprocess, "run", lambda *a, **k: DummyCompleted())
    monkeypatch.setattr(
        sys,
        "argv",
        [
            "translate_argos.py",
            target_rel,
            "--to",
            "xx",
            "--root",
            str(root),
            "--overwrite",
        ],
    )

    translate_argos.main()

    assert translator.called
    assert re.fullmatch(r"before \[\[TOKEN_[0-9a-f]+\]\] after", translator.seen)

    data = json.loads((root / target_rel).read_text())
    assert data["Messages"]["hash"] == "translated {(cond ? \"yes\" : \"no\")} after"


def test_multiple_interpolation_blocks_translated(tmp_path, monkeypatch):
    root = tmp_path
    messages_dir = root / "Resources" / "Localization" / "Messages"
    messages_dir.mkdir(parents=True)
    msg = "before {(a)} middle {(b)} after"
    (messages_dir / "English.json").write_text(
        json.dumps({"Messages": {"hash": msg}})
    )

    target_rel = "Resources/Localization/Messages/Test.json"

    class RecordingTranslator:
        def __init__(self):
            self.called = False
            self.seen = ""

        def translate(self, text):
            self.called = True
            self.seen = text
            return text.replace("before", "translated")

    class DummyCompleted:
        def __init__(self, code=0):
            self.returncode = code

    translator = RecordingTranslator()

    monkeypatch.setattr(
        translate_argos.argos_translate,
        "get_translation_from_codes",
        lambda src, dst: translator,
    )
    monkeypatch.setattr(
        translate_argos.argos_translate, "load_installed_languages", lambda: None
    )
    monkeypatch.setattr(translate_argos, "contains_english", lambda s: False)
    monkeypatch.setattr(subprocess, "run", lambda *a, **k: DummyCompleted())
    monkeypatch.setattr(
        sys,
        "argv",
        [
            "translate_argos.py",
            target_rel,
            "--to",
            "xx",
            "--root",
            str(root),
            "--overwrite",
        ],
    )

    translate_argos.main()

    assert translator.called
    assert re.fullmatch(r"before \[\[TOKEN_[0-9a-f]+\]\] middle \[\[TOKEN_[0-9a-f]+\]\] after", translator.seen)

    data = json.loads((root / target_rel).read_text())
    assert (
        data["Messages"]["hash"]
        == "translated {(a)} middle {(b)} after"
    )



def test_fatal_error_aborts(tmp_path, monkeypatch):
    root = tmp_path
    messages_dir = root / "Resources" / "Localization" / "Messages"
    messages_dir.mkdir(parents=True)
    (messages_dir / "English.json").write_text(
        json.dumps({"Messages": {"hash": "Hello"}})
    )

    target_rel = "Resources/Localization/Messages/Test.json"

    class DummyTranslator:
        def __init__(self):
            self.calls = 0

        def translate(self, text):
            self.calls += 1
            raise Exception("Unsupported model binary version 3")

    translator = DummyTranslator()

    class DummyCompleted:
        def __init__(self, code=0):
            self.returncode = code

    monkeypatch.setattr(
        translate_argos.argos_translate,
        "get_translation_from_codes",
        lambda src, dst: translator,
    )
    monkeypatch.setattr(
        translate_argos.argos_translate, "load_installed_languages", lambda: None
    )
    monkeypatch.setattr(translate_argos, "contains_english", lambda s: False)
    monkeypatch.setattr(subprocess, "run", lambda *a, **k: DummyCompleted())
    monkeypatch.setattr(
        sys,
        "argv",
        [
            "translate_argos.py",
            target_rel,
            "--to",
            "xx",
            "--root",
            str(root),
            "--overwrite",
        ],
    )

    with pytest.raises(SystemExit) as exc:
        translate_argos.main()
    assert "Unsupported model binary version" in str(exc.value)
    assert translator.calls == 1


def test_roundtrip_with_reordered_tokens(tmp_path, monkeypatch):
    root = tmp_path
    messages_dir = root / "Resources" / "Localization" / "Messages"
    messages_dir.mkdir(parents=True)
    english = {"Messages": {"h": "Hello <b>{x}</b> world"}}
    (messages_dir / "English.json").write_text(json.dumps(english))

    target_rel = "Resources/Localization/Messages/Test.json"

    class ReversingTranslator:
        def translate(self, text):
            import re
            tokens = re.findall(r"\[\[TOKEN_\d+\]\]", text)
            tokens.reverse()
            it = iter(tokens)
            return re.sub(r"\[\[TOKEN_\d+\]\]", lambda _m: next(it), text)

    class DummyCompleted:
        def __init__(self, code=0):
            self.returncode = code

    monkeypatch.setattr(
        translate_argos.argos_translate,
        "get_translation_from_codes",
        lambda src, dst: ReversingTranslator(),
    )
    monkeypatch.setattr(
        translate_argos.argos_translate, "load_installed_languages", lambda: None
    )
    monkeypatch.setattr(translate_argos, "contains_english", lambda s: False)
    monkeypatch.setattr(subprocess, "run", lambda *a, **k: DummyCompleted())
    monkeypatch.setattr(
        sys,
        "argv",
        [
            "translate_argos.py",
            target_rel,
            "--to",
            "xx",
            "--root",
            str(root),
            "--overwrite",
        ],
    )

    translate_argos.main()

    data = json.loads((root / target_rel).read_text())
    assert data["Messages"]["h"] == "Hello </b>{x}<b> world"


def test_token_only_line_sentinel_roundtrip(tmp_path, monkeypatch):
    root = tmp_path
    messages_dir = root / "Resources" / "Localization" / "Messages"
    messages_dir.mkdir(parents=True)
    english = {"Messages": {"h": "<b>{x}</b>"}}
    (messages_dir / "English.json").write_text(json.dumps(english))

    target_rel = "Resources/Localization/Messages/Test.json"

    class EchoTranslator:
        def translate(self, text):
            return text

    class DummyCompleted:
        def __init__(self, code=0):
            self.returncode = code

    monkeypatch.setattr(
        translate_argos.argos_translate,
        "get_translation_from_codes",
        lambda src, dst: EchoTranslator(),
    )
    monkeypatch.setattr(
        translate_argos.argos_translate, "load_installed_languages", lambda: None
    )
    monkeypatch.setattr(translate_argos, "contains_english", lambda s: False)
    monkeypatch.setattr(subprocess, "run", lambda *a, **k: DummyCompleted())
    monkeypatch.setattr(
        sys,
        "argv",
        [
            "translate_argos.py",
            target_rel,
            "--to",
            "xx",
            "--root",
            str(root),
            "--overwrite",
        ],
    )

    translate_argos.main()

    data = json.loads((root / target_rel).read_text())
    assert data["Messages"]["h"] == "<b>{x}</b>"



def test_metrics_file_records_failure_reason(tmp_path, monkeypatch):
    root = tmp_path
    messages_dir = root / "Resources" / "Localization" / "Messages"
    messages_dir.mkdir(parents=True)
    (messages_dir / "English.json").write_text(
        json.dumps({"Messages": {"hash": "Hello"}})
    )

    target_rel = "Resources/Localization/Messages/Test.json"

    class DummyTranslator:
        def translate(self, text):
            return "Hello"

    class DummyCompleted:
        def __init__(self, code=0):
            self.returncode = code

    monkeypatch.setattr(
        translate_argos.argos_translate,
        "get_translation_from_codes",
        lambda src, dst: DummyTranslator(),
    )
    monkeypatch.setattr(
        translate_argos.argos_translate, "load_installed_languages", lambda: None
    )
    monkeypatch.setattr(translate_argos, "contains_english", lambda s: False)
    monkeypatch.setattr(subprocess, "run", lambda *a, **k: DummyCompleted())
    monkeypatch.setattr(
        sys,
        "argv",
        [
            "translate_argos.py",
            target_rel,
            "--to",
            "xx",
            "--root",
            str(root),
            "--overwrite",
        ],
    )

    with pytest.raises(SystemExit) as exc:
        translate_argos.main()
    assert exc.value.code == 1

    metrics_files = list((root / "translations" / "xx").glob("*/metrics.json"))
    assert len(metrics_files) == 1
    data = json.loads(metrics_files[0].read_text())
    entry = data[-1]
    assert "model_version" in entry
    assert entry["run_id"]
    assert entry["git_commit"] == "unknown"
    assert entry["python_version"] == platform.python_version()
    assert entry["argos_version"]
    assert entry["metrics_file"] == str(metrics_files[0])
    assert Path(entry["log_file"]).is_file()
    assert Path(entry["report_file"]).is_file()
    assert Path(entry["run_dir"]).is_dir()
    assert entry["cli_args"]["dst"] == "xx"
    assert entry["processed"] == 1
    assert entry["successes"] == 0
    assert entry["timeouts"] == 0
    assert entry["token_reorders"] == 0
    assert "identical" in entry["failures"]["hash"].lower()
    stats = entry["hash_stats"]["hash"]
    assert stats["original_tokens"] == 0
    assert stats["translated_tokens"] == 0
    assert not stats["reordered"]


def test_metrics_file_records_timeout(tmp_path, monkeypatch):
    root = tmp_path
    messages_dir = root / "Resources" / "Localization" / "Messages"
    messages_dir.mkdir(parents=True)
    (messages_dir / "English.json").write_text(
        json.dumps({"Messages": {"hash": "Hello"}})
    )

    target_rel = "Resources/Localization/Messages/Test.json"
    metrics_path = root / "metrics.json"

    class DummyTranslator:
        pass

    class DummyCompleted:
        def __init__(self, code=0):
            self.returncode = code

    def fake_translate_batch(translator, lines, *, max_retries, timeout):
        return ["" for _ in lines], list(range(len(lines)))

    monkeypatch.setattr(
        translate_argos.argos_translate,
        "get_translation_from_codes",
        lambda src, dst: DummyTranslator(),
    )
    monkeypatch.setattr(
        translate_argos.argos_translate, "load_installed_languages", lambda: None
    )
    monkeypatch.setattr(translate_argos, "contains_english", lambda s: False)
    monkeypatch.setattr(subprocess, "run", lambda *a, **k: DummyCompleted())
    monkeypatch.setattr(translate_argos, "translate_batch", fake_translate_batch)
    monkeypatch.setattr(
        sys,
        "argv",
        [
            "translate_argos.py",
            target_rel,
            "--to",
            "xx",
            "--root",
            str(root),
            "--metrics-file",
            str(metrics_path),
            "--overwrite",
        ],
    )

    with pytest.raises(SystemExit) as exc:
        translate_argos.main()
    assert exc.value.code == 1

    data = json.loads(metrics_path.read_text())
    entry = data[-1]
    assert entry["python_version"] == platform.python_version()
    assert entry["argos_version"]
    assert entry["metrics_file"] == str(metrics_path)
    assert Path(entry["log_file"]).is_file()
    assert Path(entry["report_file"]).is_file()
    assert entry["processed"] == 1
    assert entry["successes"] == 0
    assert entry["timeouts"] == 1
    assert entry["token_reorders"] == 0
    reason = next(iter(entry["failures"].values()))
    assert "timeout" in reason
    stats = entry["hash_stats"]["hash"]
    assert stats["original_tokens"] == 0
    assert stats["translated_tokens"] == 0
    assert not stats["reordered"]


def test_metrics_file_counts_token_reorders(tmp_path, monkeypatch):
    root = tmp_path
    messages_dir = root / "Resources" / "Localization" / "Messages"
    messages_dir.mkdir(parents=True)
    english = {"Messages": {"hash": "Hello {0} {1}"}}
    (messages_dir / "English.json").write_text(json.dumps(english))

    target_rel = "Resources/Localization/Messages/Test.json"
    metrics_path = root / "metrics.json"

    class DummyTranslator:
        def translate(self, text):
            ids = token_patterns.TOKEN_RE.findall(text)
            return f"Hola [[TOKEN_{ids[1]}]], [[TOKEN_{ids[0]}]]"

    class DummyCompleted:
        def __init__(self, code=0):
            self.returncode = code

    monkeypatch.setattr(
        translate_argos.argos_translate,
        "get_translation_from_codes",
        lambda src, dst: DummyTranslator(),
    )
    monkeypatch.setattr(
        translate_argos.argos_translate, "load_installed_languages", lambda: None
    )
    monkeypatch.setattr(translate_argos, "contains_english", lambda s: False)
    monkeypatch.setattr(subprocess, "run", lambda *a, **k: DummyCompleted())
    monkeypatch.setattr(
        sys,
        "argv",
        [
            "translate_argos.py",
            target_rel,
            "--to",
            "xx",
            "--root",
            str(root),
            "--metrics-file",
            str(metrics_path),
            "--overwrite",
        ],
    )

    translate_argos.main()

    data = json.loads(metrics_path.read_text())
    entry = data[-1]
    assert entry["processed"] == 1
    assert entry["successes"] == 1
    assert entry["timeouts"] == 0
    assert entry["token_reorders"] == 1
    assert entry["failures"] == {}
    stats = entry["hash_stats"]["hash"]
    assert stats["original_tokens"] == 2
    assert stats["translated_tokens"] == 2
    assert stats["reordered"]


def test_token_only_lines_restored_without_skip(tmp_path, monkeypatch):
    root = tmp_path
    messages_dir = root / "Resources" / "Localization" / "Messages"
    messages_dir.mkdir(parents=True)
    english = {"Messages": {"h1": "{0}"}}
    (messages_dir / "English.json").write_text(json.dumps(english))

    target_rel = "Resources/Localization/Messages/Test.json"
    (root / target_rel).write_text(json.dumps({"Messages": {}}))
    report_path = root / "report.json"

    class DummyTranslator:
        def translate(self, text):
            return text

    class DummyCompleted:
        def __init__(self, code=0):
            self.returncode = code

    monkeypatch.setattr(
        translate_argos.argos_translate,
        "get_translation_from_codes",
        lambda src, dst: DummyTranslator(),
    )
    monkeypatch.setattr(
        translate_argos.argos_translate, "load_installed_languages", lambda: None
    )
    monkeypatch.setattr(translate_argos, "contains_english", lambda s: False)

    called = {}

    def fake_run(args, *a, **k):
        called["args"] = args
        return DummyCompleted()

    monkeypatch.setattr(subprocess, "run", fake_run)
    monkeypatch.setattr(
        sys,
        "argv",
        [
            "translate_argos.py",
            target_rel,
            "--to",
            "xx",
            "--root",
            str(root),
            "--report-file",
            str(report_path),
            "--log-level",
            "INFO",
        ],
    )

    translate_argos.main()

    data = json.loads((root / target_rel).read_text())
    assert data["Messages"]["h1"] == "{0}"

    report = json.loads(report_path.read_text())
    assert report == []
    assert "--reorder" in called.get("args", [])


def test_retry_loop_resolves_skipped_translation(tmp_path, monkeypatch):
    root = tmp_path
    messages_dir = root / "Resources" / "Localization" / "Messages"
    messages_dir.mkdir(parents=True)
    (messages_dir / "English.json").write_text(
        json.dumps({"Messages": {"h": "Hello"}})
    )

    target_rel = "Resources/Localization/Messages/Test.json"
    report_path = root / "skipped.csv"

    class DummyTranslator:
        def __init__(self):
            self.calls = 0

        def translate(self, text):
            self.calls += 1
            return "Hello" if self.calls == 1 else "Bonjour"

    translator = DummyTranslator()

    class DummyCompleted:
        def __init__(self, code=0):
            self.returncode = code

    monkeypatch.setattr(
        translate_argos.argos_translate,
        "get_translation_from_codes",
        lambda src, dst: translator,
    )
    monkeypatch.setattr(
        translate_argos.argos_translate, "load_installed_languages", lambda: None
    )
    monkeypatch.setattr(translate_argos, "contains_english", lambda s: False)
    monkeypatch.setattr(subprocess, "run", lambda *a, **k: DummyCompleted())
    monkeypatch.setattr(
        sys,
        "argv",
        [
            "translate_argos.py",
            target_rel,
            "--to",
            "xx",
            "--root",
            str(root),
            "--report-file",
            str(report_path),
            "--overwrite",
        ],
    )

    translate_argos.main()

    data = json.loads((root / target_rel).read_text())
    assert data["Messages"]["h"] == "Bonjour"
    rows = list(csv.DictReader(report_path.open()))
    assert rows == []
    assert translator.calls == 2


def test_report_written_on_exception(tmp_path, monkeypatch):
    root = tmp_path
    messages_dir = root / "Resources" / "Localization" / "Messages"
    messages_dir.mkdir(parents=True)
    english = {"Messages": {"h1": "Hello", "h2": "World"}}
    (messages_dir / "English.json").write_text(json.dumps(english))

    target_rel = "Resources/Localization/Messages/Test.json"
    report_path = root / "skipped.csv"

    class DummyTranslator:
        def translate(self, text):
            return text

    class DummyCompleted:
        def __init__(self, code=0):
            self.returncode = code

    monkeypatch.setattr(
        translate_argos.argos_translate,
        "get_translation_from_codes",
        lambda src, dst: DummyTranslator(),
    )
    monkeypatch.setattr(
        translate_argos.argos_translate, "load_installed_languages", lambda: None
    )
    monkeypatch.setattr(translate_argos, "contains_english", lambda s: False)
    monkeypatch.setattr(subprocess, "run", lambda *a, **k: DummyCompleted())

    original_unprotect = translate_argos.unprotect
    call_count = {"n": 0}

    def flaky_unprotect(text, tokens):
        call_count["n"] += 1
        if call_count["n"] == 2:
            raise RuntimeError("boom")
        return original_unprotect(text, tokens)

    monkeypatch.setattr(translate_argos, "unprotect", flaky_unprotect)

    monkeypatch.setattr(
        sys,
        "argv",
        [
            "translate_argos.py",
            target_rel,
            "--to",
            "xx",
            "--root",
            str(root),
            "--report-file",
            str(report_path),
            "--overwrite",
        ],
    )

    with pytest.raises(SystemExit) as exc:
        translate_argos.main()
    assert exc.value.code == 1

    assert report_path.is_file()
    rows = list(csv.DictReader(report_path.open()))
    hashes = {row["hash"] for row in rows}
    assert "h2" in hashes


def test_report_cleared_between_runs(tmp_path, monkeypatch):
    root = tmp_path
    messages_dir = root / "Resources" / "Localization" / "Messages"
    messages_dir.mkdir(parents=True)
    english = {"Messages": {"h1": "Hello"}}
    (messages_dir / "English.json").write_text(json.dumps(english))

    target_rel = "Resources/Localization/Messages/Test.json"
    (root / target_rel).write_text(json.dumps({"Messages": {}}))
    report_path = root / "skipped.csv"

    # Pre-populate with stale data to ensure it is cleared
    report_path.write_text(
        "hash,english,reason,category\nold,Old,old reason,oldcat\n"
    )

    class DummyTranslator:
        def translate(self, text):
            return text + "!"

    class DummyCompleted:
        def __init__(self, code=0):
            self.returncode = code

    monkeypatch.setattr(
        translate_argos.argos_translate,
        "get_translation_from_codes",
        lambda src, dst: DummyTranslator(),
    )
    monkeypatch.setattr(
        translate_argos.argos_translate, "load_installed_languages", lambda: None
    )
    monkeypatch.setattr(translate_argos, "contains_english", lambda s: True)
    monkeypatch.setattr(subprocess, "run", lambda *a, **k: DummyCompleted())
    monkeypatch.setattr(
        sys,
        "argv",
        [
            "translate_argos.py",
            target_rel,
            "--to",
            "xx",
            "--root",
            str(root),
            "--report-file",
            str(report_path),
            "--log-level",
            "INFO",
        ],
    )

    with pytest.raises(SystemExit) as exc:
        translate_argos.main()
    assert exc.value.code == 1

    rows = list(csv.DictReader(report_path.open()))
    assert any(row["hash"] == "h1" for row in rows)
    assert rows[-1]["category"] == "summary"


def test_write_report_deduplicates_rows(tmp_path):
    path = tmp_path / "out.csv"
    rows = [
        {"hash": "a", "english": "Hello", "reason": "r1", "category": "c"},
        {"hash": "a", "english": "Hola", "reason": "r2", "category": "c"},
    ]
    translate_argos._write_report(str(path), rows)
    out_rows = list(csv.DictReader(path.open()))
    assert len(out_rows) == 2
    assert out_rows[0]["reason"] == "r2"
    assert out_rows[1]["category"] == "summary"


def test_write_report_logs_counts(tmp_path, caplog):
    path = tmp_path / "out.csv"
    rows = [
        {"hash": "a", "english": "Hello", "reason": "r1", "category": "c"},
        {"hash": "a", "english": "Hola", "reason": "r2", "category": "c"},
        {"hash": "b", "english": "Hi", "reason": "r3", "category": "c"},
    ]
    with caplog.at_level("INFO", logger="translate_argos"):
        translate_argos._write_report(str(path), rows)
    out_rows = list(csv.DictReader(path.open()))
    assert len(out_rows) == 3
    assert out_rows[-1]["category"] == "summary"
    assert (
        f"Received {len(rows)} rows; deduplicated to {len(out_rows) - 1} rows"
        in caplog.text
    )


def test_write_report_warns_on_failure(tmp_path, monkeypatch, caplog):
    path = tmp_path / "out.csv"
    rows: list[dict[str, str]] = []
    real_open = open
    calls = {"count": 0}

    def flaky_open(*args, **kwargs):
        if args[0] == str(path) and calls["count"] == 0:
            calls["count"] += 1
            raise OSError("fail")
        return real_open(*args, **kwargs)

    monkeypatch.setattr("builtins.open", flaky_open)
    with caplog.at_level("WARNING", logger="translate_argos"):
        translate_argos._write_report(str(path), rows, max_retries=2)
    assert f"Failed to write report to {path} (attempt 1/2)" in caplog.text
    assert path.is_file()


def test_report_written_once_and_deduplicated(tmp_path, monkeypatch, caplog):
    root = tmp_path
    messages_dir = root / "Resources" / "Localization" / "Messages"
    messages_dir.mkdir(parents=True)
    (messages_dir / "English.json").write_text(
        json.dumps({"Messages": {"h1": "Hello", "h2": "Hi"}})
    )

    target_rel = "Resources/Localization/Messages/Test.json"
    report_path = root / "out.csv"

    class DummyTranslator:
        def translate(self, text):
            return text

    class DummyCompleted:
        def __init__(self, code=0):
            self.returncode = code

    monkeypatch.setattr(
        translate_argos.argos_translate,
        "get_translation_from_codes",
        lambda src, dst: DummyTranslator(),
    )
    monkeypatch.setattr(
        translate_argos.argos_translate, "load_installed_languages", lambda: None
    )
    monkeypatch.setattr(translate_argos, "contains_english", lambda s: True)
    monkeypatch.setattr(subprocess, "run", lambda *a, **k: DummyCompleted())

    real_run_translation = translate_argos._run_translation

    def dup_run_translation(args, root):
        (
            rows,
            processed,
            successes,
            skipped,
            failures,
            fix_code,
        ) = real_run_translation(args, root)
        return rows + rows, processed, successes, skipped, failures, fix_code

    monkeypatch.setattr(translate_argos, "_run_translation", dup_run_translation)

    calls: list[list[dict[str, str]]] = []
    real_write_report = translate_argos._write_report

    def counting_write_report(path, rows, **kwargs):
        calls.append(list(rows))
        return real_write_report(path, rows, **kwargs)

    monkeypatch.setattr(translate_argos, "_write_report", counting_write_report)
    monkeypatch.setattr(
        sys,
        "argv",
        [
            "translate_argos.py",
            target_rel,
            "--to",
            "xx",
            "--root",
            str(root),
            "--report-file",
            str(report_path),
            "--log-level",
            "INFO",
        ],
    )

    with caplog.at_level("INFO", logger="translate_argos"):
        with pytest.raises(SystemExit) as exc:
            translate_argos.main()
    assert exc.value.code == 1

    assert len(calls) == 1
    out_rows = list(csv.DictReader(report_path.open()))
    hashes = [row["hash"] for row in out_rows[:-1]]
    assert len(hashes) == len(set(hashes))
    assert (
        f"Wrote skip report to {report_path} with {len(out_rows) - 1} row(s)" in caplog.text
    )


def test_translate_preserves_special_tokens(tmp_path, monkeypatch):
    root = tmp_path
    messages_dir = root / "Resources" / "Localization" / "Messages"
    messages_dir.mkdir(parents=True)
    english_text = "Hello {0} ${var} [[TOKEN_0]] {(a (b))}"
    (messages_dir / "English.json").write_text(
        json.dumps({"Messages": {"h1": english_text}})
    )
    target_rel = "Resources/Localization/Messages/Test.json"
    log_path = root / "out.log"

    class DummyTranslator:
        def translate(self, text):
            return text.replace("Hello", "Bonjour")

    class DummyCompleted:
        def __init__(self, code=0):
            self.returncode = code

    monkeypatch.setattr(
        translate_argos.argos_translate,
        "get_translation_from_codes",
        lambda src, dst: DummyTranslator(),
    )
    monkeypatch.setattr(
        translate_argos.argos_translate, "load_installed_languages", lambda: None
    )
    monkeypatch.setattr(translate_argos, "contains_english", lambda s: False)
    monkeypatch.setattr(subprocess, "run", lambda *a, **k: DummyCompleted())
    monkeypatch.setattr(
        sys,
        "argv",
        [
            "translate_argos.py",
            target_rel,
            "--to",
            "xx",
            "--root",
            str(root),
            "--log-file",
            str(log_path),
            "--overwrite",
        ],
    )

    translate_argos.main()

    log = log_path.read_text().lower()
    assert "token mismatch" not in log
    data = json.loads((root / target_rel).read_text())
    assert (
        data["Messages"]["h1"]
        == "Bonjour {0} ${var} [[TOKEN_0]] {(a (b))}"
    )


def test_handles_bracket_tokens(tmp_path, monkeypatch):
    root = tmp_path
    messages_dir = root / "Resources" / "Localization" / "Messages"
    messages_dir.mkdir(parents=True)
    english_text = "[color=red]"
    (messages_dir / "English.json").write_text(
        json.dumps({"Messages": {"h1": english_text}})
    )
    target_rel = "Resources/Localization/Messages/Test.json"

    class DummyTranslator:
        def translate(self, text):
            return text

    class DummyCompleted:
        def __init__(self, code=0):
            self.returncode = code

    monkeypatch.setattr(
        translate_argos.argos_translate,
        "get_translation_from_codes",
        lambda src, dst: DummyTranslator(),
    )
    monkeypatch.setattr(
        translate_argos.argos_translate, "load_installed_languages", lambda: None
    )
    monkeypatch.setattr(translate_argos, "contains_english", lambda s: False)
    monkeypatch.setattr(subprocess, "run", lambda *a, **k: DummyCompleted())
    monkeypatch.setattr(
        sys,
        "argv",
        [
            "translate_argos.py",
            target_rel,
            "--to",
            "xx",
            "--root",
            str(root),
            "--overwrite",
        ],
    )

    translate_argos.main()

    data = json.loads((root / target_rel).read_text())
    assert data["Messages"]["h1"] == english_text


def test_wraps_and_unwraps_placeholders(tmp_path, monkeypatch):
    root = tmp_path
    messages_dir = root / "Resources" / "Localization" / "Messages"
    messages_dir.mkdir(parents=True)
    english_text = "Hi {0}"
    (messages_dir / "English.json").write_text(
        json.dumps({"Messages": {"h1": english_text}})
    )
    target_rel = "Resources/Localization/Messages/Test.json"

    class RecordingTranslator:
        def __init__(self):
            self.seen: list[str] = []

        def translate(self, text):
            self.seen.append(text)
            return text.replace("Hi", "Bonjour")

    class DummyCompleted:
        def __init__(self, code=0):
            self.returncode = code

    translator = RecordingTranslator()

    monkeypatch.setattr(
        translate_argos.argos_translate,
        "get_translation_from_codes",
        lambda src, dst: translator,
    )
    monkeypatch.setattr(
        translate_argos.argos_translate, "load_installed_languages", lambda: None
    )
    monkeypatch.setattr(translate_argos, "contains_english", lambda s: False)
    monkeypatch.setattr(subprocess, "run", lambda *a, **k: DummyCompleted())
    monkeypatch.setattr(
        sys,
        "argv",
        [
            "translate_argos.py",
            target_rel,
            "--to",
            "xx",
            "--root",
            str(root),
            "--overwrite",
        ],
    )

    translate_argos.main()

    assert translator.seen and re.search(r"\[\[TOKEN_[0-9a-f]+\]\]", translator.seen[0])
    data = json.loads((root / target_rel).read_text())
    assert data["Messages"]["h1"] == "Bonjour {0}"


def test_run_index_appended(tmp_path, monkeypatch):
    root = tmp_path
    messages_dir = root / "Resources" / "Localization" / "Messages"
    messages_dir.mkdir(parents=True)
    (messages_dir / "English.json").write_text(
        json.dumps({"Messages": {"hash": "Hello"}})
    )

    target_rel = "Resources/Localization/Messages/Test.json"

    class DummyTranslator:
        def translate(self, text):
            return "Bonjour"

    class DummyCompleted:
        def __init__(self, code=0):
            self.returncode = code

    monkeypatch.setattr(
        translate_argos.argos_translate,
        "get_translation_from_codes",
        lambda src, dst: DummyTranslator(),
    )
    monkeypatch.setattr(
        translate_argos.argos_translate, "load_installed_languages", lambda: None
    )
    monkeypatch.setattr(translate_argos, "contains_english", lambda s: False)
    monkeypatch.setattr(subprocess, "run", lambda *a, **k: DummyCompleted())
    monkeypatch.setattr(
        sys,
        "argv",
        [
            "translate_argos.py",
            target_rel,
            "--to",
            "xx",
            "--root",
            str(root),
            "--overwrite",
        ],
    )

    translate_argos.main()

    index_path = root / "translations" / "run_index.json"
    assert index_path.is_file()
    data = json.loads(index_path.read_text())
    assert data
    entry = data[-1]
    assert entry["language"] == "xx"
    assert entry["success_rate"] == 1.0
    assert entry["log_file"]
    assert entry["report_file"]
    assert entry["metrics_file"]


def test_stack_trace_logged_on_exception(tmp_path, monkeypatch, caplog):
    root = tmp_path
    messages_dir = root / "Resources" / "Localization" / "Messages"
    messages_dir.mkdir(parents=True)
    (messages_dir / "English.json").write_text(
        json.dumps({"Messages": {"hash": "Hello"}})
    )

    target_rel = "Resources/Localization/Messages/Test.json"

    class FailingTranslator:
        def translate(self, text):
            raise ValueError("boom")

    class DummyCompleted:
        def __init__(self, code=0):
            self.returncode = code

    monkeypatch.setattr(
        translate_argos.argos_translate,
        "get_translation_from_codes",
        lambda src, dst: FailingTranslator(),
    )
    monkeypatch.setattr(
        translate_argos.argos_translate, "load_installed_languages", lambda: None
    )
    monkeypatch.setattr(translate_argos, "contains_english", lambda s: False)
    monkeypatch.setattr(subprocess, "run", lambda *a, **k: DummyCompleted())
    monkeypatch.setattr(
        sys,
        "argv",
        [
            "translate_argos.py",
            target_rel,
            "--to",
            "xx",
            "--root",
            str(root),
            "--overwrite",
            "--max-retries",
            "1",
        ],
    )

    with caplog.at_level(logging.ERROR, logger="translate_argos"):
        with pytest.raises(SystemExit):
            translate_argos.main()

    assert "ValueError: boom" in caplog.text
    assert "Traceback" in caplog.text
