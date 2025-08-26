import fix_tokens

def test_replace_placeholders_preserves_quotes_705325693():
    value = (
        "Familiar already has all prestige stats! (\\\"[[TOKEN_0]].fam pr[[TOKEN_1]]\\\" "
        "instead of \\\'[[TOKEN_2]].fam pr [[TOKEN_3]] [[TOKEN_4]]\\\')"
    )
    tokens = [
        "<color=white>",
        "</color>",
        "<color=white>",
        "[PrestigeStat]",
        "</color>",
    ]
    new_value, replaced, mismatch, missing, extra = fix_tokens.replace_placeholders(value, tokens)
    assert new_value == (
        "Familiar already has all prestige stats! (\"<color=white>.fam pr</color>\" "
        "instead of '<color=white>.fam pr [PrestigeStat] </color>')"
    )
    assert replaced and not mismatch and not missing and not extra
