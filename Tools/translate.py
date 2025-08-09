#!/usr/bin/env python3
import argparse
import json
import os
import re
import subprocess
import sys
from typing import List

from argostranslate import translate as argos_translate
from language_utils import contains_english

print(
    "WARNING: Tools/translate.py is deprecated. Use Tools/translate_argos.py instead.",
    file=sys.stderr,
)

RICHTEXT = re.compile(r'<[^>]+>')
PLACEHOLDER = re.compile(r'\{[^{}]+\}')
CSINTERP = re.compile(r'\$\{[^{}]+\}')
TOKEN_RE = re.compile(r'\[\[TOKEN_(\d+)\]\]')


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


def translate_batch(
    src: str,
    dst: str,
    lines: List[str],
    *,
    max_retries: int,
    timeout: int,
) -> List[str]:
    argos_translate.load_installed_languages()
    translator = argos_translate.get_translation_from_codes(src, dst)
    if translator is None:
        raise RuntimeError(
            f"No Argos translation model for {src}->{dst}. "
            "Assemble or install the model, or run `.codex/install.sh`."
        )
    joined = "\n".join(lines)
    for attempt in range(1, max_retries + 1):
        try:
            result = subprocess.run(
                ["argos-translate", "-f", src, "-t", dst],
                input=joined,
                text=True,
                capture_output=True,
                timeout=timeout,
            )
            if result.returncode == 0:
                return result.stdout.strip().splitlines()
            print(
                f"argos-translate exited with {result.returncode} on attempt {attempt}/{max_retries}"
            )
        except subprocess.TimeoutExpired:
            print(
                f"argos-translate timed out after {timeout}s on attempt {attempt}/{max_retries}"
            )
    raise RuntimeError("Translation failed after maximum retries")


def main():
    ap = argparse.ArgumentParser(description="Translate message JSON files with Argos Translate")
    ap.add_argument("target_file", help="Path to the target language JSON file")
    ap.add_argument("--from", dest="src", default="en", help="Source language code (default: en)")
    ap.add_argument("--to", dest="dst", required=True, help="Target language code")
    ap.add_argument("--root", default=os.path.dirname(os.path.dirname(__file__)), help="Repo root")
    ap.add_argument("--batch-size", type=int, default=100, help="Number of lines to translate per request")
    ap.add_argument("--max-retries", type=int, default=3, help="Retry failed batches up to this many times")
    ap.add_argument(
        "--timeout",
        type=int,
        default=60,
        help="Seconds to wait for argos-translate before giving up",
    )
    ap.add_argument("--overwrite", action="store_true", help="Translate all messages even if already present")
    ap.add_argument("--verbose", action="store_true", help="Print per-message translation details")
    ap.add_argument("--log-file", help="Write verbose output to this file")
    args = ap.parse_args()

    log_fp = open(args.log_file, "w", encoding="utf-8") if args.log_file else None

    def log_verbose(msg: str) -> None:
        if args.verbose:
            print(msg)
        if log_fp:
            log_fp.write(msg + "\n")

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
    if args.overwrite:
        to_translate = list(english.items())
        messages = {}
    else:
        to_translate = [(k, v) for k, v in english.items() if k not in messages]

    if not to_translate:
        print("No messages need translation.")
        return

    safe_lines: List[str] = []
    tokens_list: List[tuple[List[str], bool]] = []
    keys: List[str] = []

    for key, text in to_translate:
        safe, tokens = protect(text)
        token_only = TOKEN_RE.sub("", safe).strip() == ""
        if token_only:
            safe += " TRANSLATE"
        safe_lines.append(safe)
        tokens_list.append((tokens, token_only))
        keys.append(key)

    translated: dict[str, str] = {}
    skipped: List[str] = []

    def log_entry(key: str, original: str, result: str, reason: str | None = None) -> None:
        status = "SKIPPED" if reason else "TRANSLATED"
        msg = f"{key}: {status}"
        if reason:
            msg += f" ({reason})"
        msg += f"\n  Original: {original}\n  Result: {result}"
        log_verbose(msg)

    for i in range(0, len(safe_lines), args.batch_size):
        batch_lines = safe_lines[i : i + args.batch_size]
        batch_keys = keys[i : i + args.batch_size]
        batch_tokens = tokens_list[i : i + args.batch_size]
        try:
            batch_results = translate_batch(
                args.src,
                args.dst,
                batch_lines,
                max_retries=args.max_retries,
                timeout=args.timeout,
            )
        except Exception as e:
            log_verbose(f"Translation error: {e}")
            for k in batch_keys:
                log_entry(k, english[k], "", f"batch error: {e}")
            skipped.extend(batch_keys)
            continue

        for key, result, (tokens, token_only) in zip(
            batch_keys, batch_results, batch_tokens
        ):
            if token_only:
                result = result.replace(" TRANSLATE", "")
            if len(TOKEN_RE.findall(result)) != len(tokens):
                log_entry(key, english[key], result, "token mismatch")
                skipped.append(key)
                continue
            un = unprotect(result, tokens)
            un = un.replace("\\u003C", "<").replace("\\u003E", ">")
            if un == english[key] or contains_english(un):
                log_entry(key, english[key], un, "looks untranslated")
                skipped.append(key)
                continue
            translated[key] = un
            log_entry(key, english[key], un)

    messages.update(translated)
    target["Messages"] = messages
    with open(target_path, "w", encoding="utf-8") as f:
        json.dump(target, f, indent=2, ensure_ascii=False)

    print(f"Wrote translations to {target_path}")

    if log_fp:
        log_fp.close()

    if skipped:
        print("Skipped the following message hashes due to repeated errors:")
        for k in skipped:
            print(f" - {k}")


if __name__ == "__main__":
    main()
