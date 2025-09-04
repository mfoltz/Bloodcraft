import json
import subprocess
import sys
from pathlib import Path

import pytest

# Ensure the Tools directory is importable
sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import translate_argos


def test_metrics_retained_on_failure(tmp_path, monkeypatch):
    root = tmp_path
    messages_dir = root / "Resources" / "Localization" / "Messages"
    messages_dir.mkdir(parents=True)
    english = {"Messages": {"h1": "Line 1", "h2": "Line 2"}}
    (messages_dir / "English.json").write_text(json.dumps(english))

    target_rel = "Resources/Localization/Messages/Test.json"
    metrics_path = root / "metrics.json"

    class DummyTranslator:
        def translate(self, text):
            return text + "_t"

    class DummyCompleted:
        def __init__(self, code=0):
            self.returncode = code

    monkeypatch.setattr(
        translate_argos.argos_translate,
        "get_translation_from_codes",
        lambda s, d: DummyTranslator(),
    )
    monkeypatch.setattr(
        translate_argos.argos_translate, "load_installed_languages", lambda: None
    )
    monkeypatch.setattr(translate_argos, "contains_english", lambda s: False)
    monkeypatch.setattr(subprocess, "run", lambda *a, **k: DummyCompleted())

    original_write = translate_argos._write_json
    calls = {"n": 0}

    def flaky_write(path, data, *a, **k):
        calls["n"] += 1
        if path.endswith("Test.json") and calls["n"] == 1:
            raise RuntimeError("boom")
        return original_write(path, data, *a, **k)

    monkeypatch.setattr(translate_argos, "_write_json", flaky_write)

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

    with pytest.raises(SystemExit):
        translate_argos.main()

    data = json.loads(metrics_path.read_text())
    entry = data[-1]
    assert entry["status"] == "failed"
    assert entry["processed"] == 2
    assert entry["successes"] == 2
