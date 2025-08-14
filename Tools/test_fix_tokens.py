import json
import sys

import fix_tokens


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

    monkeypatch.setattr(
        sys,
        "argv",
        [
            "fix_tokens.py",
            "--root",
            str(root),
            "--reorder",
            str(target),
        ],
    )

    fix_tokens.main()
    data = json.loads(target.read_text())
    assert data["Messages"]["hash"] == "{a} {b}"
