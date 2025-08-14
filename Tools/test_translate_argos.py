import csv
import json
import os
import subprocess
import sys
import tempfile

import pytest

import translate_argos
from translate_argos import ensure_model_installed


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


def test_creates_directories_for_logs_and_reports(tmp_path, monkeypatch):
    root = tmp_path
    messages_dir = root / "Resources" / "Localization" / "Messages"
    messages_dir.mkdir(parents=True)
    (messages_dir / "English.json").write_text(
        json.dumps({"Messages": {"hash": "Hello"}})
    )

    target_rel = "Resources/Localization/Messages/Test.json"
    log_path = root / "logs" / "nested" / "out.log"
    report_path = root / "reports" / "nested" / "report.json"

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
            "--log-file",
            str(log_path),
            "--report-file",
            str(report_path),
            "--overwrite",
        ],
    )

    translate_argos.main()

    assert log_path.is_file()
    assert report_path.is_file()


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
        ],
    )

    with pytest.raises(SystemExit) as exc:
        translate_argos.main()
    assert exc.value.code == 1


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
        ],
    )

    translate_argos.main()
    assert report_path.is_file()
    import csv
    rows = list(csv.DictReader(report_path.open()))
    assert rows[0]["category"] == "untranslated"
    assert "untranslated" in rows[0]["reason"]


def test_sentinel_missing_report(tmp_path, monkeypatch):
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
            return "[[TOKEN_0]]"  # missing sentinel

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
    rows = list(csv.DictReader(report_path.open()))
    assert rows[0]["category"] == "sentinel"
    assert "sentinel" in rows[0]["reason"]


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
        def translate(self, text):
            if "__T0__" in text:
                return "__T0__Bonjour__T1__"
            return "[[TOKEN_1]]Bonjour[[TOKEN_0]]"

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
    text, changed = translate_argos.reorder_tokens("[[TOKEN_1]] then [[TOKEN_0]]", 2)
    assert text == "[[TOKEN_0]] then [[TOKEN_1]]"
    assert changed


def test_reorder_tokens_from_one_based():
    text, changed = translate_argos.reorder_tokens("[[TOKEN_1]] then [[TOKEN_2]]", 2)
    assert text == "[[TOKEN_0]] then [[TOKEN_1]]"
    assert changed
