#!/usr/bin/env python3
"""Shared token regex patterns and helpers."""

import re
from typing import List

# Placeholder token pattern ``[[...]]``
TOKEN_PLACEHOLDER = re.compile(r"\[\[[^\]]+\]\]")

# Match standard placeholder patterns:
#   * XML-like tags:        ``<tag>``
#   * Format items:         ``{0}`` or ``{PlayerName}``
#   * String interpolation: ``${var}``
#   * Bracket tags:         ``[tag]`` or ``[tag=value]``
#   * Existing tokens:      ``[[TOKEN_n]]``
#   * Percent sign:         ``%``
TOKEN_PATTERN = re.compile(
    r"<[^>]+>|\{[^{}]+\}|\$\{[^{}]+\}|\[(?:/?[a-zA-Z]+(?:=[^\]]+)?)\]|\[\[TOKEN_[0-9a-f]+\]\]|%"
)

TOKEN_RE = re.compile(r"\[\[TOKEN_([0-9a-f]+)\]\]")
TOKEN_OR_SENTINEL_RE = re.compile(r"\[\[TOKEN_(?:[0-9a-f]+|SENTINEL)\]\]", re.I)
TOKEN_CLEAN = re.compile(r"\[\s*TOKEN_([0-9a-f]+)\s*\](?!\])", re.I)
TOKEN_WORD = re.compile(r"TOKEN\s*_\s*([0-9a-f]+)", re.I)
TOKEN_SENTINEL = "[[TOKEN_SENTINEL]]"
SENTINEL_ONLY_RE = re.compile(rf"^(?:\s*{re.escape(TOKEN_SENTINEL)})+\s*$")


def extract_tokens(text: str) -> List[str]:
    """Return all token matches in ``text`` using :data:`TOKEN_PATTERN`."""
    return TOKEN_PATTERN.findall(text or "")

