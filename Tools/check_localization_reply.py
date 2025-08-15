#!/usr/bin/env python3
import argparse
import hashlib
import re
import sys
from pathlib import Path

BASELINE_PATH = Path(__file__).with_name('localization_reply.baseline')

CALL_PATTERN = re.compile(r'LocalizationService\.Reply\s*\(', re.MULTILINE)
STRING_PATTERN = re.compile(r'@?\$?"')

def extract_call(text, start):
    idx = start
    depth = 1
    in_string = False
    escape = False
    while idx < len(text):
        ch = text[idx]
        if in_string:
            if escape:
                escape = False
            elif ch == '\\':
                escape = True
            elif ch == '"':
                in_string = False
        else:
            if ch == '"':
                in_string = True
            elif ch == '(':
                depth += 1
            elif ch == ')':
                depth -= 1
                if depth == 0:
                    return text[start:idx]
        idx += 1
    return text[start:]

def find_violations():
    violations = []
    for path in Path('.').rglob('*.cs'):
        if any(part in ('obj', 'bin') for part in path.parts):
            continue
        text = path.read_text(encoding='utf-8')
        for match in CALL_PATTERN.finditer(text):
            args_text = extract_call(text, match.end())
            if STRING_PATTERN.search(args_text):
                call_text = text[match.start():match.end()+len(args_text)+1]
                normalized = re.sub(r'\s+', ' ', call_text.strip())
                violations.append((str(path), normalized))
    return violations

def load_baseline():
    entries = set()
    if BASELINE_PATH.exists():
        for line in BASELINE_PATH.read_text(encoding='utf-8').splitlines():
            line = line.strip()
            if not line:
                continue
            entries.add(line)
    return entries

def save_baseline(violations):
    seen = set()
    with BASELINE_PATH.open('w', encoding='utf-8') as f:
        for path, call in sorted(violations):
            digest = hashlib.sha256(call.encode('utf-8')).hexdigest()
            key = f"{path}|{digest}"
            if key in seen:
                continue
            seen.add(key)
            f.write(key + "\n")
    print(f"Baseline written to {BASELINE_PATH}")

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--update-baseline', action='store_true', help='Rewrite baseline with current violations')
    args = parser.parse_args()

    violations = find_violations()
    if args.update_baseline:
        save_baseline(violations)
        return 0

    baseline = load_baseline()
    new_violations = []
    for path, call in violations:
        digest = hashlib.sha256(call.encode('utf-8')).hexdigest()
        key = f"{path}|{digest}"
        if key not in baseline:
            new_violations.append((path, call))

    if new_violations:
        print('Found LocalizationService.Reply calls with string literals or interpolations:')
        for path, call in new_violations:
            print(f"{path}: {call}")
        return 1
    return 0

if __name__ == '__main__':
    sys.exit(main())
