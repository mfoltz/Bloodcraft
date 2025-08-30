import json
import sys
import types
import subprocess

import pytest

argos_stub = types.ModuleType("argostranslate")
argos_stub.translate = types.SimpleNamespace(
    get_translation_from_codes=lambda *a, **k: None,
    load_installed_languages=lambda: None,
)
sys.modules.setdefault("argostranslate", argos_stub)

import translate_argos


@pytest.fixture(autouse=True)
def _stub_check_output(monkeypatch):
    def fake_check_output(cmd, *a, **k):
        if cmd and cmd[0] == "git":
            raise subprocess.CalledProcessError(1, cmd)
        return b""

    monkeypatch.setattr(subprocess, "check_output", fake_check_output)


def test_retry_missing_tokens(tmp_path, monkeypatch):
    root = tmp_path
    messages_dir = root / "Resources" / "Localization" / "Messages"
    messages_dir.mkdir(parents=True)
    english = {"Messages": {"hash": "Attack {0}!"}}
    (messages_dir / "English.json").write_text(json.dumps(english))

    target_rel = "Resources/Localization/Messages/Test.json"
    target_path = root / target_rel
    target_path.parent.mkdir(parents=True, exist_ok=True)
    target_path.write_text(json.dumps({"Messages": {"hash": ""}}))

    class RetryTranslator:
        def __init__(self):
            self.calls = 0

        def translate(self, text: str) -> str:
            self.calls += 1
            if '"[[TOKEN_0]]"' in text:
                return "Translated [[TOKEN_0]]!"
            return "Translated tokenless!"

    class DummyCompleted:
        def __init__(self, code=0):
            self.returncode = code

    translator = RetryTranslator()

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
            "--retry-mismatches",
        ],
    )

    translate_argos.main()

    assert translator.calls == 2
    data = json.loads(target_path.read_text())
    assert data["Messages"]["hash"] == "Translated {0}!"
    run_dir = next((root / "translations" / "xx").iterdir())
    metrics = json.loads((run_dir / "metrics.json").read_text())
    stats = metrics[0]["hash_stats"]["hash"]
    assert stats["retry_attempted"]
    assert stats["retry_succeeded"]


def test_fix_order_rewrites_tokens(tmp_path, monkeypatch):
    root = tmp_path
    messages_dir = root / "Resources" / "Localization" / "Messages"
    messages_dir.mkdir(parents=True)
    english = {"Messages": {"hash": "Attack {0} {1}!"}}
    (messages_dir / "English.json").write_text(json.dumps(english))

    target_rel = "Resources/Localization/Messages/Test.json"
    target_path = root / target_rel
    target_path.parent.mkdir(parents=True, exist_ok=True)
    target_path.write_text(json.dumps({"Messages": {"hash": ""}}))

    class SwapTranslator:
        def translate(self, text: str) -> str:
            return "Translated [[TOKEN_1]] [[TOKEN_0]]!"

    class DummyCompleted:
        def __init__(self, code=0):
            self.returncode = code

    translator = SwapTranslator()

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
            "--fix-order",
        ],
    )

    translate_argos.main()

    data = json.loads(target_path.read_text())
    assert data["Messages"]["hash"] == "Translated {0} {1}!"


def test_failure_records_progress(tmp_path, monkeypatch):
    root = tmp_path
    messages_dir = root / "Resources" / "Localization" / "Messages"
    messages_dir.mkdir(parents=True)
    english = {"Messages": {"hash": "Hello"}}
    (messages_dir / "English.json").write_text(json.dumps(english))

    target_rel = "Resources/Localization/Messages/Test.json"
    target_path = root / target_rel
    target_path.parent.mkdir(parents=True, exist_ok=True)
    target_path.write_text(json.dumps({"Messages": {"hash": ""}}))

    class EchoTranslator:
        def translate(self, text: str) -> str:
            return text  # identical to source -> failure

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

    with pytest.raises(SystemExit):
        translate_argos.main()

    run_dir = next((root / "translations" / "xx").iterdir())
    metrics = json.loads((run_dir / "metrics.json").read_text())
    entry = metrics[-1]
    assert entry["status"] == "failed"
    assert entry["processed"] == 1
    assert entry["successes"] == 0
