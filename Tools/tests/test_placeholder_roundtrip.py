import translate_argos


def test_round_trip_mixed_placeholders_and_nested_tags():
    text = "[b]<color=red>${var} {0}</color>[/b]"
    safe, tokens = translate_argos.protect_strict(text)
    translation = "[[TOKEN_1]][[TOKEN_2]][[TOKEN_0]] [[TOKEN_4]][[TOKEN_3]][[TOKEN_5]]"
    normalized = translate_argos.normalize_tokens(translation)
    reordered, changed = translate_argos.reorder_tokens(normalized, len(tokens))
    assert changed
    restored = translate_argos.unprotect(reordered, tokens)
    assert restored == text
