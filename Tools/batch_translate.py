#!/usr/bin/env python3
import argparse
import json
import os
import re
import subprocess
from typing import List

RICHTEXT = re.compile(r'</?\w+[^>]*>')
PLACEHOLDER = re.compile(r'\{[^}]+\}')
CSINTERP = re.compile(r'\$\{[^}]+\}')
TOKEN_RE = re.compile(r'\[\[TOKEN_(\d+)\]\]')

ENGLISH_WORDS = re.compile(r'\b(the|and|of|to|with|you|your|for|a|an)\b', re.I)


def protect(text: str):
    tokens: List[str] = []
    for regex in (RICHTEXT, PLACEHOLDER, CSINTERP):
        text = regex.sub(lambda m: _store(tokens, m.group(0)), text)
    return text, tokens


def _store(tokens: List[str], value: str) -> str:
    tokens.append(value)
    return f"[[TOKEN_{len(tokens)-1}]]"


def unprotect(text: str, tokens: List[str]) -> str:
    def repl(m):
        idx = int(m.group(1))
        return tokens[idx] if 0 <= idx < len(tokens) else m.group(0)
    return TOKEN_RE.sub(repl, text)


def contains_english(text: str) -> bool:
    return bool(ENGLISH_WORDS.search(text))


def translate_batch(src: str, dst: str, lines: List[str]) -> List[str]:
    joined = "\n".join(lines)
    result = subprocess.run(
        ["argos-translate", "-f", src, "-t", dst],
        input=joined,
        text=True,
        capture_output=True,
    )
    if result.returncode != 0:
        raise RuntimeError(result.stderr)
    return result.stdout.strip().splitlines()


def main():
    ap = argparse.ArgumentParser(description="Translate message JSON files with Argos Translate")
    ap.add_argument("target_file", help="Path to the target language JSON file")
    ap.add_argument("--from", dest="src", default="en", help="Source language code (default: en)")
    ap.add_argument("--to", dest="dst", required=True, help="Target language code")
    ap.add_argument("--root", default=os.path.dirname(os.path.dirname(__file__)), help="Repo root")
    args = ap.parse_args()

    root = os.path.abspath(args.root)
    english_path = os.path.join(root, "Resources", "Localization", "Messages", "English.json")
    target_path = os.path.join(root, args.target_file)

    with open(english_path, "r", encoding="utf-8") as f:
        english = json.load(f)["Messages"]

    if os.path.exists(target_path):
        with open(target_path, "r", encoding="utf-8") as f:
            target = json.load(f)
    else:
        target = {"Messages": {}}

    messages = target.get("Messages", {})
    to_translate = [(k, v) for k, v in english.items() if k not in messages]

    queue = to_translate
    translated = {}
    while queue:
        batch = queue[:20]
        queue = queue[20:]
        safe_lines = []
        tokens_list = []
        keys = []
        for key, text in batch:
            safe, tokens = protect(text)
            safe_lines.append(safe)
            tokens_list.append(tokens)
            keys.append(key)
        try:
            results = translate_batch(args.src, args.dst, safe_lines)
        except Exception as e:
            print("Translation error", e)
            queue.extend(batch)
            continue
        for key, result, tokens in zip(keys, results, tokens_list):
            if TOKEN_RE.findall(result).count != len(tokens):
                # translator mangled tokens
                queue.append((key, english[key]))
                continue
            un = unprotect(result, tokens)
            if contains_english(un):
                queue.append((key, english[key]))
                continue
            translated[key] = un

    messages.update(translated)
    target["Messages"] = messages
    with open(target_path, "w", encoding="utf-8") as f:
        json.dump(target, f, indent=2, ensure_ascii=False)

    print(f"Wrote translations to {target_path}")


if __name__ == "__main__":
    main()
