from token_patterns import extract_tokens


def test_extract_tokens_captures_supported_placeholder_shapes():
    text = "<color=red>{0}${name}[b][icon=sword][[TOKEN_ab12]]%"
    assert extract_tokens(text) == [
        "<color=red>",
        "{0}",
        "${name}",
        "[b]",
        "[icon=sword]",
        "[[TOKEN_ab12]]",
        "%",
    ]
