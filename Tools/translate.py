#!/usr/bin/env python3
import argparse
import json
import os
import re
import subprocess
from typing import List

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
    ap.add_argument("--batch-size", type=int, default=20, help="Number of lines to send per translation request")
    ap.add_argument("--max-retries", type=int, default=3, help="Maximum attempts per message before skipping")
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
    queue: List[tuple[str, str, int]] = [(k, v, 0) for k, v in english.items() if k not in messages]

    if not queue:
        print("No messages need translation.")
        return

    translated: dict[str, str] = {}
    skipped: List[str] = []

    while queue:
        batch = queue[: args.batch_size]
        del queue[: args.batch_size]

        safe_lines: List[str] = []
        meta: List[tuple[str, List[str], bool, str, int]] = []

        for key, text, attempts in batch:
            safe, tokens = protect(text)
            token_only = TOKEN_RE.sub("", safe).strip() == ""
            if token_only:
                safe += " TRANSLATE"
            safe_lines.append(safe)
            meta.append((key, tokens, token_only, text, attempts))

        try:
            results = translate_batch(args.src, args.dst, safe_lines)
            if len(results) != len(meta):
                raise RuntimeError("result count mismatch")
        except Exception as e:
            print("Translation error:", e)
            for key, _tokens, _tokonly, text, attempts in meta:
                attempts += 1
                if attempts <= args.max_retries:
                    queue.append((key, text, attempts))
                else:
                    print(f"Skipping {key}: exceeded max retries")
                    skipped.append(key)
            continue

        for (key, tokens, token_only, text, attempts), result in zip(meta, results):
            if token_only:
                result = result.replace(" TRANSLATE", "")
            if len(TOKEN_RE.findall(result)) != len(tokens):
                print(f"Requeueing {key}: token mismatch")
                attempts += 1
                if attempts <= args.max_retries:
                    queue.append((key, text, attempts))
                else:
                    print(f"Skipping {key}: exceeded max retries")
                    skipped.append(key)
                continue
            un = unprotect(result, tokens)
            un = un.replace("\\u003C", "<").replace("\\u003E", ">")
            if un == english[key] or contains_english(un):
                print(f"Requeueing {key}: looks untranslated")
                attempts += 1
                if attempts <= args.max_retries:
                    queue.append((key, text, attempts))
                else:
                    print(f"Skipping {key}: exceeded max retries")
                    skipped.append(key)
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
