#!/usr/bin/env python3
"""Normalize placeholder tokens without requiring Argos dependencies."""

import re
from typing import List

from token_patterns import TOKEN_CLEAN, TOKEN_PATTERN, TOKEN_RE, TOKEN_SENTINEL, TOKEN_WORD


def normalize_tokens(text: str) -> str:
    """Normalize token formatting consistent with Argos-aware tooling."""

    def to_token(tok: str) -> str:
        return f"[[TOKEN_{tok}]]"

    # Normalise potential variations of the sentinel token.
    text = re.sub(
        r"\[\[\s*TOKEN\s*_?\s*SENTINEL\s*\]\]|TOKEN\s*_?\s*SENTINEL",
        TOKEN_SENTINEL,
        text,
        flags=re.I,
    )
    text = text.replace(f" {TOKEN_SENTINEL}", "").replace(TOKEN_SENTINEL, "")

    # Canonicalise any ``[[TOKEN_<id>]]`` with stray spacing or split digits.
    text = re.sub(
        r"\[\[\s*TOKEN\s*_?\s*((?:[0-9a-f]\s*)+)\s*\]\]",
        lambda m: to_token(re.sub(r"\s+", "", m.group(1))),
        text,
        flags=re.I,
    )
    text = re.sub(
        r"\[\[\s*TOKEN\s*_?\s*([^\]\s]+)\s*\]\]",
        lambda m: to_token(m.group(1).strip()),
        text,
        flags=re.I,
    )

    # Normalise single-bracket forms like ``[TOKEN_4]``.
    text = TOKEN_CLEAN.sub(lambda m: to_token(m.group(1)), text)

    # Catch bare ``TOKEN_x`` words and rewrite them to placeholder form.
    text = TOKEN_WORD.sub(lambda m: to_token(m.group(1)), text)

    # After tokens are normalized, strip any stray brackets Argos may emit.
    placeholders: List[str] = []

    def store(m: re.Match) -> str:
        placeholders.append(m.group(0))
        return f"@@{len(placeholders)-1}@@"

    def restore(m: re.Match) -> str:
        return placeholders[int(m.group(1))]

    tmp = TOKEN_PATTERN.sub(store, text)
    tmp = tmp.replace("[", "").replace("]", "")

    def drop_unmatched_brackets(s: str) -> str:
        pairs = {"[": "]", "(": ")", "{": "}", "<": ">"}
        opens = set(pairs)
        closes = set(pairs.values())
        stack: List[tuple[str, int]] = []
        remove: set[int] = set()
        for idx, ch in enumerate(s):
            if ch in opens:
                stack.append((ch, idx))
            elif ch in closes:
                if stack and pairs[stack[-1][0]] == ch:
                    stack.pop()
                else:
                    remove.add(idx)
        remove.update(pos for _, pos in stack)
        if not remove:
            return s
        return "".join(ch for i, ch in enumerate(s) if i not in remove)

    tmp = drop_unmatched_brackets(tmp)
    restored = re.sub(r"@@(\d+)@@", restore, tmp)

    def remove_unmatched_tags(s: str) -> str:
        """Strip stray opening or closing tags for any element name."""
        pattern = re.compile(r"</?(\w+)(?:[^<>]*)>")
        stack: List[tuple[str, int, int]] = []
        remove: List[tuple[int, int]] = []
        for m in pattern.finditer(s):
            tag = m.group(0)
            name = m.group(1).lower()
            start, end = m.span()
            if tag.startswith("</"):
                if stack and stack[-1][0] == name:
                    stack.pop()
                else:
                    remove.append((start, end))
            elif not tag.endswith("/>"):
                stack.append((name, start, end))
        remove.extend((start, end) for _, start, end in stack)
        if not remove:
            return s
        result: List[str] = []
        last = 0
        for start, end in sorted(remove):
            result.append(s[last:start])
            last = end
        result.append(s[last:])
        return "".join(result)

    restored = remove_unmatched_tags(restored)

    # Trim whitespace around format tokens within colour tags.
    restored = re.sub(
        r"(<color[^>]*>)\s*(\{[^{}]+\})\s*(</color>)",
        lambda m: f"{m.group(1)}{m.group(2)}{m.group(3)}",
        restored,
        flags=re.I,
    )
    return restored
