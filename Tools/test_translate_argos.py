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
    assert rows[0]["category"] == "identical"
    assert "identical" in rows[0]["reason"]


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
        ],
    )

    translate_argos.main()
    rows = list(csv.DictReader(report_path.open()))
    assert rows[0]["category"] == "placeholder"
    data = json.loads((root / target_rel).read_text())
    assert data["Messages"]["hash"] == "Hello {0}"


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

    translate_argos.main()
    rows = list(csv.DictReader(report_path.open()))
    assert rows[0]["category"] == "untranslated"
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
            if self.calls == 1:
                return "[[TOKEN_1]]Bonjour"
            return "__T0__Bonjour__T1__"

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


def test_normalize_and_reorder_many_tokens():
    raw = " ".join(f"__T{i}__" for i in range(12))
    normalized = translate_argos.normalize_tokens(raw)
    assert normalized == "".join(f"[[TOKEN_{i}]]" for i in range(12))
    text, changed = translate_argos.reorder_tokens(
        " ".join(f"[[TOKEN_{i}]]" for i in range(11, -1, -1)), 12
    )
    assert text == " ".join(f"[[TOKEN_{i}]]" for i in range(12))
    assert changed


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
    assert translator.seen == "before __T0__ after"

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
    assert translator.seen == "before __T0__ middle __T1__ after"

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
            tokens = re.findall("__T\\d+__", text)
            tokens.reverse()
            it = iter(tokens)
            return re.sub("__T\\d+__", lambda _m: next(it), text)

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
    assert data["Messages"]["h"] == "Hello <b>{x}</b> world"


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

    translate_argos.main()

    metrics_path = root / "translate_metrics.json"
    data = json.loads(metrics_path.read_text())
    entry = data[-1]
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

    translate_argos.main()

    data = json.loads(metrics_path.read_text())
    entry = data[-1]
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
            return "Hola [[TOKEN_1]], [[TOKEN_0]]"

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

    translate_argos.main()

    assert report_path.is_file()
    rows = list(csv.DictReader(report_path.open()))
    hashes = {row["hash"] for row in rows}
    assert "h2" in hashes
