import json
import subprocess
import sys

import pytest

import translate_argos
import fix_tokens


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


def test_token_only_line_uses_sentinel(tmp_path, monkeypatch):
    root = tmp_path
    messages_dir = root / "Resources" / "Localization" / "Messages"
    messages_dir.mkdir(parents=True)
    english = {"Messages": {"hash": "<b>{0}</b>"}}
    (messages_dir / "English.json").write_text(json.dumps(english))

    target_rel = "Resources/Localization/Messages/Test.json"
    target_path = root / target_rel
    target_path.parent.mkdir(parents=True, exist_ok=True)
    target_path.write_text(json.dumps({"Messages": {"hash": ""}}))

    class EchoTranslator:
        def __init__(self):
            self.last = ""

        def translate(self, text: str) -> str:
            self.last = text
            return text

    class DummyCompleted:
        def __init__(self, code=0):
            self.returncode = code

    translator = EchoTranslator()

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

    assert translator.last.endswith(" " + translate_argos.TOKEN_SENTINEL)
    data = json.loads(target_path.read_text())
    assert data["Messages"]["hash"] == "<b>{0}</b>"


def test_sentinel_round_trip():
    text = "<b>{0}</b>"
    safe, tokens = translate_argos.protect_strict(text)
    ids = list(tokens.keys())
    result = "".join(f"[[TOKEN_{tid}]]" for tid in ids) + " [[ TOKEN_SENTINEL ]]"
    normalized = translate_argos.normalize_tokens(result)
    restored = translate_argos.unprotect(normalized, tokens)
    assert restored == text


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


def test_extra_placeholders_trimmed(tmp_path, monkeypatch):
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
        ],
    )

    translate_argos.main()

    data = json.loads(target_path.read_text())
    assert data["Messages"]["hash"] == "Translated {0}!"

    monkeypatch.setattr(
        sys, "argv", ["fix_tokens.py", "--root", str(root), "--check-only", target_rel]
    )
    fix_tokens.main()
