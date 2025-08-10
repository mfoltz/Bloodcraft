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

    monkeypatch.setattr(
        translate_argos.argos_translate,
        "get_translation_from_codes",
        lambda src, dst: DummyTranslator(),
    )
    monkeypatch.setattr(
        translate_argos.argos_translate, "load_installed_languages", lambda: None
    )
    monkeypatch.setattr(translate_argos, "contains_english", lambda s: False)
    monkeypatch.setattr(subprocess, "run", lambda *a, **k: None)
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
