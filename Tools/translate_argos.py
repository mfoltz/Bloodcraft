#!/usr/bin/env python3
"""Translate message JSON files using the argostranslate Python API.

Lines exceeding the timeout are skipped and listed in the report.
"""

import argparse
import csv
import json
import os
import re
import subprocess
import sys
from concurrent.futures import ThreadPoolExecutor, TimeoutError as FuturesTimeout
from typing import List

from argostranslate import translate as argos_translate
from language_utils import contains_english

RICHTEXT = re.compile(r'<[^>]+>')
PLACEHOLDER = re.compile(r'\{[^{}]+\}')
CSINTERP = re.compile(r'\$\{[^{}]+\}')
TOKEN_RE = re.compile(r'\[\[TOKEN_(\d+)\]\]')
TOKEN_CLEAN = re.compile(r'\[\s*TOKEN_(\d+)\s*\]', re.I)
# Matches stray TOKEN_n occurrences regardless of surrounding context
TOKEN_WORD = re.compile(r'TOKEN_\s*(\d+)', re.I)
TOKEN_SENTINEL = "[[TOKEN_SENTINEL]]"


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


def normalize_tokens(text: str) -> str:
    """Normalize token formatting in Argos output."""
    text = TOKEN_CLEAN.sub(lambda m: f"[[TOKEN_{m.group(1)}]]", text)
    text = TOKEN_WORD.sub(lambda m: f"[[TOKEN_{m.group(1)}]]", text)
    text = re.sub(r"\]\s+\[\[", "][[", text)

    placeholders: List[str] = []

    def store(m: re.Match) -> str:
        placeholders.append(m.group(0))
        return f"@@{len(placeholders)-1}@@"

    def restore(m: re.Match) -> str:
        return placeholders[int(m.group(1))]

    tmp = TOKEN_RE.sub(store, text)
    tmp = tmp.replace("[", "").replace("]", "")
    return re.sub(r"@@(\d+)@@", restore, tmp)


def ensure_model_installed(root: str, dst: str) -> None:
    """Verify split Argos model segments are combined and installed.

    Scans ``Resources/Localization/Models/<dst>`` for ``.z01`` segments.
    If segments exist but no ``.argosmodel`` is present, instruct the user to
    assemble and install the model.
    """
    model_dir = os.path.join(
        root, "Resources", "Localization", "Models", dst
    )
    if not os.path.isdir(model_dir):
        return

    entries = os.listdir(model_dir)
    has_segments = any(name.endswith(".z01") for name in entries)
    has_model = any(name.endswith(".argosmodel") for name in entries)
    if has_segments and not has_model:
        snippet = (
            f"cd Resources/Localization/Models/{dst}\n"
            "cat translate-*.z[0-9][0-9] translate-*.zip > model.zip\n"
            "unzip -o model.zip\n"
            "unzip -p translate-*.argosmodel */metadata.json | jq '.from_code, .to_code'\n"
            "argos-translate install translate-*.argosmodel"
        )
        raise RuntimeError(
            "Split Argos model segments detected without an installed model. "
            "Combine and install the model:\n" + snippet
        )
def translate_batch(
    translator,
    lines: List[str],
    *,
    max_retries: int,
    timeout: int,
) -> tuple[List[str], List[int]]:
    """Translate a list of lines using argostranslate.

    Any line taking longer than ``timeout`` seconds is aborted and noted by index.
    """
    results: List[str] = []
    timed_out: List[int] = []
    for idx, line in enumerate(lines):
        for attempt in range(1, max_retries + 1):
            with ThreadPoolExecutor(max_workers=1) as executor:
                future = executor.submit(translator.translate, line)
                try:
                    results.append(future.result(timeout=timeout))
                    break
                except FuturesTimeout:
                    future.cancel()
                    if attempt == max_retries:
                        results.append("")
                        timed_out.append(idx)
                    else:
                        print(
                            f"Argos timed out on attempt {attempt}/{max_retries}"
                        )
                except Exception as e:
                    future.cancel()
                    if attempt == max_retries:
                        raise RuntimeError(f"Translation failed: {e}")
                    print(
                        f"Argos failed on attempt {attempt}/{max_retries}: {e}"
                    )
    return results, timed_out


def main():
    ap = argparse.ArgumentParser(
        description="Translate message JSON files with Argos Translate",
        epilog="Reassemble and install the Argos model before running this script.",
    )
    ap.add_argument("target_file", help="Path to the target language JSON file")
    ap.add_argument("--from", dest="src", default="en", help="Source language code (default: en)")
    ap.add_argument("--to", dest="dst", required=True, help="Target language code")
    ap.add_argument("--root", default=os.path.dirname(os.path.dirname(__file__)), help="Repo root")
    ap.add_argument("--batch-size", type=int, default=100, help="Number of lines to translate per request")
    ap.add_argument(
        "--max-retries", type=int, default=3, help="Retry failed translations up to this many times"
    )
    ap.add_argument(
        "--timeout",
        type=int,
        default=60,
        help="Abort a single line if it takes longer than this many seconds",
    )
    ap.add_argument("--overwrite", action="store_true", help="Translate all messages even if already present")
    ap.add_argument("--verbose", action="store_true", help="Print per-message translation details")
    ap.add_argument(
        "--log-file",
        help="Write verbose output to this file; missing directories are created",
    )
    ap.add_argument(
        "--report-file",
        help=(
            "Write skipped hashes and reasons to this JSON or CSV file; "
            "missing directories are created"
        ),
    )
    args = ap.parse_args()

    root = os.path.abspath(args.root)

    translator = argos_translate.get_translation_from_codes(args.src, args.dst)
    if translator is None:
        ensure_model_installed(root, args.dst)
        argos_translate.load_installed_languages()
        translator = argos_translate.get_translation_from_codes(args.src, args.dst)
    if translator is None:
        raise RuntimeError(
            f"No Argos translation model for {args.src}->{args.dst}. "
            "Assemble or install the model, or run `.codex/install.sh`."
        )

    log_fp = None
    if args.log_file:
        log_dir = os.path.dirname(args.log_file)
        if log_dir:
            os.makedirs(log_dir, exist_ok=True)
        log_fp = open(args.log_file, "w", encoding="utf-8")

    def log_verbose(msg: str) -> None:
        if args.verbose:
            print(msg)
        if log_fp:
            log_fp.write(msg + "\n")

    print("NOTE TO TRANSLATORS: **DO NOT** alter anything inside [[TOKEN_n]], <...> tags, or {...} variables.")
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
            safe += f" {TOKEN_SENTINEL}"
        safe_lines.append(safe)
        tokens_list.append((tokens, token_only))
        keys.append(key)

    translated: dict[str, str] = {}
    skipped: List[str] = []
    report: list[dict[str, str]] = []

    def log_entry(key: str, original: str, result: str, reason: str | None = None) -> None:
        status = "SKIPPED" if reason else "TRANSLATED"
        msg = f"{key}: {status}"
        if reason:
            msg += f" ({reason})"
            report.append({"hash": key, "english": original, "reason": reason})
        msg += f"\n  Original: {original}\n  Result: {result}"
        log_verbose(msg)

    for i in range(0, len(safe_lines), args.batch_size):
        batch_lines = safe_lines[i : i + args.batch_size]
        batch_keys = keys[i : i + args.batch_size]
        batch_tokens = tokens_list[i : i + args.batch_size]
        try:
            batch_results, timeouts = translate_batch(
                translator,
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

        for idx, (key, result, (tokens, token_only)) in enumerate(
            zip(batch_keys, batch_results, batch_tokens)
        ):
            if idx in timeouts:
                log_entry(key, english[key], "", "timeout")
                skipped.append(key)
                continue
            if token_only:
                if TOKEN_SENTINEL not in result:
                    log_entry(key, english[key], result, "sentinel missing")
                    skipped.append(key)
                    continue
                result = result.replace(f" {TOKEN_SENTINEL}", "").replace(TOKEN_SENTINEL, "")
            result = normalize_tokens(result)

            stripped = TOKEN_RE.sub("", result)
            if "[" in stripped or "]" in stripped:
                log_entry(key, english[key], result, "stray brackets")
                skipped.append(key)
                continue

            found_tokens = TOKEN_RE.findall(result)
            expected = [str(i) for i in range(len(tokens))]
            if set(found_tokens) != set(expected):
                reason = f"token mismatch (expected {expected}, got {found_tokens})"
                log_entry(key, english[key], result, reason)
                skipped.append(key)
                continue
            if found_tokens != expected:
                log_verbose(
                    f"WARNING {key}: token order differs (expected {expected}, got {found_tokens})"
                )
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

    subprocess.run(
        [
            sys.executable,
            os.path.join(os.path.dirname(__file__), "fix_tokens.py"),
            "--root",
            root,
        ],
        check=True,
    )

    print(f"Wrote translations to {target_path}")

    if log_fp:
        log_fp.close()

    if args.report_file:
        report_dir = os.path.dirname(args.report_file)
        if report_dir:
            os.makedirs(report_dir, exist_ok=True)
        ext = os.path.splitext(args.report_file)[1].lower()
        with open(args.report_file, "w", encoding="utf-8", newline="") as fp:
            if ext == ".json":
                json.dump(report, fp, indent=2, ensure_ascii=False)
            elif ext == ".csv":
                writer = csv.DictWriter(fp, fieldnames=["hash", "english", "reason"])
                writer.writeheader()
                writer.writerows(report)
            else:
                raise RuntimeError("Report file must end with .json or .csv")
        print(f"Wrote skip report to {args.report_file}")

    if skipped:
        print("Skipped the following message hashes due to repeated errors:")
        for k in skipped:
            print(f" - {k}")


if __name__ == "__main__":
    main()
