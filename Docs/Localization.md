# Localization Contributor Guide

Bloodcraft uses hash-based localization. Follow these steps when adding or editing messages.

Before introducing a new message, check the catalog in
`Docs/MessageKeys.md` to avoid creating duplicate entries. Run
`python Tools/generate_message_catalog.py` if you change
`Resources/Localization/Messages/English.json` so the catalog stays
current.

## Replace interpolated strings

Instead of C# string interpolation, use `LocalizationService.Reply` with numbered placeholders:

```csharp
LocalizationService.Reply(ctx, "You're level {0}", level);
```

## Update language JSONs

When you add or change a message, update every JSON file under `Resources/Localization/Messages` to include the same numbered placeholders `{0}`, `{1}`, etc. so translations match the English template.

Propagate hash keys from the English source into each language file by running:

```bash
python Tools/propagate_hashes.py Resources/Localization/Messages/<Language>.json
```

The script preserves existing translations, adds any new English hashes, and removes obsolete ones. See [`Tools/propagate_hashes.py`](../Tools/propagate_hashes.py) for details.

## Automated Translation for Messages

Only the JSON files in `Resources/Localization/Messages` contain user-facing messages. Use Argos Translate to automate translating these files:

1. **Verify model installation**

   Ensure the Argos model for your target language is installed:

   ```bash
   argos-translate --from en --to <iso-code> - < /dev/null
   ```

2. **Run the translator**

   ```bash
   python Tools/translate_argos.py Resources/Localization/Messages/<Language>.json --to <iso-code> --batch-size 100 --max-retries 3 --verbose --log-file translate.log --report-file skipped.csv --overwrite
   ```

   Omitting `--overwrite` translates only missing entries and keeps existing
   translations intact. Use `--overwrite` sparingly, as it retranslates every
   line and can reprocess thousands of entries unnecessarily.

   To refresh specific messages without touching the rest, pass one or more
   `--hash <hash>` options to translate only those hashes.

3. **Handle skipped rows**

   Any hashes listed in `skipped.csv` must be translated manually. Re-run the translator until the file is empty.

   To extract hashes that were skipped due to token mismatches, scan the
   translation log:

   ```bash
   python Tools/collect_skipped_hashes.py --log-file translate.log --csv mismatches.csv
   ```

   Omit `--csv` to print the unique hashes to stdout.

   ### Interpolation blocks

   Entries containing C# interpolation expressions like `{(condition ? "A" : "B")}` are skipped
   automatically and appear in `skipped.csv` with the `interpolation` category. To translate them:

   1. Locate the hash in the target language JSON file.
   2. Translate only the surrounding text, leaving the `{(...)}` block untouched.
   3. Run `python Tools/fix_tokens.py` for that JSON file.
   4. Re-run the translator to verify the hash no longer appears in `skipped.csv`.

   ### Placeholder-only results

    Sometimes the translator may output only `[[TOKEN_n]]` placeholders without any surrounding text. These messages are left in their original English form and appear in `skipped.csv` with the `placeholder` category so they can be reviewed manually.

    ### Manual review script

    Use `Tools/review_skipped.py skipped_Spanish.csv` to list each skipped hash with its current Spanish text. Replace the English strings in `Resources/Localization/Messages/Spanish.json` with manual translations, then run:

    ```bash
    python Tools/fix_tokens.py Resources/Localization/Messages/Spanish.json
    python Tools/translate_argos.py Resources/Localization/Messages/Spanish.json --to es --overwrite
    ```

    Re-run the translator until `skipped_Spanish.csv` is empty and verify the catalog:

    ```bash
    dotnet run --project Bloodcraft.csproj -p:RunGenerateREADME=false -- check-translations
    ```

4. **Fix tokens**

   ```bash
   python Tools/fix_tokens.py --check-only Resources/Localization/Messages/<Language>.json
   python Tools/fix_tokens.py Resources/Localization/Messages/<Language>.json
   ```

   Run the check-only mode first to fail fast if tokens were altered, then apply the fixes.

### Token warnings

Argos may reorder or drop placeholder tokens during translation. The translation log highlights these issues:

```text
2024-02-24T12:00:00Z WARNING 3981447812 tokens reordered (expected ['0', '1'], got ['1', '0'])
```

When placeholders move, the log shows the original and translated strings for the affected hash:

```
Original: "<color=yellow>{0}</color> has {1} apples"
Result:   "<color=yellow>{1}</color> tiene {0} manzanas"
```

A token count mismatch appears when placeholders are dropped entirely:

```
2844729307: token mismatch on strict retry (expected ['0', '1'], got [])
```

#### Validation steps

Use `fix_tokens.py` to detect and repair token problems. Start with a dry run to surface warnings without modifying files:

```
python Tools/fix_tokens.py --check-only Resources/Localization/Messages/Spanish.json
Resources/Localization/Messages/Spanish.json: 3981447812 tokens reordered
```

Apply the fixes and re‑run in check-only mode to confirm a clean result:

```
python Tools/fix_tokens.py Resources/Localization/Messages/Spanish.json
python Tools/fix_tokens.py --check-only Resources/Localization/Messages/Spanish.json
```

Finally, verify the language files contain no token mismatches or leftover English text:

```
dotnet run --project Bloodcraft.csproj -p:RunGenerateREADME=false -- check-translations --show-text
```

The check should report no token mismatches or leftover English text.

## Verify translation hashes

After updating the files, run:

```bash
dotnet run --project Bloodcraft.csproj -p:RunGenerateREADME=false -- check-translations --summary-json summary.json
```

The command should report no hash changes, confirming placeholders are aligned across languages. Use `--summary-json <path>` to capture aggregate counts per language.

## Automated validation

The CI pipeline runs the following checks and fails if any English text remains or tokens are malformed:

```bash
python Tools/fix_tokens.py --check-only Resources/Localization/Messages/*.json
dotnet run --project Bloodcraft.csproj -p:RunGenerateREADME=false -- check-translations
```

## Build-time check

A check runs during the build to ensure `LocalizationService.Reply` does not receive raw string
literals or interpolated strings. Instead, reference a `MessageKeys` constant or other
localization identifier. The script uses a baseline file to track existing violations:

```bash
python Tools/check_localization_reply.py --update-baseline  # refresh baseline after fixing strings
```

If new string literals are introduced, the build will fail until they are removed or added to the
baseline.

## Scan source for untranslated strings

Run `scan_english.py` to highlight English phrases in C# files that are not wrapped in
`LocalizationService` helpers:

```bash
python Tools/scan_english.py
```

You can skip directories or file patterns that should not be scanned:

```bash
python Tools/scan_english.py --whitelist-dir Bloodcraft.Tests --whitelist-pattern "^Generated/"
```

Use this to quickly locate messages that still need localization.

## Debugging & Metrics

### Localization pipeline

Enable verbose logging and capture end-to-end metrics when running the
full pipeline:

```bash
python Tools/localization_pipeline.py --debug
```

Add `--archive-logs` to preserve per-language `translate_<Language>.log`
and `skipped_<Language>.csv` files under `TranslationLogs/<timestamp>`
for later investigation.

This writes `localization_metrics.json` in the repository root. Each step
records start/end timestamps and per-language results. A successful run
looks like:

```json
{
  "steps": {
    "generation": {"start": "2024-02-20T12:00:00Z", "end": "2024-02-20T12:00:01Z"},
    "propagation": {"start": "…", "end": "…"},
    "translation": {"start": "…", "end": "…"},
    "token_fix": {"start": "…", "end": "…"},
    "verification": {"start": "…", "end": "…", "returncode": 0}
  },
  "languages": {
    "Turkish": {
      "translation": {"returncode": 0, "duration": 1.2},
      "token_fix": {"returncode": 0, "duration": 0.3},
      "skipped_hash_count": 0,
      "success": true
    }
  }
}
```

Non‑zero `returncode` values or `success: false` indicate a failed step.

### Automatic translation

`translate_argos.py` appends metrics for each run when `--metrics-file`
is specified (default `translate_metrics.json` under `--root`):

```bash
python Tools/translate_argos.py Resources/Localization/Messages/Turkish.json --to tr --metrics-file translate_metrics.json
```

An entry summarises successes, timeouts, token reorders and per‑hash token statistics:

```json
[
  {
    "file": "Resources/Localization/Messages/Turkish.json",
    "timestamp": "2024-02-20T12:00:02Z",
    "processed": 500,
    "successes": 498,
    "timeouts": 2,
    "token_reorders": 1,
    "failures": {},
    "hash_stats": {
      "1234567890": {
        "original_tokens": 2,
        "translated_tokens": 2,
        "reordered": false
      }
    }
  }
]
```

### Token fixer

Record how many placeholders were restored or reordered by supplying
`--metrics-file` to `fix_tokens.py`:

```bash
python Tools/fix_tokens.py Resources/Localization/Messages/Turkish.json --metrics-file fix_tokens_metrics.json
```

The metrics file reports token changes and mismatches:

```json
{
  "tokens_restored": 4,
  "tokens_reordered": 1,
  "token_mismatches": 0
}
```

### Log analysis

Use `analyze_translation_logs.py` to summarise unresolved issues across
translations:

```bash
python Tools/analyze_translation_logs.py
```

The script reads `translate_metrics.json` along with any `skipped_*.csv`
reports and lists token mismatches or placeholder-only entries. It exits
non-zero when problems remain so CI and contributors can investigate.

## Troubleshooting & Notes

- **Argos model installation**: Some environments lack the `install` subcommand. Combine split archives and install using the Python API:
  ```bash
  cd Resources/Localization/Models/EN_ES
  cat translate-*.z[0-9][0-9] translate-*.zip > model.zip
  unzip -o model.zip
  python - <<'PY'
  import argostranslate.package
  argostranslate.package.install_from_path('translate-en_es-1_0.argosmodel')
  PY
  ```
- **Slow translations**: The `--verbose` option can produce huge output and slow processing. Consider omitting it or lowering `--batch-size`.
- **Missing SDKs**: `dotnet` may be absent on minimal systems; ensure the .NET SDK is installed before running checks.
- **.NET 6 runtime**: The project targets .NET 6; install `dotnet-runtime-6.0` from Microsoft's package feed even if newer SDKs are present.
- **Translator stalls**: After reporting skipped rows the translator may appear idle; lower `--batch-size` or drop `--verbose` to speed it up.
- **Token mismatches**: Entries listed in `skipped.csv` often need manual review and translation.

### Follow-up
- Improve token handling in `translate_argos.py` to reduce `skipped.csv` entries.
- Provide progress indicators or non-verbose defaults for lengthy translation runs.
