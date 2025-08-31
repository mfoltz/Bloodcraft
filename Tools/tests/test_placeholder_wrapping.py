import translate_argos


def test_wrap_and_unwrap_preserves_tokens_over_threshold():
    original = " ".join(f"[[TOKEN_{i}]]" for i in range(25))
    wrapped, ids = translate_argos.wrap_placeholders(original)
    assert len(ids) == 25
    # Wrapped text should no longer contain explicit placeholders
    assert not translate_argos.TOKEN_RE.search(wrapped)
    unwrapped = translate_argos.unwrap_placeholders(wrapped, ids)
    assert unwrapped == original


def test_normalize_tokens_trims_sentinels():
    text = "[[TOKEN_0]] [[ TOKEN_sentinel ]] TOKEN_SENTINEL [[TOKEN_1]]"
    normalized = translate_argos.normalize_tokens(text)
    assert normalized == "[[TOKEN_0]] [[TOKEN_1]]"
