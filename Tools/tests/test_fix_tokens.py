import json
import sys

import fix_tokens


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
