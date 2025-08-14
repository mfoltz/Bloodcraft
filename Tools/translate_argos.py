#!/usr/bin/env python3
"""Translate message JSON files using the argostranslate Python API.

Lines exceeding the timeout are skipped and listed in the report.
Use ``--hash`` to restrict translation to specific hashes for targeted updates.
"""

import argparse
import csv
import json
import os
import re
import subprocess
import sys
import time
import traceback
from collections import Counter
from concurrent.futures import ThreadPoolExecutor, TimeoutError as FuturesTimeout
from typing import List

from argostranslate import translate as argos_translate
from language_utils import contains_english

RICHTEXT = re.compile(r'<[^>]+>')
PLACEHOLDER = re.compile(r'\{[^{}]+\}')
CSINTERP = re.compile(r'\$\{[^{}]+\}')
TOKEN_RE = re.compile(r'\[\[TOKEN_(\d+)\]\]')
STRICT_TOKEN_RE = re.compile(r'__T(\d+)__')
STRICT_TOKEN_CLEAN = re.compile(r'__\s*T\s*(\d+)\s*__', re.I)
TOKEN_CLEAN = re.compile(r'\[\s*TOKEN_(\d+)\s*\]', re.I)
# Matches stray TOKEN_n occurrences regardless of surrounding context
# Matches cases like ``TOKEN_1`` and ``TOKEN _ 1``
TOKEN_WORD = re.compile(r'TOKEN\s*_\s*(\d+)', re.I)
TOKEN_SENTINEL = "[[TOKEN_SENTINEL]]"
INTERP_BLOCK = re.compile(r'\{\(')


def protect_strict(text: str) -> tuple[str, List[str]]:
    """Protect tokens using ``__Tn__`` markers for stricter isolation."""
    text = text.replace("\\u003C", "<").replace("\\u003E", ">")
    tokens: List[str] = []
    for regex in (RICHTEXT, PLACEHOLDER, CSINTERP):
        def store(m: re.Match) -> str:
            tokens.append(m.group(0))
            return f"__T{len(tokens)-1}__"
        text = regex.sub(store, text)
    return text, tokens


def unprotect(text: str, tokens: List[str]) -> str:
    def repl(m):
        idx = int(m.group(1))
        return tokens[idx] if 0 <= idx < len(tokens) else m.group(0)
    return TOKEN_RE.sub(repl, text)


def normalize_tokens(text: str) -> str:
    """Normalize token formatting in Argos output."""
    text = re.sub(r"\]\s+\[\[", "][[", text)

    # Merge cases where Argos splits multi-digit token numbers.
    # ``[[TOKEN_1 0]]`` -> ``[[TOKEN_10]]``
    def merge_inner(m: re.Match) -> str:
        digits = re.sub(r"\s+", "", m.group(1))
        return f"[[TOKEN_{digits}]]"

    text = re.sub(r"\[\[TOKEN_((?:\d\s*)+\d)\]\]", merge_inner, text)

    # ``[[TOKEN_1]][[TOKEN_0]]`` -> ``[[TOKEN_10]]``
    def merge_adjacent(m: re.Match) -> str:
        digits = re.findall(r"TOKEN_(\d)", m.group(0))
        return f"[[TOKEN_{''.join(digits)}]]"

    text = re.sub(r"(?:\[\[TOKEN_\d\]\]){2,}", merge_adjacent, text)

    text = STRICT_TOKEN_CLEAN.sub(lambda m: f"[[TOKEN_{m.group(1)}]]", text)
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


def reorder_tokens(text: str, token_count: int) -> tuple[str, bool]:
    """Renumber TOKEN_n markers to match the English source order.

    Argos may emit tokens with non-sequential or permuted indices. This
    function rewrites them so ``[[TOKEN_0]]`` through
    ``[[TOKEN_{token_count-1}]]`` appear in their expected order. The token
    positions in the string are preserved; only the numeric identifiers are
    remapped.
    """
    found = TOKEN_RE.findall(text)
    if len(found) != token_count:
        return text, False
    expected = [str(i) for i in range(token_count)]
    if found == expected:
        return text, False

    mapping: dict[str, str] = {}
    for old, new in zip(found, expected):
        mapping.setdefault(old, new)

    remapped = TOKEN_RE.sub(
        lambda m: f"[[TOKEN_{mapping.get(m.group(1), m.group(1))}]]", text
    )
    return remapped, True


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
    with ThreadPoolExecutor(max_workers=1) as executor:
        for idx, line in enumerate(lines):
            for attempt in range(1, max_retries + 1):
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


def _run_translation(args, root: str, log_fp) -> None:
    translator = argos_translate.get_translation_from_codes(args.src, args.dst)
    if translator is None:
        ensure_model_installed(root, args.dst)
        argos_translate.load_installed_languages()
        translator = argos_translate.get_translation_from_codes(args.src, args.dst)
    if translator is None:
        raise SystemExit(
            f"No Argos translation model for {args.src}->{args.dst}. "
            "Assemble or install the model, or run `.codex/install.sh`."
        )

    def log_verbose(msg: str) -> None:
        if args.verbose:
            print(msg)
        if log_fp:
            log_fp.write(msg + "\n")

    print(
        "NOTE TO TRANSLATORS: **DO NOT** alter anything inside [[TOKEN_n]], <...> tags, or {...} variables."
    )
    english_path = os.path.join(
        root, "Resources", "Localization", "Messages", "English.json"
    )
    target_path = os.path.join(root, args.target_file)

    with open(english_path, "r", encoding="utf-8") as f:
        english = json.load(f)["Messages"]

    if os.path.exists(target_path):
        with open(target_path, "r", encoding="utf-8") as f:
            target = json.load(f)
    else:
        target = {"Messages": {}}

    messages = target.get("Messages", {})
    if args.hashes:
        requested = set(args.hashes)
        to_translate = [(k, v) for k, v in english.items() if k in requested]
    elif args.overwrite:
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
    skipped_interp: List[tuple[str, str]] = []

    for key, text in to_translate:
        if INTERP_BLOCK.search(text):
            skipped_interp.append((key, text))
            continue
        safe, tokens = protect_strict(text)
        token_only = STRICT_TOKEN_RE.sub("", safe).strip() == ""
        if token_only:
            safe += f" {TOKEN_SENTINEL}"
        safe_lines.append(safe)
        tokens_list.append((tokens, token_only))
        keys.append(key)

    translated: dict[str, str] = {}
    failures: dict[str, tuple[str, str]] = {}
    report: list[dict[str, str]] = []
    category_counts: Counter[str] = Counter()

    def categorize(reason: str | None) -> str:
        if not reason:
            return ""
        r = reason.lower()
        if "sentinel" in r:
            return "sentinel"
        if "identical" in r:
            return "identical"
        if "untranslated" in r or "english" in r:
            return "untranslated"
        if "interpolation" in r:
            return "interpolation"
        if "placeholder" in r or "tokens only" in r or "token only" in r:
            return "placeholder"
        return "other"

    def log_entry(
        key: str,
        original: str,
        result: str,
        reason: str | None = None,
        *,
        category: str | None = None,
        record: bool = True,
    ) -> None:
        status = "SKIPPED" if reason else "TRANSLATED"
        msg = f"{key}: {status}"
        if reason:
            msg += f" ({reason})"
            if record:
                cat = category or categorize(reason)
                report.append(
                    {
                        "hash": key,
                        "english": original,
                        "reason": reason,
                        "category": cat,
                    }
                )
                category_counts[cat] += 1
        msg += f"\n  Original: {original}\n  Result: {result}"
        log_verbose(msg)

    for key, text in skipped_interp:
        reason = "interpolation block"
        category = "interpolation"
        log_entry(key, english[key], english[key], reason, category=category)
        translated[key] = english[key]
        failures[key] = (reason, category)

    num_batches = (len(safe_lines) + args.batch_size - 1) // args.batch_size
    start = time.perf_counter()

    for batch_idx in range(num_batches):
        i = batch_idx * args.batch_size
        batch_lines = safe_lines[i : i + args.batch_size]
        batch_keys = keys[i : i + args.batch_size]
        batch_tokens = tokens_list[i : i + args.batch_size]
        batch_start = time.perf_counter()
        log_verbose(
            f"Batch {batch_idx + 1}/{num_batches} start @ {batch_start - start:.2f}s"
        )
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
                reason = f"batch error: {e}"
                category = categorize(reason)
                log_entry(k, english[k], "", reason, category=category, record=False)
                failures[k] = (reason, category)
            batch_end = time.perf_counter()
            log_verbose(
                f"Batch {batch_idx + 1}/{num_batches} end @ {batch_end - start:.2f}s "
                f"({batch_end - batch_start:.2f}s)"
            )
            continue

        for idx, (key, result, (tokens, token_only)) in enumerate(
            zip(batch_keys, batch_results, batch_tokens)
        ):
            if idx in timeouts:
                reason = "timeout"
                category = categorize(reason)
                log_entry(key, english[key], "", reason, category=category, record=False)
                failures[key] = (reason, category)
                continue
            if token_only:
                if TOKEN_SENTINEL not in result:
                    reason = "sentinel missing"
                    category = categorize(reason)
                    log_entry(
                        key,
                        english[key],
                        english[key],
                        reason,
                        category=category,
                    )
                    failures[key] = (reason, category)
                    continue
                result = result.replace(f" {TOKEN_SENTINEL}", "").replace(
                    TOKEN_SENTINEL, ""
                )
            result = normalize_tokens(result)
            result, changed = reorder_tokens(result, len(tokens))
            stripped = TOKEN_RE.sub("", result)
            if stripped.strip() == "":
                reason = "placeholders only"
                category = categorize(reason)
                log_entry(
                    key,
                    english[key],
                    result,
                    reason,
                    category=category,
                )
                translated[key] = english[key]
                failures[key] = (reason, category)
                continue
            if "[" in stripped or "]" in stripped:
                reason = "stray brackets"
                category = categorize(reason)
                log_entry(
                    key,
                    english[key],
                    result,
                    reason,
                    category=category,
                    record=False,
                )
                failures[key] = (reason, category)
                continue
            found_tokens = TOKEN_RE.findall(result)
            expected = [str(i) for i in range(len(tokens))]
            if set(found_tokens) != set(expected):
                reason = f"token mismatch (expected {expected}, got {found_tokens})"
                category = categorize(reason)
                log_entry(
                    key,
                    english[key],
                    result,
                    reason,
                    category=category,
                    record=False,
                )
                failures[key] = (reason, category)
                continue
            un = unprotect(result, tokens)
            un = un.replace("\\u003C", "<").replace("\\u003E", ">")
            if un == english[key]:
                reason = "identical to source"
                category = categorize(reason)
                log_entry(key, english[key], un, reason, category=category)
                translated[key] = un
                failures[key] = (reason, category)
                continue
            if contains_english(un):
                reason = "contains English"
                category = categorize(reason)
                log_entry(key, english[key], un, reason, category=category)
                translated[key] = english[key]
                failures[key] = (reason, category)
                continue
            if changed:
                reason = "tokens reordered"
                category = categorize(reason)
                log_entry(key, english[key], un, reason, category=category)
                translated[key] = un
                failures[key] = (reason, category)
                continue
            translated[key] = un
            log_entry(key, english[key], un)

        batch_end = time.perf_counter()
        log_verbose(
            f"Batch {batch_idx + 1}/{num_batches} end @ {batch_end - start:.2f}s "
            f"({batch_end - batch_start:.2f}s)"
        )

    def strict_retry(key: str) -> tuple[bool, str]:
        safe, tokens = protect_strict(english[key])
        token_only = STRICT_TOKEN_RE.sub("", safe).strip() == ""
        if token_only:
            safe += f" {TOKEN_SENTINEL}"
        try:
            results, timeouts = translate_batch(
                translator,
                [safe],
                max_retries=args.max_retries,
                timeout=args.timeout,
            )
        except Exception as e:
            return False, f"batch error on strict retry: {e}"
        if timeouts:
            return False, "timeout on strict retry"
        result = results[0]
        if token_only:
            if TOKEN_SENTINEL not in result:
                return False, "sentinel missing on strict retry"
            result = result.replace(f" {TOKEN_SENTINEL}", "").replace(
                TOKEN_SENTINEL, ""
            )
        result = normalize_tokens(result)
        result, _ = reorder_tokens(result, len(tokens))
        stripped = TOKEN_RE.sub("", result)
        if "[" in stripped or "]" in stripped:
            return False, "stray brackets on strict retry"
        found_tokens = TOKEN_RE.findall(result)
        expected = [str(i) for i in range(len(tokens))]
        if set(found_tokens) != set(expected):
            return False, f"token mismatch on strict retry (expected {expected}, got {found_tokens})"
        un = unprotect(result, tokens)
        un = un.replace("\\u003C", "<").replace("\\u003E", ">")
        if un == english[key]:
            return False, "identical to source on strict retry"
        if contains_english(un):
            return False, "contains English on strict retry"
        return True, un

    for key, (_initial_reason, category) in list(failures.items()):
        if category in ("interpolation", "placeholder"):
            continue
        ok, out = strict_retry(key)
        if ok:
            translated[key] = out
            log_entry(key, english[key], out)
            failures.pop(key, None)
        else:
            category = categorize(out)
            translated[key] = english[key]
            failures[key] = (out, category)
            log_entry(
                key,
                english[key],
                english[key],
                out,
                category=category,
            )
    end = time.perf_counter()
    total_elapsed = end - start
    summary_msg = (
        f"Processed {num_batches} batches in {total_elapsed:.2f} seconds"
    )
    log_verbose(summary_msg)
    if not args.verbose:
        print(summary_msg)
    if category_counts:
        breakdown_msg = "Report breakdown: " + ", ".join(
            f"{k}: {v}" for k, v in sorted(category_counts.items())
        )
    else:
        breakdown_msg = "Report breakdown: none"
    log_verbose(breakdown_msg)
    if not args.verbose:
        print(breakdown_msg)

    translated_count = len(translated) - len(failures)
    skipped_count = len(failures)
    counts_msg = (
        f"Translation results: {translated_count} translated, {skipped_count} skipped"
    )
    log_verbose(counts_msg)
    if not args.verbose:
        print(counts_msg)

    messages.update(translated)
    target["Messages"] = messages
    with open(target_path, "w", encoding="utf-8") as f:
        json.dump(target, f, indent=2, ensure_ascii=False)

    result = subprocess.run(
        [
            sys.executable,
            os.path.join(os.path.dirname(__file__), "fix_tokens.py"),
            "--check-only",
            target_path,
        ]
    )
    if result.returncode != 0:
        raise SystemExit(result.returncode)

    print(f"Wrote translations to {target_path}")

    if args.report_file:
        report_dir = os.path.dirname(args.report_file)
        if report_dir:
            os.makedirs(report_dir, exist_ok=True)
        ext = os.path.splitext(args.report_file)[1].lower()
        with open(args.report_file, "w", encoding="utf-8", newline="") as fp:
            if ext == ".json":
                json.dump(report, fp, indent=2, ensure_ascii=False)
            elif ext == ".csv":
                writer = csv.DictWriter(
                    fp, fieldnames=["hash", "english", "reason", "category"]
                )
                writer.writeheader()
                writer.writerows(report)
            else:
                raise RuntimeError("Report file must end with .json or .csv")
        print(f"Wrote skip report to {args.report_file}")

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
        "--max-retries", type=int, default=3, help="Retry failed translations up to this many times",
    )
    ap.add_argument(
        "--timeout",
        type=int,
        default=60,
        help="Abort a single line if it takes longer than this many seconds",
    )
    ap.add_argument(
        "--overwrite",
        action="store_true",
        help=(
            "Translate all messages even if already present; "
            "without this flag only missing entries are processed. "
            "Use sparingly to avoid reprocessing thousands of lines."
        ),
    )
    ap.add_argument(
        "--hash",
        dest="hashes",
        action="append",
        metavar="HASH",
        help="Only translate entries matching this hash; can be repeated for targeted updates",
    )
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

    log_fp = None
    if args.log_file:
        log_dir = os.path.dirname(args.log_file)
        if log_dir:
            os.makedirs(log_dir, exist_ok=True)
        log_fp = open(args.log_file, "w", encoding="utf-8")

    root = os.path.abspath(args.root)

    try:
        _run_translation(args, root, log_fp)
    except SystemExit as e:
        if e.code not in (0, None):
            msg = str(e)
            if not msg or msg == str(e.code):
                msg = f"Exited with code {e.code}"
            print(msg, file=sys.stderr)
            if log_fp:
                log_fp.write(msg + "\n")
        raise
    except Exception as e:
        msg = f"Unhandled exception: {e}"
        print(msg, file=sys.stderr)
        if log_fp:
            log_fp.write(msg + "\n")
            log_fp.write(traceback.format_exc() + "\n")
        raise SystemExit(1)
    finally:
        if log_fp:
            log_fp.close()


if __name__ == "__main__":
    main()
