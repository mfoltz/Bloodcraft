import translate_argos


def test_round_trip_mixed_placeholders_and_nested_tags():
    text = "[b]<color=red>${var} {0}</color>[/b]"
    safe, tokens = translate_argos.protect_strict(text)
    translation = "⟦T1⟧⟦T2⟧⟦T0⟧ ⟦T4⟧⟦T3⟧⟦T5⟧"
    normalized = translate_argos.normalize_tokens(translation)
    reordered, changed = translate_argos.reorder_tokens(normalized, len(tokens))
    assert changed
    restored = translate_argos.unprotect(reordered, tokens)
    assert restored == text
