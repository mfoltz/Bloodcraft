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
import logging
from collections import Counter
from concurrent.futures import ThreadPoolExecutor, TimeoutError as FuturesTimeout
from typing import List

from argostranslate import translate as argos_translate
from language_utils import contains_english


logger = logging.getLogger("translate_argos")

FATAL_ARGOS_ERRORS = [
    "Unsupported model binary version",
]


class FatalTranslationError(Exception):
    """Raised when Argos Translate encounters an unrecoverable error."""

TOKEN_PATTERN = re.compile(
    r"<[^>]+>|\{[^{}]+\}|\$\{[^{}]+\}|\[(?:/?[a-zA-Z]+(?:=[^\]]+)?)\]|\[\[TOKEN_\d+\]\]|⟦T\d+⟧"
)
TOKEN_RE = re.compile(r'\[\[TOKEN_(\d+)\]\]')
STRICT_TOKEN_RE = re.compile(r'⟦T(\d+)⟧')
STRICT_TOKEN_CLEAN = re.compile(r'⟦\s*T\s*(\d+)\s*⟧', re.I)
TOKEN_CLEAN = re.compile(r'\[\s*TOKEN_(\d+)\s*\]', re.I)
# Matches stray TOKEN_n occurrences regardless of surrounding context
# Matches cases like ``TOKEN_1`` and ``TOKEN _ 1``
TOKEN_WORD = re.compile(r'TOKEN\s*_\s*(\d+)', re.I)
TOKEN_SENTINEL = "[[TOKEN_SENTINEL]]"
SENTINEL_ONLY_RE = re.compile(
    rf"^(?:\s*{re.escape(TOKEN_SENTINEL)})+\s*$"
)


def wrap_placeholders(text: str) -> str:
    """No-op placeholder wrapper using ``⟦Tn⟧`` markers."""
    return text


def unwrap_placeholders(text: str) -> str:
    """No-op placeholder unwrapper for ``⟦Tn⟧`` markers."""
    return text


def protect_strict(text: str) -> tuple[str, List[str]]:
    """Protect tokens using ``⟦Tn⟧`` markers for stricter isolation."""
    text = text.replace("\\u003C", "<").replace("\\u003E", ">")
    tokens: List[str] = []

    def store(m: re.Match) -> str:
        tokens.append(m.group(0))
        return f"⟦T{len(tokens)-1}⟧"

    # First replace nested interpolation blocks `{(...)}'` with temporary
    # markers so we can process remaining tokens via regex. The blocks may
    # contain arbitrary nested parentheses.
    nested: List[str] = []
    res: List[str] = []
    i = 0
    while i < len(text):
        if text[i] == "{":
            j = i + 1
            while j < len(text) and text[j].isspace():
                j += 1
            if j < len(text) and text[j] == "(":
                start = i
                i = j + 1
                depth = 1
                while i < len(text) and depth:
                    ch = text[i]
                    if ch == "(":
                        depth += 1
                    elif ch == ")":
                        depth -= 1
                    i += 1
                if depth == 0:
                    while i < len(text) and text[i].isspace():
                        i += 1
                    if i < len(text) and text[i] == "}":
                        i += 1
                        marker = f"@@NB{len(nested)}@@"
                        nested.append(text[start:i])
                        res.append(marker)
                        continue
                i = start
        res.append(text[i])
        i += 1
    text = "".join(res)

    # Handle standard tokens including `<...>`, `{...}`, `${...}`,
    # `[[TOKEN_n]]` markers, and existing ``⟦Tn⟧`` sentinels.
    text = TOKEN_PATTERN.sub(store, text)

    # Restore nested blocks with proper ordering.
    for idx, block in enumerate(nested):
        placeholder = f"⟦T{len(tokens)}⟧"
        text = text.replace(f"@@NB{idx}@@", placeholder, 1)
        tokens.append(block)

    return text, tokens


def unprotect(text: str, tokens: List[str]) -> str:
    def repl(m):
        idx = int(m.group(1))
        return tokens[idx] if 0 <= idx < len(tokens) else m.group(0)
    return TOKEN_RE.sub(repl, text)


def normalize_tokens(text: str) -> str:
    """Normalize token formatting in Argos output.

    Converts ``⟦Tn⟧`` placeholders back to ``[[TOKEN_n]]`` and cleans up
    spacing so existing tokens like ``{0}`` or ``${var}`` remain intact after
    translation.
    """

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
    """Renumber ``[[TOKEN_n]]`` markers to match English token order.

    The translator may permute or use one-based numbering for placeholders.
    This helper reindexes them sequentially starting from ``0`` so they can be
    mapped back to the original tokens. ``token_count`` is the number of tokens
    extracted from the English source.
    """
    if token_count <= 1:
        return text, False

    # Normalize any ``⟦Tn⟧`` markers before processing.
    text = STRICT_TOKEN_CLEAN.sub(lambda m: f"[[TOKEN_{m.group(1)}]]", text)

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
            wrapped = wrap_placeholders(line)
            for attempt in range(1, max_retries + 1):
                future = executor.submit(translator.translate, wrapped)
                try:
                    translated = future.result(timeout=timeout)
                    results.append(unwrap_placeholders(translated))
                    break
                except FuturesTimeout:
                    future.cancel()
                    if attempt == max_retries:
                        results.append("")
                        timed_out.append(idx)
                    else:
                        logger.warning(
                            f"Argos timed out on attempt {attempt}/{max_retries}"
                        )
                except Exception as e:
                    future.cancel()
                    msg = str(e)
                    if any(err in msg for err in FATAL_ARGOS_ERRORS):
                        raise FatalTranslationError(msg)
                    if attempt == max_retries:
                        raise RuntimeError(f"Translation failed: {msg}")
                    logger.warning(
                        f"Argos failed on attempt {attempt}/{max_retries}: {msg}"
                    )
    return results, timed_out


def _read_json(path: str, *, default=None, max_retries: int = 3):
    """Read JSON from ``path`` with retry logic."""
    for attempt in range(1, max_retries + 1):
        try:
            with open(path, "r", encoding="utf-8") as f:
                return json.load(f)
        except FileNotFoundError:
            if default is not None:
                return default
            raise
        except Exception:
            if attempt == max_retries:
                raise
            time.sleep(0.1)


def _write_json(path: str, data, *, max_retries: int = 3) -> None:
    """Write JSON ``data`` to ``path`` with retry logic."""
    for attempt in range(1, max_retries + 1):
        try:
            with open(path, "w", encoding="utf-8") as f:
                json.dump(data, f, indent=2, ensure_ascii=False)
            return
        except Exception:
            if attempt == max_retries:
                raise
            time.sleep(0.1)


def _write_report(path: str, rows: list[dict[str, str]], *, max_retries: int = 3) -> None:
    """Write translation ``rows`` to ``path`` as JSON or CSV."""
    report_dir = os.path.dirname(path)
    if report_dir:
        os.makedirs(report_dir, exist_ok=True)
    # Deduplicate by hash so reruns don't accumulate stale entries
    if rows:
        deduped: dict[str, dict[str, str]] = {}
        for row in rows:
            key = row.get("hash")
            if key:
                deduped[key] = row
        rows = list(deduped.values())
    ext = os.path.splitext(path)[1].lower()
    for attempt in range(1, max_retries + 1):
        try:
            with open(path, "w", encoding="utf-8", newline="") as fp:
                if ext == ".json":
                    json.dump(rows, fp, indent=2, ensure_ascii=False)
                elif ext == ".csv":
                    writer = csv.DictWriter(
                        fp, fieldnames=["hash", "english", "reason", "category"]
                    )
                    writer.writeheader()
                    writer.writerows(rows)
                else:
                    raise RuntimeError("Report file must end with .json or .csv")
            logger.warning(f"Wrote skip report to {path}")
            break
        except Exception:
            if attempt == max_retries:
                raise
            time.sleep(0.1)


def _run_translation(args, root: str) -> None:
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

    logger.warning(
        "NOTE TO TRANSLATORS: **DO NOT** alter anything inside [[TOKEN_n]], <...> tags, or {...} variables."
    )
    english_path = os.path.join(
        root, "Resources", "Localization", "Messages", "English.json"
    )
    target_path = os.path.join(root, args.target_file)

    english = _read_json(english_path)["Messages"]

    target = _read_json(target_path, default={"Messages": {}})

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
        logger.warning("No messages need translation.")
        return

    safe_lines: List[str] = []
    tokens_list: List[tuple[List[str], bool]] = []
    keys: List[str] = []

    for key, text in to_translate:
        safe, tokens = protect_strict(text)
        token_only = STRICT_TOKEN_RE.sub("", safe).strip() == ""
        if token_only:
            safe += f" {TOKEN_SENTINEL}"
        safe_lines.append(safe)
        tokens_list.append((tokens, token_only))
        keys.append(key)

    translated: dict[str, str] = {}
    failures: dict[str, tuple[str, str]] = {}
    report: dict[str, dict[str, str]] = {}
    processed_lines = len(safe_lines)
    timeouts_count = 0
    token_reorders = 0
    timed_out_hashes: set[str] = set()
    token_stats: dict[str, dict[str, int | bool]] = {}

    def categorize(reason: str | None) -> str:
        if not reason:
            return ""
        r = reason.lower()
        if "sentinel" in r:
            return "sentinel"
        if "identical" in r:
            return "identical"
        if "token mismatch" in r:
            return "token_mismatch"
        if "placeholder" in r or "tokens only" in r or "token only" in r:
            return "placeholder"
        if "untranslated" in r or "english" in r:
            return "english"
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
                report[key] = {
                    "hash": key,
                    "english": original,
                    "reason": reason,
                    "category": cat,
                }
            logger.warning(
                f"{msg}\n  Original: {original}\n  Result: {result}"
            )
        else:
            report.pop(key, None)
            logger.info(f"{msg}\n  Original: {original}\n  Result: {result}")


    num_batches = (len(safe_lines) + args.batch_size - 1) // args.batch_size
    start = time.perf_counter()
    try:
        for batch_idx in range(num_batches):
            i = batch_idx * args.batch_size
            batch_lines = safe_lines[i : i + args.batch_size]
            batch_keys = keys[i : i + args.batch_size]
            batch_tokens = tokens_list[i : i + args.batch_size]
            batch_start = time.perf_counter()
            logger.info(
                f"Batch {batch_idx + 1}/{num_batches} start @ {batch_start - start:.2f}s"
            )
            batch_results: List[str] = []
            timeouts: List[int] = []
            success = False
            for attempt in range(1, args.max_retries + 1):
                try:
                    batch_results, timeouts = translate_batch(
                        translator,
                        batch_lines,
                        max_retries=args.max_retries,
                        timeout=args.timeout,
                    )
                    success = True
                    break
                except FatalTranslationError as e:
                    msg = f"Fatal Argos error: {e}"
                    guidance = "Upgrade Argos Translate to support model version"
                    logger.error(msg)
                    logger.error(guidance)
                    raise SystemExit(msg)
                except Exception as e:
                    if attempt == args.max_retries:
                        logger.warning(f"Translation error: {e}")
                        for k in batch_keys:
                            reason = f"batch error: {e}"
                            category = categorize(reason)
                            log_entry(
                                k,
                                english[k],
                                "",
                                reason,
                                category=category,
                                record=False,
                            )
                            failures[k] = (reason, category)
                        batch_end = time.perf_counter()
                        logger.info(
                            f"Batch {batch_idx + 1}/{num_batches} end @ {batch_end - start:.2f}s "
                            f"({batch_end - batch_start:.2f}s)"
                        )
                        break
                    logger.warning(
                        f"Batch {batch_idx + 1} failed attempt {attempt}/{args.max_retries}: {e}"
                    )
                    time.sleep(0.1)
            if not success:
                continue

            for idx, (key, result, (tokens, token_only)) in enumerate(
                zip(batch_keys, batch_results, batch_tokens)
            ):
                try:
                    if idx in timeouts:
                        reason = "timeout"
                        category = categorize(reason)
                        log_entry(
                            key,
                            english[key],
                            "",
                            reason,
                            category=category,
                            record=False,
                        )
                        failures[key] = (reason, category)
                        timeouts_count += 1
                        timed_out_hashes.add(key)
                        token_stats[key] = {
                            "original_tokens": len(tokens),
                            "translated_tokens": 0,
                            "reordered": False,
                        }
                        continue
                    if token_only:
                        if TOKEN_SENTINEL not in result:
                            logger.debug(f"{key}: sentinel missing, reinserting")
                            result = f"{result} {TOKEN_SENTINEL}".strip()
                        if SENTINEL_ONLY_RE.fullmatch(result.strip()):
                            reason = "sentinel only"
                            category = categorize(reason)
                            log_entry(
                                key,
                                english[key],
                                english[key],
                                reason,
                                category=category,
                            )
                            failures[key] = (reason, category)
                            token_stats[key] = {
                                "original_tokens": len(tokens),
                                "translated_tokens": 0,
                                "reordered": False,
                            }
                            continue
                        result = result.replace(f" {TOKEN_SENTINEL}", "").replace(
                            TOKEN_SENTINEL, ""
                        )
                    result = normalize_tokens(result)
                    result, changed = reorder_tokens(result, len(tokens))
                    found_tokens = TOKEN_RE.findall(result)
                    token_stats[key] = {
                        "original_tokens": len(tokens),
                        "translated_tokens": len(found_tokens),
                        "reordered": changed,
                    }
                    if changed:
                        token_reorders += 1
                    stripped = TOKEN_RE.sub("", result)
                    if not token_only:
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
                            )
                            failures[key] = (reason, category)
                            continue
                    expected = [str(i) for i in range(len(tokens))]
                    reason: str | None = None
                    category: str | None = None
                    if set(found_tokens) != set(expected):
                        missing = [t for t in expected if t not in found_tokens]
                        extra = [t for t in found_tokens if t not in expected]
                        if missing:
                            result += "".join(
                                f" [[TOKEN_{m}]]" for m in missing
                            )
                            found_tokens.extend(missing)
                        token_stats[key]["translated_tokens"] = len(
                            found_tokens
                        )
                        parts = []
                        if missing:
                            parts.append(f"missing {missing}")
                        if extra:
                            parts.append(f"unexpected {extra}")
                        reason = "token mismatch (" + ", ".join(parts) + ")"
                        category = categorize(reason)
                    un = unprotect(result, tokens)
                    un = un.replace("\\u003C", "<").replace("\\u003E", ">")
                    if token_only:
                        translated[key] = un
                        log_entry(
                            key,
                            english[key],
                            un,
                            reason,
                            category=category,
                        )
                        if reason:
                            failures[key] = (reason, category)
                        continue
                    if un == english[key]:
                        reason = "identical to source"
                        category = categorize(reason)
                        log_entry(
                            key,
                            english[key],
                            un,
                            reason,
                            category=category,
                        )
                        translated[key] = un
                        failures[key] = (reason, category)
                        continue
                    if contains_english(un):
                        reason = "contains English"
                        category = categorize(reason)
                        log_entry(
                            key,
                            english[key],
                            un,
                            reason,
                            category=category,
                        )
                        translated[key] = english[key]
                        failures[key] = (reason, category)
                        continue
                    if changed:
                        reason = "tokens reordered"
                        category = categorize(reason)
                        log_entry(
                            key,
                            english[key],
                            un,
                            reason,
                            category=category,
                        )
                        translated[key] = un
                        failures[key] = (reason, category)
                        continue
                    translated[key] = un
                    if reason:
                        log_entry(
                            key,
                            english[key],
                            un,
                            reason,
                            category=category,
                        )
                        failures[key] = (reason, category)
                    else:
                        log_entry(key, english[key], un)
                except Exception as e:
                    reason = f"processing error: {e}"
                    category = categorize(reason)
                    log_entry(
                        key,
                        english[key],
                        "",
                        reason,
                        category=category,
                    )
                    failures[key] = (reason, category)
                    token_stats[key] = {
                        "original_tokens": len(tokens),
                        "translated_tokens": 0,
                        "reordered": False,
                    }
            batch_end = time.perf_counter()
            logger.info(
                f"Batch {batch_idx + 1}/{num_batches} end @ {batch_end - start:.2f}s "
                f"({batch_end - batch_start:.2f}s)"
            )
            if args.report_file:
                try:
                    _write_report(args.report_file, list(report.values()))
                except Exception as e:
                    logger.warning(f"Failed to flush skip report: {e}")

        def strict_retry(key: str) -> tuple[bool, str, int, bool, str | None]:
            nonlocal token_reorders
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
            except FatalTranslationError:
                raise
            except Exception as e:
                return False, f"batch error on strict retry: {e}", 0, False, None
            if timeouts:
                return False, "timeout on strict retry", 0, False, None
            result = results[0]
            if token_only:
                if TOKEN_SENTINEL not in result:
                    logger.debug(f"{key}: sentinel missing on strict retry, reinserting")
                    result = f"{result} {TOKEN_SENTINEL}".strip()
                if SENTINEL_ONLY_RE.fullmatch(result.strip()):
                    return False, "sentinel only on strict retry", 0, False, None
                result = result.replace(f" {TOKEN_SENTINEL}", "").replace(
                    TOKEN_SENTINEL, "",
                )
            result = normalize_tokens(result)
            result, changed = reorder_tokens(result, len(tokens))
            found_tokens = TOKEN_RE.findall(result)
            if changed and not token_stats.get(key, {}).get("reordered"):
                token_reorders += 1
                stripped = TOKEN_RE.sub("", result)
                if "[" in stripped or "]" in stripped:
                    return False, "stray brackets on strict retry", len(found_tokens), changed, None
            expected = [str(i) for i in range(len(tokens))]
            reason: str | None = None
            if set(found_tokens) != set(expected):
                missing = [t for t in expected if t not in found_tokens]
                extra = [t for t in found_tokens if t not in expected]
                if missing:
                    result += "".join(f" [[TOKEN_{m}]]" for m in missing)
                    found_tokens.extend(missing)
                parts = []
                if missing:
                    parts.append(f"missing {missing}")
                if extra:
                    parts.append(f"unexpected {extra}")
                reason = "token mismatch on strict retry (" + ", ".join(parts) + ")"
            un = unprotect(result, tokens)
            un = un.replace("\u003C", "<").replace("\u003E", ">")
            if un == english[key]:
                return False, "identical to source on strict retry", len(found_tokens), changed, None
            if contains_english(un):
                return False, "contains English on strict retry", len(found_tokens), changed, None
            return True, un, len(found_tokens), changed, reason

        for key, (_initial_reason, category) in list(failures.items()):
            if category == "placeholder":
                continue
            ok, out, tcount, changed, reason = strict_retry(key)
            token_stats.setdefault(
                key, {"original_tokens": len(protect_strict(english[key])[1])}
            )
            token_stats[key]["translated_tokens"] = tcount
            token_stats[key]["reordered"] = token_stats[key].get("reordered", False) or changed
            if ok:
                translated[key] = out
                if reason:
                    log_entry(key, english[key], out, reason, category=categorize(reason))
                    failures[key] = (reason, categorize(reason))
                else:
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
                if "timeout" in out and key not in timed_out_hashes:
                    timeouts_count += 1
        end = time.perf_counter()
        total_elapsed = end - start
        summary_msg = (
            f"Processed {num_batches} batches in {total_elapsed:.2f} seconds"
        )
        logger.warning(summary_msg)
        category_counts = Counter(entry["category"] for entry in report.values())
        if category_counts:
            breakdown_msg = "Report breakdown: " + ", ".join(
                f"{k}: {v}" for k, v in sorted(category_counts.items())
            )
        else:
            breakdown_msg = "Report breakdown: none"
        logger.warning(breakdown_msg)

        successes = processed_lines - len(failures)

        messages.update(translated)
        target["Messages"] = messages
        _write_json(target_path, target)

        result = subprocess.run(
            [
                sys.executable,
                os.path.join(os.path.dirname(__file__), "fix_tokens.py"),
                "--root",
                root,
                "--reorder",
                target_path,
            ]
        )
        if result.returncode != 0:
            raise SystemExit(result.returncode)

        logger.warning(f"Wrote translations to {target_path}")

        metrics_dir = os.path.dirname(args.metrics_file)
        if metrics_dir:
            os.makedirs(metrics_dir, exist_ok=True)
        metrics_entry = {
            "file": args.target_file,
            "timestamp": time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime()),
            "processed": processed_lines,
            "successes": successes,
            "timeouts": timeouts_count,
            "token_reorders": token_reorders,
            "failures": {k: v[0] for k, v in failures.items()},
            "hash_stats": token_stats,
        }
        metrics_log = _read_json(args.metrics_file, default=[])
        if not isinstance(metrics_log, list):
            metrics_log = []
        metrics_log.append(metrics_entry)
        _write_json(args.metrics_file, metrics_log)

        summary_line = (
            f"Summary: {successes}/{processed_lines} translated, {timeouts_count} timeouts, "
            f"{token_reorders} token reorders. Metrics written to {args.metrics_file}"
        )
        logger.warning(summary_line)
    finally:
        exc_type = sys.exc_info()[0]
        if args.report_file:
            try:
                _write_report(
                    args.report_file, list(report.values()), max_retries=args.max_retries
                )
            except Exception as e:
                logger.warning(f"Failed to write skip report: {e}")
                if exc_type is None:
                    raise


def _load_report(path: str) -> list[dict[str, str]]:
    """Return rows from a JSON or CSV skip report."""
    if not path or not os.path.exists(path):
        return []
    ext = os.path.splitext(path)[1].lower()
    try:
        with open(path, "r", encoding="utf-8", newline="") as fp:
            if ext == ".json":
                data = json.load(fp)
            elif ext == ".csv":
                data = list(csv.DictReader(fp))
            else:
                return []
    except Exception:
        return []
    deduped: dict[str, dict[str, str]] = {}
    for row in data:
        key = row.get("hash")
        if key:
            deduped[key] = row
    return list(deduped.values())

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
    ap.add_argument(
        "--verbose",
        action="store_true",
        help="Log per-message translation details",
    )
    ap.add_argument(
        "--log-file",
        help="Write log output to this file; missing directories are created",
    )
    ap.add_argument(
        "--report-file",
        help=(
            "Write skipped hashes and reasons to this JSON or CSV file; "
            "missing directories are created"
        ),
    )
    ap.add_argument(
        "--metrics-file",
        help=(
            "Append translation metrics to this JSON file; "
            "defaults to translate_metrics.json under --root"
        ),
    )
    args = ap.parse_args()

    root = os.path.abspath(args.root)

    logger.setLevel(logging.INFO if args.verbose else logging.WARNING)
    formatter = logging.Formatter("%(asctime)s %(levelname)s %(message)s")
    console_handler = logging.StreamHandler()
    console_handler.setFormatter(formatter)
    logger.addHandler(console_handler)
    if args.log_file:
        log_dir = os.path.dirname(args.log_file)
        if log_dir:
            os.makedirs(log_dir, exist_ok=True)
        file_handler = logging.FileHandler(args.log_file, encoding="utf-8")
        file_handler.setFormatter(formatter)
        logger.addHandler(file_handler)

    if args.metrics_file:
        metrics_path = (
            args.metrics_file
            if os.path.isabs(args.metrics_file)
            else os.path.join(root, args.metrics_file)
        )
    else:
        metrics_path = os.path.join(root, "translate_metrics.json")
    args.metrics_file = metrics_path

    if args.report_file:
        try:
            _write_report(args.report_file, [])
        except Exception as e:
            logger.warning(f"Failed to initialize report file: {e}")

    remaining: list[dict[str, str]] = []
    try:
        prev_hashes: set[str] | None = None
        while True:
            _run_translation(args, root)
            if not args.report_file:
                break
            remaining = _load_report(args.report_file)
            hashes = [row["hash"] for row in remaining]
            if not hashes or set(hashes) == prev_hashes:
                break
            prev_hashes = set(hashes)
            logger.warning(
                f"Retrying {len(hashes)} hash(es) from {args.report_file}"
            )
            args.hashes = hashes
        if remaining:
            logger.warning("Unresolved hashes after retries:")
            for row in remaining:
                logger.warning(f"{row['hash']}: {row.get('reason', '')}")
    except SystemExit as e:
        if e.code not in (0, None):
            msg = str(e)
            if not msg or msg == str(e.code):
                msg = f"Exited with code {e.code}"
            logger.error(msg)
        raise
    except Exception as e:
        msg = f"Unhandled exception: {e}"
        logger.exception(msg)
        raise SystemExit(1)
    finally:
        for handler in logger.handlers:
            handler.flush()
            handler.close()
        logger.handlers.clear()


if __name__ == "__main__":
    main()
