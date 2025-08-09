import os
import re
from typing import Set

_STOP_WORDS_PATH = os.path.join(os.path.dirname(__file__), "english_stopwords.txt")
with open(_STOP_WORDS_PATH, encoding="utf-8") as f:
    STOP_WORDS: Set[str] = {line.strip().lower() for line in f if line.strip()}

_WORD_RE = re.compile(r"\b\w+\b")


def contains_english(text: str) -> bool:
    """Return True if the text appears to contain English words."""
    words = set(_WORD_RE.findall(text.lower()))
    return any(word in STOP_WORDS for word in words)
