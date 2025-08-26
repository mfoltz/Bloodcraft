import language_utils


def test_allowlisted_word_is_ignored(monkeypatch):
    monkeypatch.setattr(language_utils, "STOP_WORDS", {"english": {"bloodcraft"}})
    monkeypatch.setattr(language_utils, "ALLOWLIST", {"english": set()})
    assert not language_utils.contains_english("Bloodcraft")


def test_tokens_are_ignored(monkeypatch):
    monkeypatch.setattr(language_utils, "STOP_WORDS", {"english": {"token_0"}})
    monkeypatch.setattr(language_utils, "ALLOWLIST", {"english": set()})
    assert not language_utils.contains_english("Hola [[TOKEN_0]]")


def test_spanish_detection(monkeypatch):
    monkeypatch.setattr(language_utils, "STOP_WORDS", {"spanish": {"hola"}})
    monkeypatch.setattr(language_utils, "ALLOWLIST", {})
    assert language_utils.contains_language("Hola amigo", "spanish")
