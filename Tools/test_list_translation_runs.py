import json
import sys

import list_translation_runs


def test_lists_runs(tmp_path, capsys, monkeypatch):
    index_path = tmp_path / "translations" / "run_index.json"
    index_path.parent.mkdir(parents=True)
    entries = [
        {
            "run_id": "1",
            "timestamp": "2024-01-01T00:00:00Z",
            "language": "xx",
            "run_dir": "dir1",
            "success_rate": 1.0,
        }
    ]
    index_path.write_text(json.dumps(entries))

    monkeypatch.setattr(
        sys,
        "argv",
        ["list_translation_runs.py", "--root", str(tmp_path)],
    )
    list_translation_runs.main()
    out = capsys.readouterr().out
    assert "xx" in out and "dir1" in out
