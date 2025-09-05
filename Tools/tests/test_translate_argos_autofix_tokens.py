import json
import subprocess
import sys
from pathlib import Path

import pytest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import translate_argos


class DummyCompleted:
    def __init__(self, code=0):
        self.returncode = code


def _setup(monkeypatch, root, translator):
    monkeypatch.setattr(
        translate_argos.argos_translate,
        "get_translation_from_codes",
        lambda s, d: translator,
    )
    monkeypatch.setattr(
        translate_argos.argos_translate, "load_installed_languages", lambda: None
    )
    monkeypatch.setattr(translate_argos, "contains_english", lambda s: False)
    monkeypatch.setattr(subprocess, "run", lambda *a, **k: DummyCompleted())
    monkeypatch.setattr(subprocess, "check_output", lambda *a, **k: b"")
    monkeypatch.setattr(
        sys,
        "argv",
        [
            "translate_argos.py",
            "Resources/Localization/Messages/Test.json",
            "--to",
            "xx",
            "--root",
            str(root),
            "--run-dir",
            str(root / "run"),
            "--metrics-file",
            str(root / "metrics.json"),
            "--overwrite",
        ],
    )


def test_missing_token_autofixed(tmp_path, monkeypatch):
    root = tmp_path
    messages_dir = root / "Resources" / "Localization" / "Messages"
    messages_dir.mkdir(parents=True)
    english = {"Messages": {"h1": "Hello [[TOKEN_abcd]]"}}
    (messages_dir / "English.json").write_text(json.dumps(english))

    class MissingTranslator:
        def translate(self, text):
            return text.replace("[[TOKEN_0]]", "")

    _setup(monkeypatch, root, MissingTranslator())

    try:
        translate_argos.main()
    except SystemExit:
        pass

    data = json.loads((root / "run" / "metrics.json").read_text())
    entry = data[-1]
    hstats = entry["hash_stats"]["h1"]
    assert hstats["missing_tokens"] == 1
    assert hstats["translated_tokens"] == 1
    assert entry["token_mismatches"] == 1
    assert entry["tokens_removed"] == 0


def test_extra_token_autofixed(tmp_path, monkeypatch):
    root = tmp_path
    messages_dir = root / "Resources" / "Localization" / "Messages"
    messages_dir.mkdir(parents=True)
    english = {"Messages": {"h1": "Hello [[TOKEN_abcd]]"}}
    (messages_dir / "English.json").write_text(json.dumps(english))

    class ExtraTranslator:
        def translate(self, text):
            return text + " [[TOKEN_deadbeef]]"

    _setup(monkeypatch, root, ExtraTranslator())

    try:
        translate_argos.main()
    except SystemExit:
        pass

    data = json.loads((root / "run" / "metrics.json").read_text())
    entry = data[-1]
    hstats = entry["hash_stats"]["h1"]
    assert hstats["removed_tokens"] == 1
    assert hstats["translated_tokens"] == 1
    assert entry["token_mismatches"] == 1
    assert entry["tokens_removed"] == 1
