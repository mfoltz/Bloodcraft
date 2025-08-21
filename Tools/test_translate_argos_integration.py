import csv
import json
import subprocess
import sys
import textwrap
from pathlib import Path

import pytest
import translate_argos


def test_multibatch_continues_after_line_error(tmp_path, monkeypatch):
    root = tmp_path
    messages_dir = root / "Resources" / "Localization" / "Messages"
    messages_dir.mkdir(parents=True)
    english = {"Messages": {f"h{i}": f"Line {i}" for i in range(5)}}
    (messages_dir / "English.json").write_text(json.dumps(english))

    target_rel = "Resources/Localization/Messages/Test.json"
    report_path = root / "skipped.csv"

    class DummyTranslator:
        def translate(self, text):
            return text + "_t"

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
            "--batch-size",
            "2",
            "--report-file",
            str(report_path),
            "--overwrite",
        ],
    )

    translate_argos.main()

    assert call_count["n"] >= 2

    data = json.loads((root / target_rel).read_text())
    assert data["Messages"]["h0"] == "Line 0_t"
    assert data["Messages"]["h2"] == "Line 2_t"
    assert data["Messages"]["h4"] == "Line 4_t"
    rows = list(csv.DictReader(report_path.open()))
    assert rows == []


def test_report_written_on_exception(tmp_path, monkeypatch):
    root = tmp_path
    messages_dir = root / "Resources" / "Localization" / "Messages"
    messages_dir.mkdir(parents=True)
    english = {"Messages": {f"h{i}": f"Line {i}" for i in range(2)}}
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
    monkeypatch.setattr(subprocess, "run", lambda *a, **k: DummyCompleted())

    original_unprotect = translate_argos.unprotect
    call_count = {"n": 0}

    def boom_unprotect(text, tokens):
        call_count["n"] += 1
        if call_count["n"] == 2:
            raise SystemExit(1)
        return original_unprotect(text, tokens)

    monkeypatch.setattr(translate_argos, "unprotect", boom_unprotect)

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

    with pytest.raises(SystemExit):
        translate_argos.main()

    rows = list(csv.DictReader(report_path.open()))
    assert [row["hash"] for row in rows[:-1]] == ["h0"]
    assert rows[-1]["category"] == "summary"


def test_report_persisted_when_process_killed(tmp_path):
    root = tmp_path
    messages_dir = root / "Resources" / "Localization" / "Messages"
    messages_dir.mkdir(parents=True)
    english = {"Messages": {f"h{i}": f"Line {i}" for i in range(3)}}
    (messages_dir / "English.json").write_text(json.dumps(english))

    target_rel = "Resources/Localization/Messages/Test.json"
    report_path = root / "skipped.csv"
    tools_dir = Path(__file__).resolve().parent

    script = textwrap.dedent(
        f"""
import os, sys
sys.path.insert(0, {str(tools_dir)!r})
import translate_argos

class DummyTranslator:
    def translate(self, text):
        return text

class DummyCompleted:
    def __init__(self, code=0):
        self.returncode = code

translate_argos.argos_translate.get_translation_from_codes = lambda s, d: DummyTranslator()
translate_argos.argos_translate.load_installed_languages = lambda: None
translate_argos.subprocess.run = lambda *a, **k: DummyCompleted()
translate_argos.contains_english = lambda s: False

original_unprotect = translate_argos.unprotect
call_count = 0

def killer(text, tokens):
    global call_count
    call_count += 1
    if call_count == 3:
        os._exit(1)
    return original_unprotect(text, tokens)

translate_argos.unprotect = killer

sys.argv = [
    "translate_argos.py",
    {target_rel!r},
    "--to",
    "xx",
    "--root",
    {str(root)!r},
    "--batch-size",
    "2",
    "--report-file",
    {str(report_path)!r},
    "--overwrite",
]

translate_argos.main()
"""
    )

    runner = root / "runner.py"
    runner.write_text(script)
    completed = subprocess.run([sys.executable, str(runner)], cwd=root)
    assert completed.returncode != 0

    rows = list(csv.DictReader(report_path.open()))
    assert [row["hash"] for row in rows[:-1]] == ["h0", "h1"]
    assert rows[-1]["category"] == "summary"
