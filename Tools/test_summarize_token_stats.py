import summarize_token_stats as sts


def test_aggregate():
    entries = [
        {
            "file": "a.json",
            "failures": {"1": "token mismatch"},
            "hash_stats": {
                "1": {"original_tokens": 1, "translated_tokens": 0, "reordered": False},
                "2": {"original_tokens": 1, "translated_tokens": 1, "reordered": True},
            },
        },
        {
            "file": "b.json",
            "failures": {},
            "hash_stats": {
                "2": {"original_tokens": 1, "translated_tokens": 1, "reordered": False}
            },
        },
    ]
    stats = sts._aggregate(entries)
    assert stats == {
        "1": {"mismatches": 1, "reorders": 0, "file": "a.json"},
        "2": {"mismatches": 0, "reorders": 1, "file": "a.json"},
    }
