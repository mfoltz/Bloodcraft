import json
import sys
import approve_translation_run


def test_approve_run(tmp_path, monkeypatch):
    lang_dir = tmp_path / "translations" / "xx"
    lang_dir.mkdir(parents=True)
    data = [{"run_id": "1", "approved": False}]
    index_file = lang_dir / "run_index.json"
    index_file.write_text(json.dumps(data))
    monkeypatch.setattr(
        sys,
        "argv",
        [
            "approve_translation_run.py",
            "--language",
            "xx",
            "--run-id",
            "1",
            "--root",
            str(tmp_path),
        ],
    )
    approve_translation_run.main()
    result = json.loads(index_file.read_text())
    assert result[0]["approved"] is True
