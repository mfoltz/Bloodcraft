#!/usr/bin/env python3
import argparse
import json
import os
import re
import subprocess
from typing import List
from argostranslate import translate as argos_translate

MAX_ATTEMPTS = 3

RICHTEXT = re.compile(r'<[^>]+>')
PLACEHOLDER = re.compile(r'\{[^{}]+\}')
CSINTERP = re.compile(r'\$\{[^{}]+\}')
TOKEN_RE = re.compile(r'\[\[TOKEN_(\d+)\]\]')

ENGLISH_WORDS = re.compile(r'\b(the|and|of|to|with|you|your|for|a|an)\b', re.I)


def protect(text: str):
    text = text.replace("\\u003C", "<").replace("\\u003E", ">")
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
    languages = {l.code: l for l in argos_translate.get_installed_languages()}
    if src not in languages or dst not in languages:
        raise RuntimeError(f"Languages {src}->{dst} not installed")
    translator = languages[src].get_translation(languages[dst])
    return [translator.translate(line) for line in lines]


def main():
    ap = argparse.ArgumentParser(description="Translate message JSON files with Argos Translate")
    ap.add_argument("target_file", help="Path to the target language JSON file")
    ap.add_argument("--from", dest="src", default="en", help="Source language code (default: en)")
    ap.add_argument("--to", dest="dst", required=True, help="Target language code")
    ap.add_argument("--root", default=os.path.dirname(os.path.dirname(__file__)), help="Repo root")
    args = ap.parse_args()

    print("NOTE TO TRANSLATORS: **DO NOT** alter anything inside [[TOKEN_n]], <...> tags, or {...} variables.")

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

    queue = [(k, v, 0) for k, v in to_translate]
    translated = {}
    skipped: List[str] = []
    while queue:
        batch = queue
        queue = []
        safe_lines = []
        tokens_list = []
        for key, text, tries in batch:
            safe, tokens = protect(text)
            token_only = TOKEN_RE.sub("", safe).strip() == ""
            if token_only:
                safe += " TRANSLATE"
            safe_lines.append(safe)
            tokens_list.append((tokens, token_only, tries))
        try:
            results = translate_batch(args.src, args.dst, safe_lines)
        except Exception as e:
            print("Translation error", e)
            for key, text, tries in batch:
                if tries + 1 >= MAX_ATTEMPTS:
                    print(f"Skipping {key} after {MAX_ATTEMPTS} attempts")
                    skipped.append(key)
                else:
                    queue.append((key, text, tries + 1))
            continue
        for (key, text, tries), result, (tokens, token_only, _) in zip(batch, results, tokens_list):
            if token_only:
                result = result.replace(" TRANSLATE", "")
            if len(TOKEN_RE.findall(result)) != len(tokens):
                # translator mangled tokens
                if tries + 1 >= MAX_ATTEMPTS:
                    print(f"Skipping {key} after {MAX_ATTEMPTS} attempts")
                    skipped.append(key)
                else:
                    queue.append((key, text, tries + 1))
                continue
            un = unprotect(result, tokens)
            un = un.replace("\\u003C", "<").replace("\\u003E", ">")
            if un == text or contains_english(un):
                if tries + 1 >= MAX_ATTEMPTS:
                    print(f"Skipping {key} after {MAX_ATTEMPTS} attempts")
                    skipped.append(key)
                else:
                    queue.append((key, text, tries + 1))
                continue
            translated[key] = un

    messages.update(translated)
    target["Messages"] = messages
    with open(target_path, "w", encoding="utf-8") as f:
        json.dump(target, f, indent=2, ensure_ascii=False)

    print(f"Wrote translations to {target_path}")

    if skipped:
        print("Skipped the following message hashes due to repeated errors:")
        for k in skipped:
            print(f" - {k}")


if __name__ == "__main__":
    main()
