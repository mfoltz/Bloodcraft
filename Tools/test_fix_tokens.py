import json
import sys

import logging
import subprocess
import pytest

import fix_tokens
import translate_argos
import token_patterns


@pytest.fixture(autouse=True)
def _stub_check_output(monkeypatch):
    def fake_check_output(cmd, *a, **k):
        if cmd and cmd[0] == "git":
            raise subprocess.CalledProcessError(1, cmd)
        return b""

    monkeypatch.setattr(subprocess, "check_output", fake_check_output)


def test_reorder_option(tmp_path, monkeypatch):
    root = tmp_path
    messages_dir = root / "Resources" / "Localization" / "Messages"
    messages_dir.mkdir(parents=True)
    (messages_dir / "English.json").write_text(
        json.dumps({"Messages": {"hash": "{a} {b}"}})
    )
    # minimal root-level English file for load_english_nodes
    (root / "Resources" / "Localization" / "English.json").write_text(json.dumps({}))
    target = messages_dir / "Test.json"
    target.write_text(json.dumps({"Messages": {"hash": "{b} {a}"}}))
    metrics_path = root / "metrics.json"

    monkeypatch.setattr(
        sys,
        "argv",
        [
            "fix_tokens.py",
            "--root",
            str(root),
            "--reorder",
            "--metrics-file",
            str(metrics_path),
            str(target),
        ],
    )

    fix_tokens.main()
    data = json.loads(target.read_text())
    assert data["Messages"]["hash"] == "{a} {b}"
    metrics = json.loads(metrics_path.read_text())[-1]
    assert metrics["tokens_restored"] == 0
    assert metrics["tokens_reordered"] == 1
    assert metrics["token_mismatches"] == 0


def test_normalize_tokens_merge_with_space():
    raw = "[[TOKEN_1 0]]"
    assert translate_argos.normalize_tokens(raw) == "[[TOKEN_10]]"


def test_normalize_tokens_merge_adjacent():
    raw = "[[TOKEN_1]][[TOKEN_0]]"
    assert translate_argos.normalize_tokens(raw) == raw


def test_replace_placeholders_token_only_line():
    tokens = ["<b>", "{x}", "</b>"]
    value = "[[TOKEN_0]] [[TOKEN_1]] [[TOKEN_2]]"
    new_value, replaced, mismatch, missing, extra, restored = fix_tokens.replace_placeholders(
        value, tokens
    )
    assert new_value.replace(" ", "") == "<b>{x}</b>"
    assert replaced and not mismatch and not missing and not extra and not restored


def test_replace_placeholders_with_various_tokens():
    tokens = ["{0}", "${var}", "[[TOKEN_0]]", "{PlayerName}"]
    value = " ".join(f"[[TOKEN_{i}]]" for i in range(len(tokens)))
    new_value, replaced, mismatch, missing, extra, restored = fix_tokens.replace_placeholders(
        value, tokens
    )
    assert new_value.replace(" ", "") == "".join(tokens)
    assert replaced and not mismatch and not missing and not extra and not restored


def test_replace_placeholders_restores_missing_token4_and_token5():
    tokens = ["<a>", "</a>", "<b>", "</b>", "<c>", "</c>"]
    value = " ".join(f"[[TOKEN_{i}]]" for i in range(4))
    new_value, replaced, mismatch, missing, extra, restored = fix_tokens.replace_placeholders(
        value, tokens
    )
    assert new_value.replace(" ", "") == "<a></a><b></b><c></c>"
    assert replaced and not mismatch and not missing and not extra and restored == [4, 5]


def test_replace_placeholders_restores_missing_token4():
    tokens = ["<a>", "</a>", "<b>", "</b>", "<c>", "</c>"]
    value = " ".join(["[[TOKEN_0]]", "[[TOKEN_1]]", "[[TOKEN_2]]", "[[TOKEN_3]]", "[[TOKEN_5]]"])
    new_value, replaced, mismatch, missing, extra, restored = fix_tokens.replace_placeholders(
        value, tokens
    )
    assert new_value.replace(" ", "") == "<a></a><b></b><c></c>"
    assert replaced and not mismatch and not missing and not extra and restored == [4]


def test_normalize_tokens_single_bracket_forms():
    assert translate_argos.normalize_tokens("[TOKEN_4]") == "[[TOKEN_4]]"
    assert translate_argos.normalize_tokens("[token_5]") == "[[TOKEN_5]]"


def test_normalization_merges_split_tokens(tmp_path, monkeypatch):
    root = tmp_path
    messages_dir = root / "Resources" / "Localization" / "Messages"
    messages_dir.mkdir(parents=True)
    (messages_dir / "English.json").write_text(
        json.dumps({"Messages": {"hash": "{a}"}})
    )
    (root / "Resources" / "Localization" / "English.json").write_text(json.dumps({}))
    target = messages_dir / "Test.json"
    target.write_text(json.dumps({"Messages": {"hash": "[[TOKEN_1]][[TOKEN_0]]"}}))
    metrics_path = root / "metrics.json"

    monkeypatch.setattr(
        sys,
        "argv",
        [
            "fix_tokens.py",
            "--root",
            str(root),
            "--metrics-file",
            str(metrics_path),
            str(target),
        ],
    )

    with pytest.raises(SystemExit):
        fix_tokens.main()


def test_exit_on_mismatch(tmp_path, monkeypatch):
    root = tmp_path
    messages_dir = root / "Resources" / "Localization" / "Messages"
    messages_dir.mkdir(parents=True)
    (messages_dir / "English.json").write_text(
        json.dumps({"Messages": {"hash": "{a}"}})
    )
    (root / "Resources" / "Localization" / "English.json").write_text(json.dumps({}))
    target = messages_dir / "Test.json"
    target.write_text(json.dumps({"Messages": {"hash": "[[TOKEN_0]] [[TOKEN_1]]"}}))
    metrics_path = root / "metrics.json"

    monkeypatch.setattr(
        sys,
        "argv",
        [
            "fix_tokens.py",
            "--root",
            str(root),
            "--metrics-file",
            str(metrics_path),
            str(target),
        ],
    )

    with pytest.raises(SystemExit) as exc:
        fix_tokens.main()
    assert str(metrics_path) in str(exc.value)
    metrics = json.loads(metrics_path.read_text())[-1]
    assert metrics["token_mismatches"] == 1
    assert metrics["tokens_restored"] == 0
    assert metrics["tokens_reordered"] == 0
    assert metrics["mismatches"][0]["key"] == "hash"


def test_allow_mismatch(tmp_path, monkeypatch, caplog):
    root = tmp_path
    messages_dir = root / "Resources" / "Localization" / "Messages"
    messages_dir.mkdir(parents=True)
    (messages_dir / "English.json").write_text(json.dumps({"Messages": {"hash": "{a}"}}))
    (root / "Resources" / "Localization" / "English.json").write_text(json.dumps({}))
    target = messages_dir / "Test.json"
    target.write_text(json.dumps({"Messages": {"hash": "[[TOKEN_0]] [[TOKEN_1]]"}}))
    metrics_path = root / "metrics.json"

    monkeypatch.setattr(
        sys,
        "argv",
        [
            "fix_tokens.py",
            "--root",
            str(root),
            "--metrics-file",
            str(metrics_path),
            "--allow-mismatch",
            str(target),
        ],
    )

    with caplog.at_level(logging.WARNING):
        fix_tokens.main()
    assert "token count mismatch" in caplog.text
    metrics = json.loads(metrics_path.read_text())[-1]
    assert metrics["token_mismatches"] == 1


def test_extract_tokens_bracket_tags_and_percent():
    text = "[b]100%[/b] [color=red]"
    assert token_patterns.extract_tokens(text) == ["[b]", "%", "[/b]", "[color=red]"]


def test_replace_placeholders_with_bracket_tags_and_percent():
    tokens = ["[b]", "%", "[/b]"]
    value = "[[TOKEN_0]]100[[TOKEN_1]][[TOKEN_2]]"
    new_value, replaced, mismatch, missing, extra, restored = fix_tokens.replace_placeholders(
        value, tokens
    )
    assert new_value == "[b]100%[/b]"
    assert replaced and not mismatch and not missing and not extra and not restored


def test_missing_token_metrics_and_logging(tmp_path, monkeypatch, caplog):
    root = tmp_path
    messages_dir = root / "Resources" / "Localization" / "Messages"
    messages_dir.mkdir(parents=True)
    (messages_dir / "English.json").write_text(
        json.dumps({"Messages": {"hash": "<a>{x}</a>"}})
    )
    (root / "Resources" / "Localization" / "English.json").write_text(json.dumps({}))
    target = messages_dir / "Test.json"
    target.write_text(json.dumps({"Messages": {"hash": "[[TOKEN_0]] [[TOKEN_2]]"}}))
    metrics_path = root / "metrics.json"

    monkeypatch.setattr(
        sys,
        "argv",
        [
            "fix_tokens.py",
            "--root",
            str(root),
            "--metrics-file",
            str(metrics_path),
            str(target),
        ],
    )

    with caplog.at_level(logging.INFO):
        fix_tokens.main()

    data = json.loads(target.read_text())
    assert data["Messages"]["hash"].replace(" ", "") == "<a>{x}</a>"
    metrics = json.loads(metrics_path.read_text())[-1]
    assert metrics["tokens_restored"] == 1
    assert metrics["tokens_reordered"] == 0
    assert metrics["token_mismatches"] == 0
    assert "positions [1]" in caplog.text


@pytest.mark.skip(reason="log format varies with argostranslate version")
@pytest.mark.parametrize(
    "translation,warning",
    [
        ("Translated [[TOKEN_0]]", "token mismatch (missing ['1'])"),
        (
            "Translated [[TOKEN_0]] [[TOKEN_1]] [[TOKEN_999]]",
            "token mismatch (dropped ['999'])",
        ),
        ("Translated [[TOKEN_1]] [[TOKEN_0]]", "tokens reordered"),
    ],
)
def test_fix_tokens_after_lenient_translation(tmp_path, monkeypatch, caplog, translation, warning):
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
        def translate(self, text: str) -> str:
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

    import re
    expected = warning.split("token mismatch ", 1)[1]
    pattern = rf"token mismatch (\\[[^\\]]+\\] )?{re.escape(expected)}"
    assert re.search(pattern, caplog.text)
    intermediate = json.loads(target_path.read_text())
    assert "{0}" in intermediate["Messages"]["hash"]
    assert "{1}" in intermediate["Messages"]["hash"]

    monkeypatch.setattr(sys, "argv", ["fix_tokens.py", "--root", str(root), target_rel])
    fix_tokens.main()

    data = json.loads(target_path.read_text())
    assert data["Messages"]["hash"] == "Translated {0} {1}"
