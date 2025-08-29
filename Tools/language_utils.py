from pathlib import Path
import re
from typing import Dict, Set

_DIR = Path(__file__).resolve().parent


def _load_words(path: Path) -> Set[str]:
    with path.open(encoding="utf-8") as f:
        return {line.strip().lower() for line in f if line.strip()}


STOP_WORDS: Dict[str, Set[str]] = {}
ALLOWLIST: Dict[str, Set[str]] = {}
for p in _DIR.glob("*_stopwords.txt"):
    lang = p.stem.replace("_stopwords", "")
    STOP_WORDS[lang] = _load_words(p)
for p in _DIR.glob("*_allowlist.txt"):
    lang = p.stem.replace("_allowlist", "")
    ALLOWLIST[lang] = _load_words(p)

_WORD_RE = re.compile(r"\b\w+\b")
_PLACEHOLDER_RE = re.compile(r"\[\[TOKEN_\d+\]\]|<[^>]+>|\{[^}]+\}")

CODE_TO_LANG = {
    "en": "english",
    "es": "spanish",
    "de": "german",
    "fr": "french",
    "pb": "portuguese",
    "hu": "hungarian",
    "it": "italian",
    "ja": "japanese",
    "ko": "korean",
    "pl": "polish",
    "ru": "russian",
    "zh": "schinese",
    "zt": "tchinese",
    "th": "thai",
    "tr": "turkish",
    "uk": "ukrainian",
    "vi": "vietnamese",
}


def strip_placeholders(text: str) -> str:
    """Remove placeholder patterns from ``text``."""
    return _PLACEHOLDER_RE.sub("", text)


def has_words(text: str) -> bool:
    """Return ``True`` if ``text`` contains any word characters after stripping placeholders."""
    return bool(_WORD_RE.search(strip_placeholders(text)))


def contains_language(text: str, lang: str) -> bool:
    """Return ``True`` if the text appears to contain words from ``lang``."""
    cleaned = strip_placeholders(text)
    words = set(_WORD_RE.findall(cleaned.lower()))
    words -= ALLOWLIST.get(lang, set())
    stop_words = STOP_WORDS.get(lang)
    if not stop_words:
        return False
    return any(word in stop_words for word in words)


def contains_language_code(text: str, code: str) -> bool:
    """Return ``True`` if ``text`` appears to contain words for ISO ``code``."""
    lang = CODE_TO_LANG.get(code.lower())
    if not lang:
        return False
    return contains_language(text, lang)


def contains_english(text: str) -> bool:
    """Return ``True`` if the text appears to contain English words."""
    return contains_language(text, "english")
