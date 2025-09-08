import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import fix_tokens
import translate_argos


def test_replace_placeholders_drops_unexpected_tokens():
    tokens = ["{x}"]
    value = "start<color=red></color>{x}{y}"
    new_value, replaced, mismatch, missing, extra, restored = fix_tokens.replace_placeholders(value, tokens)
    assert new_value == "start{x}"
    assert replaced and not mismatch and not missing
    assert extra == ["<color=red>", "</color>", "{y}"]
    assert not restored


def test_check_only_passes_after_removal(tmp_path, monkeypatch):
    root = tmp_path
    messages_dir = root / "Resources" / "Localization" / "Messages"
    messages_dir.mkdir(parents=True)
    (messages_dir / "English.json").write_text(json.dumps({"Messages": {"hash": "start{x}"}}))
    # minimal root-level English file for load_english_nodes
    (root / "Resources" / "Localization" / "English.json").write_text(json.dumps({}))
    target = messages_dir / "Test.json"
    target.write_text(json.dumps({"Messages": {"hash": "start<color=red></color>{x}{y}"}}))
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
    fix_tokens.main()
    data = json.loads(target.read_text())
    assert data["Messages"]["hash"] == "start{x}"
    metrics = json.loads(metrics_path.read_text())[-1]
    assert metrics["tokens_removed"] == 3

    monkeypatch.setattr(
        sys,
        "argv",
        [
            "fix_tokens.py",
            "--root",
            str(root),
            "--check-only",
            str(target),
        ],
    )
    fix_tokens.main()


def test_reorder_handles_repeated_and_nested_tokens():
    tokens = ["<b>", "{0}", "</b>", "<i>", "{1}", "</i>"]
    value = "<i>{1}</i><b>{0}</b>"
    new_value, changed = fix_tokens.reorder_tokens_in_text(value, tokens)
    assert changed
    assert new_value == "<b>{0}</b><i>{1}</i>"


def test_restore_missing_token_and_reorder_nested():
    tokens = ["<color=red>", "{0}", "{1}", "</color>"]
    # Translation dropped the third token and shuffled the rest.
    value = "[[TOKEN_1]][[TOKEN_0]][[TOKEN_3]]"
    new_value, replaced, mismatch, missing, removed, restored = fix_tokens.replace_placeholders(
        value, tokens
    )
    assert replaced and not mismatch
    assert restored == [2]
    new_value, changed = fix_tokens.reorder_tokens_in_text(new_value, tokens)
    assert changed
    assert new_value == "<color=red>{0}{1}</color>"


def test_normalize_tokens_drops_stray_closing_tags():
    raw = "<b>{0}</b></b><i></i></i>"
    assert translate_argos.normalize_tokens(raw) == "<b>{0}</b><i></i>"


def test_normalize_tokens_removes_unmatched_openings():
    assert translate_argos.normalize_tokens("<b>{0}") == "{0}"
