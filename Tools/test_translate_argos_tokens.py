import json
import subprocess
import sys
import re
import json
import subprocess
import sys
import re
import types

import logging
import pytest

argos_stub = types.ModuleType("argostranslate")
argos_stub.translate = types.SimpleNamespace(
    get_translation_from_codes=lambda *a, **k: None,
    load_installed_languages=lambda: None,
)
sys.modules.setdefault("argostranslate", argos_stub)

import translate_argos
import fix_tokens
import token_patterns


@pytest.fixture(autouse=True)
def _stub_check_output(monkeypatch):
    def fake_check_output(cmd, *a, **k):
        if cmd and cmd[0] == "git":
            raise subprocess.CalledProcessError(1, cmd)
        return b""

    monkeypatch.setattr(subprocess, "check_output", fake_check_output)


def test_protect_unprotect_simple_tokens():
    text = "Attack {0}!"
    safe, tokens = translate_argos.protect_strict(text)
    assert list(tokens.values()) == ["{0}"]
    token_id = next(iter(tokens))
    assert f"[[TOKEN_{token_id}]]" in safe
    assert translate_argos.unprotect(safe, tokens) == text


def test_protect_unprotect_nested_color_and_placeholder():
    text = "<color=red>{0}</color>"
    safe, tokens = translate_argos.protect_strict(text)
    assert list(tokens.values()) == ["<color=red>", "{0}", "</color>"]
    assert translate_argos.unprotect(safe, tokens) == text


def test_placeholder_only_skipped(tmp_path, monkeypatch):
    root = tmp_path
    messages_dir = root / "Resources" / "Localization" / "Messages"
    messages_dir.mkdir(parents=True)
    english = {"Messages": {"hash": "<b>{0}</b>"}}
    (messages_dir / "English.json").write_text(json.dumps(english))

    target_rel = "Resources/Localization/Messages/Test.json"
    target_path = root / target_rel
    target_path.parent.mkdir(parents=True, exist_ok=True)
    target_path.write_text(json.dumps({"Messages": {"hash": ""}}))

    class DummyTranslator:
        def __init__(self):
            self.calls = 0

        def translate(self, text: str) -> str:
            self.calls += 1
            return text

    class DummyCompleted:
        def __init__(self, code=0):
            self.returncode = code

    translator = DummyTranslator()

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
            "--lenient-tokens",
        ],
    )

    translate_argos.main()

    assert translator.calls == 0
    data = json.loads(target_path.read_text())
    assert data["Messages"]["hash"] == "<b>{0}</b>"
    run_dir = next((root / "translations" / "xx").iterdir())
    report_path = run_dir / "skipped.csv"
    rows = report_path.read_text().splitlines()
    assert rows == ["hash,english,reason,category"]


def test_sentinel_round_trip():
    text = "<b>{0}</b>"
    safe, tokens = translate_argos.protect_strict(text)
    ids = list(tokens.keys())
    result = "".join(f"[[TOKEN_{tid}]]" for tid in ids) + " [[ TOKEN_SENTINEL ]]"
    normalized = translate_argos.normalize_tokens(result)
    restored = translate_argos.unprotect(normalized, tokens)
    assert restored == text


def test_trim_whitespace_inside_color_tags():
    text = "<color=red> {0} </color>"
    normalized = translate_argos.normalize_tokens(text)
    assert normalized == "<color=red>{0}</color>"


def test_mixed_placeholders_round_trip_with_reorder():
    text = "[b]<color=red>${var} {0}</color>[/b]"
    safe, tokens = translate_argos.protect_strict(text)
    ids = list(tokens.keys())
    translation = (
        f"[[TOKEN_{ids[1]}]][[TOKEN_{ids[2]}]][[TOKEN_{ids[0]}]] "
        f"[[TOKEN_{ids[4]}]][[TOKEN_{ids[3]}]][[TOKEN_{ids[5]}]]"
    )
    normalized = translate_argos.normalize_tokens(translation)
    reordered, changed = translate_argos.reorder_tokens(normalized, ids)
    assert changed
    restored = translate_argos.unprotect(reordered, tokens)
    expected = (
        f"{tokens[ids[1]]}{tokens[ids[2]]}{tokens[ids[0]]} "
        f"{tokens[ids[4]]}{tokens[ids[3]]}{tokens[ids[5]]}"
    )
    assert restored == expected


def test_many_placeholders_round_trip():
    text = "".join(f"{{{i}}}" for i in range(15))
    safe, tokens = translate_argos.protect_strict(text)
    assert len(tokens) == 15
    wrapped, mapping = translate_argos.wrap_placeholders(safe)
    unwrapped = translate_argos.unwrap_placeholders(wrapped, mapping)
    restored = translate_argos.unprotect(unwrapped, tokens)
    assert restored == text


def test_unwrap_restores_translated_order():
    count = 15
    text = "".join(f"{{{i}}}" for i in range(count))
    safe, tokens = translate_argos.protect_strict(text)
    wrapped, mapping = translate_argos.wrap_placeholders(safe)
    translated_wrapped = wrapped[::-1]
    unwrapped = translate_argos.unwrap_placeholders(translated_wrapped, mapping)
    restored = translate_argos.unprotect(unwrapped, tokens)
    expected = "".join(f"{{{i}}}" for i in reversed(range(count)))
    assert restored == expected


@pytest.mark.parametrize("threshold", [5, 10, 15])
def test_wrap_threshold_boundary(monkeypatch, threshold):
    monkeypatch.setattr(translate_argos, "PLACEHOLDER_WRAP_THRESHOLD", threshold)
    text = "".join(f"{{{i}}}" for i in range(threshold))
    safe, _ = translate_argos.protect_strict(text)
    wrapped, _ = translate_argos.wrap_placeholders(safe)
    assert wrapped == safe
    text2 = "".join(f"{{{i}}}" for i in range(threshold + 1))
    safe2, _ = translate_argos.protect_strict(text2)
    wrapped2, _ = translate_argos.wrap_placeholders(safe2)
    assert wrapped2 != safe2


def test_extra_placeholders_trimmed(tmp_path, monkeypatch, caplog):
    root = tmp_path
    messages_dir = root / "Resources" / "Localization" / "Messages"
    messages_dir.mkdir(parents=True)
    english = {"Messages": {"hash": "Attack {0}!"}}
    (messages_dir / "English.json").write_text(json.dumps(english))

    target_rel = "Resources/Localization/Messages/Test.json"
    target_path = root / target_rel
    target_path.parent.mkdir(parents=True, exist_ok=True)
    target_path.write_text(json.dumps({"Messages": {"hash": ""}}))

    class ExtraTokenTranslator:
        def translate(self, text: str) -> str:
            return "Translated [[TOKEN_0]]! [[TOKEN_999]]"

    class DummyCompleted:
        def __init__(self, code=0):
            self.returncode = code

    translator = ExtraTokenTranslator()

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
            "--lenient-tokens",
        ],
    )

    with caplog.at_level(logging.WARNING):
        translate_argos.main()

    assert re.search(r"token mismatch \[[0-9a-f]{8}\] \(dropped \['999'\]\)", caplog.text)
    assert "Suggested fix: remove ['999']" in caplog.text

    data = json.loads(target_path.read_text())
    assert data["Messages"]["hash"] == "Translated {0}!"

    run_dir = next((root / "translations" / "xx").iterdir())
    metrics = json.loads((run_dir / "metrics.json").read_text())
    assert metrics[0]["hash_stats"]["hash"]["removed_tokens"] == 1
    assert metrics[0]["tokens_removed"] == 1

    monkeypatch.setattr(
        sys, "argv", ["fix_tokens.py", "--root", str(root), "--check-only", target_rel]
    )
    fix_tokens.main()


@pytest.mark.parametrize(
    "translation,expected,pattern",
    [
        (
            "Translated [[TOKEN_0]]",
            "Translated {0} {1}",
            r"token mismatch \[[0-9a-f]{8}\] \(missing \['1'\]\)",
        ),
        (
            "Translated [[TOKEN_0]] [[TOKEN_1]] [[TOKEN_999]]",
            "Translated {0} {1}",
            r"token mismatch \[[0-9a-f]{8}\] \(dropped \['999'\]\)",
        ),
        (
            "Translated [[TOKEN_1]] [[TOKEN_0]]",
            "Translated {1} {0}",
            r"tokens reordered",
        ),
    ],
)
def test_lenient_token_mismatches(tmp_path, monkeypatch, caplog, translation, expected, pattern):
    root = tmp_path
    messages_dir = root / "Resources" / "Localization" / "Messages"
    messages_dir.mkdir(parents=True)
    english = {"Messages": {"hash": "Attack {0} {1}"}}
    (messages_dir / "English.json").write_text(json.dumps(english))

    target_rel = "Resources/Localization/Messages/Test.json"
    target_path = root / target_rel
    target_path.parent.mkdir(parents=True, exist_ok=True)
    target_path.write_text(json.dumps({"Messages": {"hash": ""}}))

    class DummyTranslator:
        def __init__(self):
            self.calls = 0

        def translate(self, text: str) -> str:
            self.calls += 1
            return translation

    class DummyCompleted:
        def __init__(self, code: int = 0):
            self.returncode = code

    translator = DummyTranslator()

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
            "--lenient-tokens",
        ],
    )

    with caplog.at_level(logging.WARNING):
        translate_argos.main()

    assert re.search(pattern, caplog.text)
    if pattern == r"tokens reordered":
        assert translator.calls >= 1
    else:
        assert translator.calls == 1
    data = json.loads(target_path.read_text())
    assert data["Messages"]["hash"] == expected

    run_dir = next((root / "translations" / "xx").glob("*"))
    metrics = json.loads((run_dir / "metrics.json").read_text())
    entry = metrics[-1]
    if "token mismatch" in pattern:
        assert entry["token_mismatches"] == 1
    else:
        assert entry["token_mismatches"] == 0


def test_mismatch_logs_id_and_suggestion(tmp_path, monkeypatch, caplog):
    root = tmp_path
    messages_dir = root / "Resources" / "Localization" / "Messages"
    messages_dir.mkdir(parents=True)
    english = {"Messages": {"hash": "Attack {0}!"}}
    (messages_dir / "English.json").write_text(json.dumps(english))

    target_rel = "Resources/Localization/Messages/Test.json"
    target_path = root / target_rel
    target_path.parent.mkdir(parents=True, exist_ok=True)
    target_path.write_text(json.dumps({"Messages": {"hash": ""}}))

    class BadTranslator:
        def translate(self, text: str) -> str:
            return "Translated [[TOKEN_1]] [[TOKEN_999]]"

    class DummyCompleted:
        def __init__(self, code: int = 0):
            self.returncode = code

    translator = BadTranslator()

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
            "--lenient-tokens",
        ],
    )

    with caplog.at_level(logging.WARNING):
        translate_argos.main()

    pattern = re.compile(r"hash: token mismatch \[[0-9a-f]{8}\].*Suggested fix: add \['0'\]; remove \['1', '999'\]")
    assert pattern.search(caplog.text)


def test_token_mismatch_retry(tmp_path, monkeypatch):
    root = tmp_path
    messages_dir = root / "Resources" / "Localization" / "Messages"
    messages_dir.mkdir(parents=True)
    english = {"Messages": {"hash": "Attack {0}!"}}
    (messages_dir / "English.json").write_text(json.dumps(english))

    target_rel = "Resources/Localization/Messages/Test.json"
    target_path = root / target_rel
    target_path.parent.mkdir(parents=True, exist_ok=True)
    target_path.write_text(json.dumps({"Messages": {"hash": ""}}))

    class BadTranslator:
        def __init__(self):
            self.calls = 0

        def translate(self, text: str) -> str:
            self.calls += 1
            return "Translated [[TOKEN_1]]"

    class DummyCompleted:
        def __init__(self, code: int = 0):
            self.returncode = code

    translator = BadTranslator()

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

    assert translator.calls >= 2
    data = json.loads(target_path.read_text())
    assert data["Messages"]["hash"] == "Translated {0}"
    run_dir = next((root / "translations" / "xx").glob("*"))
    skipped = (run_dir / "skipped.csv").read_text()
    assert "token_mismatch" not in skipped


def test_missing_tokens_fails(tmp_path, monkeypatch, caplog):
    root = tmp_path
    messages_dir = root / "Resources" / "Localization" / "Messages"
    messages_dir.mkdir(parents=True)
    english = {"Messages": {"hash": "Attack {0} {1} {2} {3} {4} {5}!"}}
    (messages_dir / "English.json").write_text(json.dumps(english))

    target_rel = "Resources/Localization/Messages/Test.json"
    target_path = root / target_rel
    target_path.parent.mkdir(parents=True, exist_ok=True)
    target_path.write_text(json.dumps({"Messages": {"hash": ""}}))

    class MissingTokenTranslator:
        def __init__(self):
            self.calls = 0

        def translate(self, text: str) -> str:
            self.calls += 1
            return "Translated [[TOKEN_0]] [[TOKEN_1]] [[TOKEN_2]] [[TOKEN_3]]!"

    class DummyCompleted:
        def __init__(self, code: int = 0):
            self.returncode = code

    translator = MissingTokenTranslator()

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

    with caplog.at_level(logging.WARNING):
        translate_argos.main()

    assert translator.calls > 1
    assert re.search(
        r"token mismatch \[[0-9a-f]{8}\] on strict retry \(missing \['4', '5'\]",
        caplog.text,
    )

    run_dir = next((root / "translations" / "xx").iterdir())
    metrics = json.loads((run_dir / "metrics.json").read_text())
    entry = metrics[-1]
    details = entry["token_mismatch_details"]["hash"]
    assert details["missing"] == ["4", "5"]
    assert details["extra"] == []
    stats = entry["hash_stats"]["hash"]
    assert stats["retry_attempted"]
    assert not stats["retry_succeeded"]
    assert stats["retry_missing_tokens"] == 2
