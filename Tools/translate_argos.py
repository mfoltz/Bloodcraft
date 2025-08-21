#!/usr/bin/env python3
"""Translate message JSON files using the argostranslate Python API.

Lines exceeding the timeout are skipped and listed in the report.
Use ``--hash`` to restrict translation to specific hashes for targeted updates.

After each run, summarise results and fail fast on remaining issues with
``python Tools/validate_translation_run.py --run-dir <directory>``.
"""

import argparse
import csv
import json
import os
import re
import subprocess
import sys
import time
import logging
import uuid
import importlib.metadata
import platform
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

# Match standard placeholder patterns:
#   * XML-like tags:        ``<tag>``
#   * Format items:         ``{0}``
#   * String interpolation: ``${var}``
#   * Bracket tags:         ``[tag]`` or ``[tag=value]``
#   * Existing tokens:      ``[[TOKEN_n]]``
#   * Strict markers:       ``⟦Tn⟧``
#   * Percent sign:         ``%``
TOKEN_PATTERN = re.compile(
    r"<[^>]+>|\{[^{}]+\}|\$\{[^{}]+\}|\[(?:/?[a-zA-Z]+(?:=[^\]]+)?)\]|\[\[TOKEN_\d+\]\]|⟦T\d+⟧|%"
)
TOKEN_RE = re.compile(r'\[\[TOKEN_(\d+)\]\]')
TOKEN_OR_SENTINEL_RE = re.compile(r'\[\[TOKEN_(?:\d+|SENTINEL)\]\]', re.I)
STRICT_TOKEN_RE = re.compile(r'⟦T(\d+)⟧')
STRICT_TOKEN_CLEAN = re.compile(r'⟦\s*T\s*(\d+)\s*⟧', re.I)
LOOSE_STRICT_TOKEN_RE = re.compile(r'⟦([^⟧]+)⟧')
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
    """Replace tokens with deterministic ``[[TOKEN_n]]`` sentinels."""

    text = text.replace("\\u003C", "<").replace("\\u003E", ">")
    tokens: List[str] = []
    result: List[str] = []
    i = 0

    while i < len(text):
        # Handle interpolation blocks like ``{(a(b))}`` which may contain
        # nested parentheses. These should be treated atomically so the
        # translator never touches the contents.
        if text.startswith("{", i):
            j = i + 1
            while j < len(text) and text[j].isspace():
                j += 1
            if j < len(text) and text[j] == "(":
                start = i
                j += 1
                depth = 1
                while j < len(text) and depth:
                    ch = text[j]
                    if ch == "(":
                        depth += 1
                    elif ch == ")":
                        depth -= 1
                    j += 1
                while j < len(text) and text[j].isspace():
                    j += 1
                if depth == 0 and j < len(text) and text[j] == "}":
                    block = text[start : j + 1]
                    tokens.append(block)
                    result.append(f"[[TOKEN_{len(tokens)-1}]]")
                    i = j + 1
                    continue

        m = TOKEN_PATTERN.match(text, i)
        if m:
            tokens.append(m.group(0))
            result.append(f"[[TOKEN_{len(tokens)-1}]]")
            i = m.end()
            continue

        result.append(text[i])
        i += 1

    return "".join(result), tokens


def unprotect(text: str, tokens: List[str]) -> str:
    """Restore original tokens from ``[[TOKEN_n]]`` markers."""

    def repl(m: re.Match) -> str:
        idx = int(m.group(1))
        if idx >= len(tokens):
            raise ValueError(f"Unknown token index: {idx}")
        return tokens[idx]

    return TOKEN_RE.sub(repl, text)


def normalize_tokens(text: str) -> str:
    """Normalize token formatting in Argos output."""

    def to_token(num: str) -> str:
        return f"[[TOKEN_{num}]]"

    # Canonicalise any ``⟦Tn⟧`` placeholder, permitting stray characters.
    text = re.sub(
        r"⟦\s*T([^⟧]+)⟧",
        lambda m: to_token(re.sub(r"\D", "", m.group(1))),
        text,
    )

    # Normalise potential variations of the sentinel token.
    text = re.sub(
        r"\[\[\s*TOKEN\s*_?\s*SENTINEL\s*\]\]|TOKEN\s*_?\s*SENTINEL",
        TOKEN_SENTINEL,
        text,
        flags=re.I,
    )
    text = text.replace(f" {TOKEN_SENTINEL}", "").replace(TOKEN_SENTINEL, "")

    # Merge cases where Argos inserts spaces within token digits.
    text = re.sub(
        r"\[\[\s*TOKEN\s*_\s*((?:\d\s*)+)\]\]",
        lambda m: to_token(re.sub(r"\s+", "", m.group(1))),
        text,
        flags=re.I,
    )

    # Merge adjacent placeholder fragments created from split digits but avoid
    # collapsing legitimately sequential tokens (e.g., ``[[TOKEN_0]][[TOKEN_1]]``).
    def merge_adjacent(m: re.Match) -> str:
        nums = [int(n) for n in re.findall(r"TOKEN_(\d+)", m.group(0))]
        if len(nums) == 2 and nums[1] == 0:
            return to_token(f"{nums[0]}{nums[1]}")
        return m.group(0)

    text = re.sub(r"(?:\[\[TOKEN_\d+\]\]){2,}", merge_adjacent, text)

    # Catch bare ``TOKEN_n`` words and rewrite them to placeholder form.
    text = re.sub(r"TOKEN\s*_?\s*(\d+)", lambda m: to_token(m.group(1)), text, flags=re.I)

    # After tokens are normalized, strip any stray brackets Argos may emit.
    placeholders: List[str] = []

    def store(m: re.Match) -> str:
        placeholders.append(m.group(0))
        return f"@@{len(placeholders)-1}@@"

    def restore(m: re.Match) -> str:
        return placeholders[int(m.group(1))]

    tmp = TOKEN_OR_SENTINEL_RE.sub(store, text)
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


def _write_report(
    path: str, rows: list[dict[str, str]], *, max_retries: int = 3
) -> tuple[int, Counter[str]]:
    """Write translation ``rows`` to ``path`` as JSON or CSV.

    Returns a tuple of ``(row_count, category_counts)`` where ``row_count`` is the
    number of deduplicated rows written and ``category_counts`` is a ``Counter``
    mapping categories to their respective counts.
    """
    report_dir = os.path.dirname(path)
    if report_dir:
        os.makedirs(report_dir, exist_ok=True)

    before_count = len(rows)
    # Deduplicate by hash so reruns don't accumulate stale entries
    if rows:
        deduped: dict[str, dict[str, str]] = {}
        for row in rows:
            key = row.get("hash")
            if key:
                deduped[key] = row
        rows = list(deduped.values())
    after_count = len(rows)
    logger.info("Received %d rows; deduplicated to %d rows", before_count, after_count)

    counts: Counter[str] = Counter()
    if rows:
        for row in rows:
            counts[row.get("category") or "unknown"] += 1

    ext = os.path.splitext(path)[1].lower()
    for attempt in range(1, max_retries + 1):
        try:
            with open(path, "w", encoding="utf-8", newline="") as fp:
                if ext == ".json":
                    out_rows = list(rows)
                    if counts:
                        out_rows.append({"summary": dict(counts)})
                    json.dump(out_rows, fp, indent=2, ensure_ascii=False)
                elif ext == ".csv":
                    writer = csv.DictWriter(
                        fp, fieldnames=["hash", "english", "reason", "category"]
                    )
                    writer.writeheader()
                    writer.writerows(rows)
                    if counts:
                        writer.writerow(
                            {
                                "hash": "",
                                "english": "",
                                "reason": json.dumps(dict(counts), ensure_ascii=False),
                                "category": "summary",
                            }
                        )
                else:
                    raise RuntimeError("Report file must end with .json or .csv")
            break
        except Exception as exc:
            logger.warning(
                "Failed to write report to %s (attempt %d/%d): %s",
                path,
                attempt,
                max_retries,
                exc,
            )
            if attempt == max_retries:
                raise
            time.sleep(0.1)
    return after_count, counts


def _append_metrics_entry(args, **extra) -> dict:
    """Append a metrics entry to ``args.metrics_file``."""

    metrics_dir = os.path.dirname(args.metrics_file)
    if metrics_dir:
        os.makedirs(metrics_dir, exist_ok=True)
    entry = {
        "run_id": args.run_id,
        "log_file": args.log_file,
        "report_file": args.report_file,
        "metrics_file": args.metrics_file,
        "git_commit": args.git_commit,
        "python_version": args.python_version,
        "argos_version": args.argos_version,
        "model_version": args.model_version,
        "cli_args": args.cli_args,
        "run_dir": args.run_dir,
        "file": args.target_file,
        "timestamp": time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime()),
        **extra,
    }
    log = _read_json(args.metrics_file, default=[])
    if not isinstance(log, list):
        log = []
    log.append(entry)
    _write_json(args.metrics_file, log)
    args.metrics_recorded = True

    try:
        run_index_file = getattr(args, "run_index_file", None)
        if run_index_file and not getattr(args, "run_index_recorded", False):
            index_dir = os.path.dirname(run_index_file)
            if index_dir:
                os.makedirs(index_dir, exist_ok=True)
            processed = extra.get("processed", 0)
            successes = extra.get("successes", 0)
            success_rate = successes / processed if processed else 0
            index_entry = {
                "run_id": args.run_id,
                "timestamp": entry["timestamp"],
                "language": args.dst,
                "run_dir": args.run_dir,
                "success_rate": success_rate,
            }
            index_log = _read_json(run_index_file, default=[])
            if not isinstance(index_log, list):
                index_log = []
            index_log.append(index_entry)
            _write_json(run_index_file, index_log)
            args.run_index_recorded = True
    except Exception as e:
        logger.warning(f"Failed to update run index: {e}")

    return entry


def write_failure_metrics(args, error) -> None:
    """Record metrics for a failed run if none were written."""

    if getattr(args, "metrics_recorded", False):
        return
    _append_metrics_entry(
        args,
        processed=0,
        successes=0,
        timeouts=0,
        token_reorders=0,
        failures={},
        hash_stats={},
        error=str(error) or error.__class__.__name__,
    )


def _run_translation(args, root: str) -> None:
    try:
        translator = argos_translate.get_translation_from_codes(args.src, args.dst)
    except AttributeError:
        translator = None
    except Exception as e:
        raise SystemExit(f"Failed to initialize Argos translation engine: {e}")

    if translator is None:
        try:
            ensure_model_installed(root, args.dst)
            argos_translate.load_installed_languages()
            translator = argos_translate.get_translation_from_codes(
                args.src, args.dst
            )
        except AttributeError:
            translator = None
        except Exception as e:
            raise SystemExit(f"Failed to initialize Argos translation engine: {e}")

    if translator is None:
        package = f"translate-{args.src}_{args.dst}"
        msg = (
            f"No Argos translation model for {args.src}->{args.dst}. "
            f"Install it with `argospm install {package}`. If split segments are\n"
            f"present, rebuild and install from `Resources/Localization/Models/{args.dst}`:\n"
            f"cd Resources/Localization/Models/{args.dst}\n"
            "cat translate-*.z[0-9][0-9] translate-*.zip > model.zip\n"
            "unzip -o model.zip\n"
            "unzip -p translate-*.argosmodel */metadata.json | jq '.from_code, .to_code'\n"
            "argos-translate install translate-*.argosmodel"
        )
        logger.error(msg)
        raise SystemExit(msg)

    logger.info(
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
        logger.info("No messages need translation.")
        return

    safe_lines: List[str] = []
    tokens_list: List[tuple[List[str], bool]] = []
    keys: List[str] = []

    for key, text in to_translate:
        safe, tokens = protect_strict(text)
        token_only = TOKEN_RE.sub("", safe).strip() == ""
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


    batches: list[list[int]] = []
    i = 0
    while i < len(safe_lines):
        tokens, _ = tokens_list[i]
        if tokens:
            batches.append([i])
            i += 1
            continue
        chunk: list[int] = []
        j = i
        while (
            j < len(safe_lines)
            and len(chunk) < args.batch_size
            and not tokens_list[j][0]
        ):
            chunk.append(j)
            j += 1
        batches.append(chunk)
        i = j

    num_batches = len(batches)
    start = time.perf_counter()
    try:
        for batch_idx, batch in enumerate(batches):
            batch_lines = [safe_lines[idx] for idx in batch]
            batch_keys = [keys[idx] for idx in batch]
            batch_tokens = [tokens_list[idx] for idx in batch]
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

        def strict_retry(key: str) -> tuple[bool, str, int, bool, str | None]:
            nonlocal token_reorders
            safe, tokens = protect_strict(english[key])
            token_only = TOKEN_RE.sub("", safe).strip() == ""
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
        logger.info(summary_msg)
        category_counts = Counter(entry["category"] for entry in report.values())
        if category_counts:
            breakdown_msg = "Report breakdown: " + ", ".join(
                f"{k}: {v}" for k, v in sorted(category_counts.items())
            )
        else:
            breakdown_msg = "Report breakdown: none"
        logger.info(breakdown_msg)

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

        logger.info(f"Wrote translations to {target_path}")

        _append_metrics_entry(
            args,
            processed=processed_lines,
            successes=successes,
            timeouts=timeouts_count,
            token_reorders=token_reorders,
            failures={k: v[0] for k, v in failures.items()},
            hash_stats=token_stats,
        )

        summary_line = (
            f"Summary: {successes}/{processed_lines} translated, {timeouts_count} timeouts, "
            f"{token_reorders} token reorders. Metrics written to {args.metrics_file}"
        )
        logger.info(summary_line)
    finally:
        pass

    skipped = len(report)
    failures_count = len(failures)
    return list(report.values()), processed_lines, successes, skipped, failures_count


def _run_dry_run(args, root: str) -> tuple[list[dict[str, str]], int, int, int, int]:
    """Check existing translations without invoking Argos."""

    english_path = os.path.join(
        root, "Resources", "Localization", "Messages", "English.json"
    )
    target_path = os.path.join(root, args.target_file)

    english = _read_json(english_path)["Messages"]
    target = _read_json(target_path, default={"Messages": {}})
    messages = target.get("Messages", {})

    processed_lines = len(english)
    failures: dict[str, tuple[str, str]] = {}
    report: dict[str, dict[str, str]] = {}
    token_reorders = 0
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
    ) -> None:
        status = "SKIPPED" if reason else "CHECKED"
        msg = f"{key}: {status}"
        if reason:
            msg += f" ({reason})"
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
            logger.info(f"{msg}\n  Original: {original}\n  Result: {result}")

    for key, original in english.items():
        safe, tokens = protect_strict(original)
        token_only = TOKEN_RE.sub("", safe).strip() == ""
        translated = messages.get(key)
        if translated is None:
            reason = "untranslated"
            category = categorize(reason)
            log_entry(key, original, "", reason, category=category)
            failures[key] = (reason, category)
            token_stats[key] = {
                "original_tokens": len(tokens),
                "translated_tokens": 0,
                "reordered": False,
            }
            continue

        protected, found_tokens_list = protect_strict(translated)
        protected = normalize_tokens(protected)
        reorder_tokens(protected, len(tokens))

        token_stats[key] = {
            "original_tokens": len(tokens),
            "translated_tokens": len(found_tokens_list),
            "reordered": False,
        }

        reason: str | None = None
        category: str | None = None

        if Counter(found_tokens_list) != Counter(tokens):
            missing = [t for t in tokens if t not in found_tokens_list]
            extra = [t for t in found_tokens_list if t not in tokens]
            parts: list[str] = []
            if missing:
                parts.append(f"missing {missing}")
            if extra:
                parts.append(f"unexpected {extra}")
            reason = "token mismatch (" + ", ".join(parts) + ")"
            category = categorize(reason)
        elif found_tokens_list != tokens:
            reason = "tokens reordered"
            category = categorize(reason)
            token_stats[key]["reordered"] = True
            token_reorders += 1

        stripped = TOKEN_PATTERN.sub("", translated)
        if reason is None:
            if token_only:
                if stripped.strip():
                    reason = "contains English"
            else:
                if stripped.strip() == "":
                    reason = "placeholders only"
                elif "[" in stripped or "]" in stripped:
                    reason = "stray brackets"
                elif translated == original:
                    reason = "identical to source"
                elif contains_english(translated):
                    reason = "contains English"
            if reason:
                category = categorize(reason)

        if reason:
            failures[key] = (reason, category)
            log_entry(key, original, translated, reason, category=category)
        else:
            log_entry(key, original, translated)

    successes = processed_lines - len(failures)

    _append_metrics_entry(
        args,
        processed=processed_lines,
        successes=successes,
        timeouts=0,
        token_reorders=token_reorders,
        failures={k: v[0] for k, v in failures.items()},
        hash_stats=token_stats,
        dry_run=True,
    )

    summary_line = (
        f"Summary: {successes}/{processed_lines} checked, 0 timeouts, {token_reorders} token reorders. "
        f"Metrics written to {args.metrics_file}"
    )
    logger.info(summary_line)

    skipped = len(report)
    failures_count = len(failures)
    return list(report.values()), processed_lines, successes, skipped, failures_count


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
    ap.add_argument(
        "--run-dir",
        help=(
            "Directory to store logs, reports, and metrics. "
            "Defaults to translations/<lang>/<timestamp>/ under --root"
        ),
    )
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
        "--log-level",
        default="WARNING",
        choices=["DEBUG", "INFO", "WARNING", "ERROR"],
        help="Set logging level",
    )
    ap.add_argument(
        "--verbose",
        action="store_true",
        help=argparse.SUPPRESS,
    )
    ap.add_argument(
        "--log-file",
        help=(
            "Write log output to this file (default: translate.log in run directory); "
            "missing directories are created"
        ),
    )
    ap.add_argument(
        "--report-file",
        help=(
            "Write skipped hashes and reasons to this JSON or CSV file "
            "(default: skipped.csv in run directory); missing directories are created"
        ),
    )
    ap.add_argument(
        "--metrics-file",
        help=(
            "Append translation metrics to this JSON file "
            "(default: translate_metrics.json in run directory)"
        ),
    )
    ap.add_argument(
        "--dry-run",
        action="store_true",
        help="Run token checks without calling Argos; no translations are written",
    )
    args = ap.parse_args()

    root = os.path.abspath(args.root)

    timestamp = time.strftime("%Y%m%d-%H%M%S")
    provided_run_dir = bool(args.run_dir)
    if provided_run_dir:
        run_dir = (
            args.run_dir
            if os.path.isabs(args.run_dir)
            else os.path.join(root, args.run_dir)
        )
    else:
        run_dir = os.path.join(root, "translations", args.dst, timestamp)
    os.makedirs(run_dir, exist_ok=True)
    args.run_dir = run_dir

    def resolve_path(path: str | None, default_name: str) -> str:
        if provided_run_dir:
            name = os.path.basename(path) if path else default_name
            return os.path.join(run_dir, name)
        if path:
            return path if os.path.isabs(path) else os.path.join(root, path)
        return os.path.join(run_dir, default_name)

    args.log_file = resolve_path(args.log_file, "translate.log")
    args.report_file = resolve_path(args.report_file, "skipped.csv")
    args.metrics_file = resolve_path(args.metrics_file, "translate_metrics.json")
    args.run_index_file = os.path.join(root, "translations", "run_index.json")
    cli_args = dict(vars(args))

    args.run_id = str(uuid.uuid4())
    try:
        args.git_commit = (
            subprocess.check_output(
                ["git", "rev-parse", "--short", "HEAD"], cwd=root
            )
            .decode()
            .strip()
        )
    except Exception:
        args.git_commit = "unknown"
    try:
        args.argos_version = importlib.metadata.version("argostranslate")
    except Exception:
        args.argos_version = "unknown"
    args.python_version = platform.python_version()
    try:
        from argostranslate import package as argos_package

        packages = argos_package.get_installed_packages()
        args.model_version = "unknown"
        for pkg in packages:
            meta = getattr(pkg, "metadata", {})
            if (
                meta.get("from_code") == args.src
                and meta.get("to_code") == args.dst
            ):
                args.model_version = str(
                    meta.get("package_version") or meta.get("version") or "unknown"
                )
                break
    except Exception:
        args.model_version = "unknown"

    if args.model_version == "unknown":
        try:
            output = subprocess.check_output(["argospm", "list"], stderr=subprocess.DEVNULL)
            for line in output.decode().splitlines():
                parts = line.split()
                if len(parts) >= 4 and parts[1] == args.src and parts[2] == args.dst:
                    args.model_version = parts[-1]
                    break
        except Exception:
            pass

    args.cli_args = cli_args
    args.metrics_recorded = False
    args.run_index_recorded = False

    level_name = args.log_level
    if getattr(args, "verbose", False):
        level_name = "INFO"

    logger.setLevel(getattr(logging, level_name.upper()))
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

    if getattr(args, "verbose", False):
        logger.info("--verbose is deprecated; use --log-level INFO")

    logger.info(
        "target_file=%s src=%s dst=%s batch_size=%d timeout=%d max_retries=%d run_dir=%s git_commit=%s model_version=%s",
        args.target_file,
        args.src,
        args.dst,
        args.batch_size,
        args.timeout,
        args.max_retries,
        args.run_dir,
        args.git_commit,
        args.model_version,
    )

    remaining: list[dict[str, str]] = []
    processed_total = translated_total = skipped_total = failures_total = 0
    exit_code = 0
    exit_msg: str | None = None
    try:
        if args.dry_run:
            (
                remaining,
                processed_total,
                translated_total,
                skipped_total,
                failures_total,
            ) = _run_dry_run(args, root)
        else:
            prev_hashes: set[str] | None = None
            while True:
                try:
                    (
                        remaining,
                        processed,
                        translated,
                        skipped_total,
                        failures_total,
                    ) = _run_translation(args, root)
                    processed_total += processed
                    translated_total += translated
                except BaseException as exc:
                    write_failure_metrics(args, exc)
                    error_msg = str(exc) or exc.__class__.__name__
                    summary_line = (
                        "Summary: 0/0 translated, 0 timeouts, 0 token reorders. "
                        f"Failed: {error_msg}. Metrics written to {args.metrics_file}"
                    )
                    logger.error(summary_line)
                    exit_code = 1
                    exit_msg = error_msg
                    break
                if exit_code:
                    break
                if not args.report_file:
                    break
                hashes = [row["hash"] for row in remaining]
                if not hashes or set(hashes) == prev_hashes:
                    break
                prev_hashes = set(hashes)
                logger.info(
                    f"Retrying {len(hashes)} hash(es) from {args.report_file}"
                )
                args.hashes = hashes
            if not exit_code and remaining:
                logger.warning("Unresolved hashes after retries:")
                for row in remaining:
                    logger.warning(f"{row['hash']}: {row.get('reason', '')}")
    except SystemExit as e:
        exit_code = e.code if isinstance(e.code, int) else 1
        if e.code not in (0, None):
            msg = str(e)
            if not msg or msg == str(e.code):
                msg = f"Exited with code {e.code}"
            logger.error(msg)
            exit_msg = msg
            if not getattr(args, "metrics_recorded", False):
                _append_metrics_entry(
                    args,
                    processed=0,
                    successes=0,
                    timeouts=0,
                    token_reorders=0,
                    failures={},
                    hash_stats={},
                    error=msg,
                )
    except Exception as e:
        msg = f"Unhandled exception: {e}"
        logger.exception(msg)
        if not getattr(args, "metrics_recorded", False):
            _append_metrics_entry(
                args,
                processed=0,
                successes=0,
                timeouts=0,
                token_reorders=0,
                failures={},
                hash_stats={},
                error=msg,
            )
        exit_code = 1
    finally:
        if args.report_file:
            try:
                written, counts = _write_report(args.report_file, remaining)
                logger.info(
                    "Wrote skip report to %s with %d row(s)",
                    args.report_file,
                    written,
                )
                if counts:
                    logger.info("Category counts: %s", dict(counts))
            except Exception as e:
                logger.warning(f"Failed to write skip report: {e}")

        logger.info(
            "Totals: processed=%d translated=%d skipped=%d failures=%d",
            processed_total,
            translated_total,
            skipped_total,
            failures_total,
        )
        if skipped_total or failures_total:
            exit_code = exit_code or 1

        for handler in logger.handlers:
            handler.flush()
            handler.close()
        logger.handlers.clear()

    if exit_code:
        raise SystemExit(exit_msg or exit_code)


if __name__ == "__main__":
    main()
