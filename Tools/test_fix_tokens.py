import json
import sys

import pytest

import fix_tokens
import translate_argos


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
    metrics = json.loads(metrics_path.read_text())
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
    new_value, replaced, mismatch = fix_tokens.replace_placeholders(value, tokens)
    assert new_value.replace(" ", "") == "<b>{x}</b>"
    assert replaced and not mismatch


def test_replace_placeholders_with_various_tokens():
    tokens = ["{0}", "${var}", "[[TOKEN_0]]", "{PlayerName}"]
    value = " ".join(f"[[TOKEN_{i}]]" for i in range(len(tokens)))
    new_value, replaced, mismatch = fix_tokens.replace_placeholders(value, tokens)
    assert new_value.replace(" ", "") == "".join(tokens)
    assert replaced and not mismatch


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
    metrics = json.loads(metrics_path.read_text())
    assert metrics["token_mismatches"] == 1
    assert metrics["tokens_restored"] == 0
    assert metrics["tokens_reordered"] == 0


def test_extract_tokens_bracket_and_percent():
    text = "[b]100%[/b] [color=]"
    assert fix_tokens.extract_tokens(text) == ["[b]", "%", "[/b]", "[color=]"]


def test_replace_placeholders_with_bracket_tags_and_percent():
    tokens = ["[b]", "%", "[/b]"]
    value = "[[TOKEN_0]]100[[TOKEN_1]][[TOKEN_2]]"
    new_value, replaced, mismatch = fix_tokens.replace_placeholders(value, tokens)
    assert new_value == "[b]100%[/b]"
    assert replaced and not mismatch
