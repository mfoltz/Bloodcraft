import json

import propagate_hashes


def test_propagate_hashes(tmp_path):
    english_path = tmp_path / "English.json"
    target_path = tmp_path / "Target.json"

    english_path.write_text(
        json.dumps({"Messages": {"keep": "Hello", "new": "World"}})
    )
    target_path.write_text(
        json.dumps({"Messages": {"keep": "Hola", "obsolete": "Obsoleto"}})
    )

    english = propagate_hashes.load_messages(english_path)
    stats = propagate_hashes.propagate(english, target_path)

    data = json.loads(target_path.read_text())
    assert data["Messages"] == {"keep": "Hola", "new": "World"}
    assert stats == {"added": 1, "removed": 1, "unchanged": 1}


def test_dry_run(tmp_path):
    english_path = tmp_path / "English.json"
    target_path = tmp_path / "Target.json"

    english_path.write_text(
        json.dumps({"Messages": {"keep": "Hello", "new": "World"}})
    )
    target_path.write_text(
        json.dumps({"Messages": {"keep": "Hola", "obsolete": "Obsoleto"}})
    )

    english = propagate_hashes.load_messages(english_path)
    original = target_path.read_text()
    stats = propagate_hashes.propagate(english, target_path, dry_run=True)

    assert target_path.read_text() == original
    assert stats == {"added": 1, "removed": 1, "unchanged": 1}


def test_json_metrics(tmp_path, monkeypatch):
    english_path = tmp_path / "English.json"
    target_path = tmp_path / "Target.json"

    english_path.write_text(
        json.dumps({"Messages": {"keep": "Hello", "new": "World"}})
    )
    target_path.write_text(
        json.dumps({"Messages": {"keep": "Hola", "obsolete": "Obsoleto"}})
    )

    monkeypatch.chdir(tmp_path)
    propagate_hashes.main(
        [str(target_path), "--source", str(english_path), "--json", "--dry-run"]
    )

    metrics_path = tmp_path / "propagate_metrics.json"
    data = json.loads(metrics_path.read_text())
    assert data[str(target_path.resolve())] == {
        "added": 1,
        "removed": 1,
        "unchanged": 1,
    }
