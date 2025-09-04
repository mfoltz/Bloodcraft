#!/usr/bin/env python3
"""Replace placeholder tokens in localization files with English originals."""

import argparse
import json
import logging
import re
import sys
from collections import Counter
import time
from dataclasses import dataclass
from pathlib import Path
from typing import Dict, List, Tuple

from translate_argos import normalize_tokens
from token_patterns import (
    TOKEN_PATTERN,
    TOKEN_PLACEHOLDER,
    TOKEN_RE,
    extract_tokens,
)

logger = logging.getLogger(__name__)


def replace_placeholders(
    value: str, tokens: List[str]
) -> Tuple[str, bool, bool, List[str], List[str], List[int]]:
    value = normalize_tokens(value)
    matches = list(TOKEN_PLACEHOLDER.finditer(value))
    restored_positions: List[int] = []

    parts: List[str] = []
    last = 0
    index_positions: Dict[int, int] = {}
    for m in matches:
        parts.append(value[last : m.start()])
        id_match = TOKEN_RE.match(m.group(0))
        if id_match:
            idx_str = id_match.group(1)
            try:
                idx = int(idx_str)
            except ValueError:
                idx = int(idx_str, 16)
            if idx < len(tokens):
                index_positions[idx] = sum(len(p) for p in parts)
                parts.append(tokens[idx])
                last = m.end()
                continue
        parts.append(m.group(0))
        last = m.end()
    parts.append(value[last:])
    value = "".join(parts)

    missing_indices: List[int] = []
    if matches and len(tokens) > len(matches):
        missing_indices = [i for i in range(len(tokens)) if i not in index_positions]
        missing_indices.sort()
        for idx in missing_indices:
            token = tokens[idx]
            next_candidates = [j for j in index_positions if j > idx]
            if next_candidates:
                next_idx = min(next_candidates, key=lambda j: index_positions[j])
                insert_pos = index_positions[next_idx]
            else:
                prev_candidates = [j for j in index_positions if j < idx]
                if prev_candidates:
                    prev_idx = max(prev_candidates, key=lambda j: j)
                    insert_pos = index_positions[prev_idx] + len(tokens[prev_idx])
                else:
                    insert_pos = 0
            value = value[:insert_pos] + token + value[insert_pos:]
            restored_positions.append(idx)
            for j in index_positions:
                if index_positions[j] >= insert_pos:
                    index_positions[j] += len(token)
            index_positions[idx] = insert_pos

    replaced = bool(matches) or bool(restored_positions)
    value = value.replace('\\"', '"').replace("\\'", "'")
    tokenized = extract_tokens(value)
    expected = Counter(tokens)
    found = Counter(tokenized)
    missing = list((expected - found).elements())
    extra = list((found - expected).elements())
    mismatch = bool(missing or extra)
    return value, replaced, mismatch, missing, extra, restored_positions


def reorder_tokens_in_text(value: str, tokens: List[str]) -> Tuple[str, bool]:
    found = TOKEN_PATTERN.findall(value or "")
    if len(found) != len(tokens):
        return value, False
    if found == tokens:
        return value, False
    token_iter = iter(tokens)
    def repl(_m: re.Match) -> str:
        return next(token_iter)
    return TOKEN_PATTERN.sub(repl, value, count=len(tokens)), True


def load_english_messages(path: Path) -> Dict[str, List[str]]:
    with open(path, "r", encoding="utf-8") as f:
        english = json.load(f)["Messages"]
    return {k: extract_tokens(v) for k, v in english.items()}


def load_english_nodes(path: Path) -> Dict[str, List[str]]:
    if not path.exists():
        return {}
    with open(path, "r", encoding="utf-8") as f:
        data = json.load(f)
    nodes = data.get("nodes") or data.get("Nodes") or []
    mapping: Dict[str, List[str]] = {}
    for node in nodes:
        guid = node.get("guid") or node.get("Guid")
        text = node.get("text") or node.get("Text")
        if guid and isinstance(text, str):
            mapping[guid] = extract_tokens(text)
    return mapping


@dataclass
class Counters:
    tokens_restored: int = 0
    tokens_reordered: int = 0
    token_mismatches: int = 0


def process_messages_file(
    path: Path,
    baseline: Dict[str, List[str]],
    check_only: bool,
    reorder: bool,
    allow_mismatch: bool,
    root: Path,
) -> Tuple[Counters, List[Dict[str, List[str]]]]:
    with open(path, "r", encoding="utf-8") as f:
        data = json.load(f)
    messages = data.get("Messages", {})
    changed = False
    counters = Counters()
    mismatches: List[Dict[str, List[str]]] = []
    for key, text in list(messages.items()):
        new_text, replaced, bad, missing, extra, restored_positions = replace_placeholders(
            text, baseline.get(key, [])
        )
        reordered = False
        if reorder and not bad:
            new_text, reordered = reorder_tokens_in_text(new_text, baseline.get(key, []))
        if bad:
            log = logger.warning if allow_mismatch else logger.error
            log("%s: %s token count mismatch", path, key)
            counters.token_mismatches += 1
            rel = str(path)
            try:
                rel = str(path.relative_to(root))
            except ValueError:
                pass
            mismatches.append({
                "file": rel,
                "key": key,
                "missing": missing,
                "extra": extra,
            })
            if replaced and not check_only:
                messages[key] = new_text
                changed = True
            continue
        if replaced or reordered:
            if replaced and reordered:
                if restored_positions:
                    logger.info(
                        "%s: %s tokens restored at positions %s and reordered",
                        path,
                        key,
                        restored_positions,
                    )
                else:
                    logger.info("%s: %s tokens restored and reordered", path, key)
                counters.tokens_restored += len(restored_positions)
                counters.tokens_reordered += 1
            elif replaced:
                if restored_positions:
                    logger.info(
                        "%s: %s tokens restored at positions %s",
                        path,
                        key,
                        restored_positions,
                    )
                else:
                    logger.info("%s: %s tokens restored", path, key)
                counters.tokens_restored += len(restored_positions)
            else:
                logger.info("%s: %s tokens reordered", path, key)
                counters.tokens_reordered += 1
            if not check_only:
                messages[key] = new_text
            changed = True
    if changed and not check_only:
        with open(path, "w", encoding="utf-8") as f:
            json.dump(data, f, indent=2, ensure_ascii=False)
    return counters, mismatches


def process_root_file(
    path: Path,
    baseline: Dict[str, List[str]],
    check_only: bool,
    reorder: bool,
    allow_mismatch: bool,
    root: Path,
) -> Tuple[Counters, List[Dict[str, List[str]]]]:
    with open(path, "r", encoding="utf-8") as f:
        data = json.load(f)
    nodes = data.get("nodes") or data.get("Nodes") or []
    changed = False
    counters = Counters()
    mismatches: List[Dict[str, List[str]]] = []
    for node in nodes:
        guid = node.get("guid") or node.get("Guid")
        text_key = "text" if "text" in node else "Text" if "Text" in node else None
        if not guid or not text_key:
            continue
        text = node[text_key]
        if not isinstance(text, str):
            continue
        new_text, replaced, bad, missing, extra, restored_positions = replace_placeholders(
            text, baseline.get(guid, [])
        )
        reordered = False
        if reorder and not bad:
            new_text, reordered = reorder_tokens_in_text(new_text, baseline.get(guid, []))
        if bad:
            log = logger.warning if allow_mismatch else logger.error
            log("%s: %s token count mismatch", path, guid)
            counters.token_mismatches += 1
            rel = str(path)
            try:
                rel = str(path.relative_to(root))
            except ValueError:
                pass
            mismatches.append({
                "file": rel,
                "key": guid,
                "missing": missing,
                "extra": extra,
            })
            if replaced and not check_only:
                node[text_key] = new_text
                changed = True
            continue
        if replaced or reordered:
            if replaced and reordered:
                if restored_positions:
                    logger.info(
                        "%s: %s tokens restored at positions %s and reordered",
                        path,
                        guid,
                        restored_positions,
                    )
                else:
                    logger.info("%s: %s tokens restored and reordered", path, guid)
                counters.tokens_restored += len(restored_positions)
                counters.tokens_reordered += 1
            elif replaced:
                if restored_positions:
                    logger.info(
                        "%s: %s tokens restored at positions %s",
                        path,
                        guid,
                        restored_positions,
                    )
                else:
                    logger.info("%s: %s tokens restored", path, guid)
                counters.tokens_restored += len(restored_positions)
            else:
                logger.info("%s: %s tokens reordered", path, guid)
                counters.tokens_reordered += 1
            if not check_only:
                node[text_key] = new_text
            changed = True
    if changed and not check_only:
        with open(path, "w", encoding="utf-8") as f:
            json.dump(data, f, indent=2, ensure_ascii=False)
    return counters, mismatches


def main() -> None:
    ap = argparse.ArgumentParser(description="Fix token placeholders in localization JSON files")
    ap.add_argument("--root", default=Path(__file__).resolve().parents[1], help="Repo root")
    ap.add_argument("--check-only", action="store_true", help="Report issues without modifying files")
    ap.add_argument(
        "--allow-mismatch",
        action="store_true",
        help="Only warn when token counts differ",
    )
    ap.add_argument(
        "--reorder",
        dest="reorder",
        action="store_true",
        help="Reorder tokens when counts match (default)",
    )
    ap.add_argument(
        "--no-reorder",
        dest="reorder",
        action="store_false",
        help="Disable token reordering",
    )
    ap.set_defaults(reorder=True)
    ap.add_argument("--metrics-file", help="Write JSON metrics to this path")
    ap.add_argument(
        "--mismatches-file",
        help="Write token mismatch details to this JSON path",
    )
    ap.add_argument("--baseline-file", default="Resources/Localization/Messages/English.json", help="Baseline English messages file")
    ap.add_argument("--log-level", default="INFO", help="Logging level (default: INFO)")
    ap.add_argument("paths", nargs="*", help="Specific localization JSON files to process")
    args = ap.parse_args()

    logging.basicConfig(
        level=getattr(logging, args.log_level.upper(), logging.INFO),
        format="%(asctime)s %(levelname)s %(message)s",
        stream=sys.stdout,
    )

    root = Path(args.root).resolve()
    baseline_path = Path(args.baseline_file)
    if not baseline_path.is_absolute():
        baseline_path = (root / baseline_path).resolve()
    messages_tokens = load_english_messages(baseline_path)
    node_tokens = load_english_nodes(root / "Resources" / "Localization" / "English.json")

    if args.paths:
        files = []
        for p in args.paths:
            path = Path(p)
            if not path.is_absolute():
                path = (root / path).resolve()
            files.append(path)
    else:
        files = list((root / "Resources" / "Localization" / "Messages").glob("*.json"))
        files = [p for p in files if p.name != "English.json"]
        files += [p for p in (root / "Resources" / "Localization").glob("*.json") if p.name != "English.json"]

    totals = Counters()
    all_mismatches: List[Dict[str, List[str]]] = []
    for path in files:
        if path.parent.name == "Messages":
            result, mismatches = process_messages_file(
                path,
                messages_tokens,
                args.check_only,
                args.reorder,
                args.allow_mismatch,
                root,
            )
        else:
            result, mismatches = process_root_file(
                path,
                node_tokens,
                args.check_only,
                args.reorder,
                args.allow_mismatch,
                root,
            )
        totals.tokens_restored += result.tokens_restored
        totals.tokens_reordered += result.tokens_reordered
        totals.token_mismatches += result.token_mismatches
        all_mismatches.extend(mismatches)

    metrics_path = None
    if args.metrics_file:
        metrics_path = Path(args.metrics_file).resolve()
        metrics_path.parent.mkdir(parents=True, exist_ok=True)
        entry = {
            "timestamp": time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime()),
            "tokens_restored": totals.tokens_restored,
            "tokens_reordered": totals.tokens_reordered,
            "token_mismatches": totals.token_mismatches,
            "mismatches": all_mismatches,
        }
        existing: List[dict] = []
        if metrics_path.exists():
            try:
                existing = json.loads(metrics_path.read_text(encoding="utf-8"))
                if not isinstance(existing, list):
                    existing = [existing]
            except Exception:
                existing = []
        existing.append(entry)
        with open(metrics_path, "w", encoding="utf-8") as f:
            json.dump(existing, f, indent=2)

    if args.mismatches_file:
        mismatches_path = Path(args.mismatches_file).resolve()
        mismatches_path.parent.mkdir(parents=True, exist_ok=True)
        with mismatches_path.open("w", encoding="utf-8") as fp:
            json.dump(all_mismatches, fp, indent=2, ensure_ascii=False)

    if totals.token_mismatches:
        msg = f"{totals.token_mismatches} token mismatches detected"
        if metrics_path:
            msg += f"; metrics written to {metrics_path}"
        if args.allow_mismatch:
            logger.warning(msg)
        else:
            raise SystemExit(msg)

    if args.check_only and (totals.tokens_restored or totals.tokens_reordered):
        msg = "token issues detected"
        if metrics_path:
            msg += f"; metrics written to {metrics_path}"
        raise SystemExit(msg)


if __name__ == "__main__":
    main()
