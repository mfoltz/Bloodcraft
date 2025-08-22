import translate_argos


def test_round_trip_mixed_placeholders_and_nested_tags():
    text = "[b]<color=red>${var} {0}</color>[/b]"
    safe, tokens = translate_argos.protect_strict(text)
    ids = list(tokens.keys())
    translation = (
        f"⟦T{ids[1]}⟧⟦T{ids[2]}⟧⟦T{ids[0]}⟧ "
        f"⟦T{ids[4]}⟧⟦T{ids[3]}⟧⟦T{ids[5]}⟧"
    )
    normalized = translate_argos.normalize_tokens(translation)
    reordered, changed = translate_argos.reorder_tokens(normalized, ids)
    assert changed
    restored = translate_argos.unprotect(reordered, tokens)
    expected = (
        f"{tokens[ids[1]]}{tokens[ids[2]]}{tokens[ids[0]]} "
        f"{tokens[ids[4]]}{tokens[ids[3]]}{tokens[ids[5]]}"
    )
    assert restored == expected
