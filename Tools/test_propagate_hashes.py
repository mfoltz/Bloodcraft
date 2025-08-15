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
    propagate_hashes.propagate(english, target_path)

    data = json.loads(target_path.read_text())
    assert data["Messages"] == {"keep": "Hola", "new": "World"}
