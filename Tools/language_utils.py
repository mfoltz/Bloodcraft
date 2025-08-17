import os
import re
from typing import Set

_STOP_WORDS_PATH = os.path.join(os.path.dirname(__file__), "english_stopwords.txt")
with open(_STOP_WORDS_PATH, encoding="utf-8") as f:
    STOP_WORDS: Set[str] = {line.strip().lower() for line in f if line.strip()}

_ALLOWLIST_PATH = os.path.join(os.path.dirname(__file__), "english_allowlist.txt")
if os.path.exists(_ALLOWLIST_PATH):
    with open(_ALLOWLIST_PATH, encoding="utf-8") as f:
        ALLOWLIST: Set[str] = {line.strip().lower() for line in f if line.strip()}
else:
    ALLOWLIST: Set[str] = set()

_WORD_RE = re.compile(r"\b\w+\b")
_PLACEHOLDER_RE = re.compile(r"\[\[TOKEN_\d+\]\]|<[^>]+>|\{[^}]+\}")


def contains_english(text: str) -> bool:
    """Return True if the text appears to contain English words.

    Placeholder patterns like ``[[TOKEN_n]]``, ``<...>``, and ``{...}`` are
    removed before scanning. Words listed in ``english_allowlist.txt`` are
    ignored.
    """
    cleaned = _PLACEHOLDER_RE.sub("", text)
    words = set(_WORD_RE.findall(cleaned.lower()))
    words -= ALLOWLIST
    return any(word in STOP_WORDS for word in words)
