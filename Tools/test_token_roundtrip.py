import pytest

import translate_argos


def test_mixed_placeholders_nested_tags_round_trip():
    text = "[b]<color=red>{0}% [link]{1}[/link]</color>[/b]"
    safe, tokens = translate_argos.protect_strict(text)
    normalized = translate_argos.normalize_tokens(safe)
    reordered, changed = translate_argos.reorder_tokens(normalized, len(tokens))
    assert not changed
    restored = translate_argos.unprotect(reordered, tokens)
    assert restored == text


def test_unprotect_mismatch_raises():
    tokens = ["%"]
    with pytest.raises(ValueError):
        translate_argos.unprotect("[[TOKEN_0]] [[TOKEN_1]]", tokens)
