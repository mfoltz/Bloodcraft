import csv
from collections import Counter

import analyze_translation_logs as atl


def test_categorize():
    assert atl._categorize("missing sentinel") == "sentinel"
    assert atl._categorize("identical to source") == "identical"
    assert atl._categorize("contains English") == "english"
    assert atl._categorize("token mismatch (missing [1])") == "token_mismatch"
    assert atl._categorize("placeholders only") == "placeholder"


def test_summarize_skipped(tmp_path):
    path = tmp_path / "skipped.csv"
    with path.open("w", newline="", encoding="utf-8") as fp:
        writer = csv.DictWriter(fp, fieldnames=["hash", "english", "reason", "category"])
        writer.writeheader()
        writer.writerow({"hash": "1", "english": "a", "reason": "r", "category": "identical"})
        writer.writerow({"hash": "2", "english": "a", "reason": "r", "category": "sentinel"})
        writer.writerow({"hash": "3", "english": "a", "reason": "r", "category": "placeholder"})
        writer.writerow({"hash": "4", "english": "a", "reason": "r", "category": "untranslated"})
        writer.writerow({"hash": "5", "english": "a", "reason": "r", "category": "token_mismatch"})
    counts = atl._summarize_skipped(path)
    assert counts == Counter(
        {
            "identical": 1,
            "sentinel": 1,
            "placeholder": 1,
            "english": 1,
            "token_mismatch": 1,
        }
    )
