import json
import subprocess
import sys

import pytest

import translate_argos


def test_protect_unprotect_simple_tokens():
    text = "Attack {0}!"
    safe, tokens = translate_argos.protect_strict(text)
    assert tokens == ["{0}"]
    assert "⟦T0⟧" in safe
    assert translate_argos.unprotect(safe, tokens) == text


def test_protect_unprotect_nested_color_and_placeholder():
    text = "<color=red>{0}</color>"
    safe, tokens = translate_argos.protect_strict(text)
    assert tokens == ["<color=red>", "{0}", "</color>"]
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
    tokens = ["<b>", "{0}", "</b>"]
    result = "⟦T0⟧⟦T1⟧⟦T2⟧ [[ TOKEN_SENTINEL ]]"
    normalized = translate_argos.normalize_tokens(result)
    restored = translate_argos.unprotect(normalized, tokens)
    assert restored == "<b>{0}</b>"


def test_mixed_placeholders_round_trip_with_reorder():
    text = "[b]<color=red>${var} {0}</color>[/b]"
    safe, tokens = translate_argos.protect_strict(text)
    translation = "⟦T1⟧⟦T2⟧⟦T0⟧ ⟦T4⟧⟦T3⟧⟦T5⟧"
    normalized = translate_argos.normalize_tokens(translation)
    reordered, changed = translate_argos.reorder_tokens(normalized, len(tokens))
    assert changed
    restored = translate_argos.unprotect(reordered, tokens)
    assert restored == text
