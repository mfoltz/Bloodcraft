import language_utils


def test_allowlisted_word_is_ignored(monkeypatch):
    monkeypatch.setattr(language_utils, "STOP_WORDS", {"bloodcraft"})
    assert not language_utils.contains_english("Bloodcraft")


def test_tokens_are_ignored(monkeypatch):
    monkeypatch.setattr(language_utils, "STOP_WORDS", {"token_0"})
    assert not language_utils.contains_english("Hola [[TOKEN_0]]")
