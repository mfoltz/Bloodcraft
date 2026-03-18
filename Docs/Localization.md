# Localization Contributor Guide

Bloodcraft uses hash-based localization. Follow these steps when adding or editing messages.

## Prerequisites

Install the .NET 8 SDK and the .NET 6 runtime. Verify with `dotnet --list-runtimes` that `Microsoft.NETCore.App 6.0.x` is installed before running any `dotnet run` commands.

Argos tooling is optional and not required for routine localization updates or translation QA. The default contributor/CI path uses hash propagation, token checks, and `check-translations` only.

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

The script preserves existing translations, adds any new English hashes, and removes obsolete ones. Run it before any translation work so each language file stays in sync with English. See [`Tools/propagate_hashes.py`](../Tools/propagate_hashes.py) for details.

## Glossary

A shared glossary lives at `Resources/Localization/glossary.json`. Regenerate it from official localization files when terms change:

```bash
python Tools/generate_glossary.py
```

Run `make check-glossary` during QA to flag translations that diverge from the glossary. Translators should consult this file and expand `glossary_terms.txt` during reviews.

## Translation Workflow for Messages

Only the JSON files in `Resources/Localization/Messages` contain user-facing messages.

### Canonical flow

1. **Refresh the English source.**

   ```bash
   dotnet run --project Bloodcraft.csproj -p:RunGenerateREADME=false -- generate-messages
   ```

2. **Propagate hashes into each language file (required).**

   ```bash
   python Tools/propagate_hashes.py Resources/Localization/Messages/<Language>.json
   ```

   Hash propagation is the structural contract for localization in this repository: every language file must remain hash-aligned with `Resources/Localization/Messages/English.json` before and after translation.

3. **Translate entries with your approved source (manual or assisted).**

   Translation is backend-agnostic. You may translate manually, use Codex-assisted drafting, or use a machine translation backend (including Argos).

4. **Verify placeholder tokens.**

   ```bash
   python Tools/fix_tokens.py --check-only Resources/Localization/Messages/<Language>.json
   ```

   This `--check-only` validation is required after **any** manual, Codex-assisted, or machine-generated translation edits. If mismatches are reported, run `fix_tokens.py` without `--check-only` and then re-verify.

5. **Run translation integrity checks.**

   ```bash
   dotnet run --project Bloodcraft.csproj -p:RunGenerateREADME=false -- check-translations --show-text
   ```

### Approved translation sources

- **Manual human translation** by fluent speakers.
- **Codex-assisted drafts** followed by human review and correction.
- **Machine translation backends** (such as Argos or other providers) followed by review.

Quality expectations apply regardless of source:

- Preserve placeholders/tokens (`<...>`, `{...}`, and `[[TOKEN_n]]`) exactly.
- Keep meaning, tone, and gameplay terminology accurate for the target language.
- Ensure final files pass `fix_tokens.py --check-only` and `check-translations` with no missing hashes or lingering English text.

### Localization QA tooling (required)

Treat `Tools/fix_tokens.py` and `Tools/token_patterns.py` as first-class QA tooling for localization safety.

- `fix_tokens.py --check-only` is a required gate after every translation edit (manual, Codex, or any MT backend).
- `token_patterns.py` defines the canonical placeholder matching rules used by translation tooling and tests.
- Keep and expand tests under `Tools/tests/` that verify placeholder round-tripping and mismatch detection for edited JSON translations.

## Argos Legacy Tooling (Optional / Unsupported by default CI)

Argos scripts remain in the repository as an optional fallback for maintainers who explicitly choose that backend. They are **not** part of the default contributor workflow and are **not** required for translation QA gates in CI.

If you need Argos-specific instructions, use the legacy guide:

- [Docs/Localization-Argos-Legacy.md](Docs/Localization-Argos-Legacy.md)

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

Language names correspond to the message file stems (``German.json`` → ``German``).
The pipeline maps each name to its ISO code (for example, German → ``de``) and
now verifies that translated strings contain words from that language. A run
will fail if messages are produced in another language (e.g. Spanish text in
German files). After each translation run, `fix_tokens.py --reorder` executes
automatically to restore placeholders and align their order with the English
baseline, recording token restoration and reordering metrics per language.

Override the output paths for aggregate metrics and skipped hashes with
`--metrics-file` and `--skipped-file` (defaults are `localization_metrics.json`
and `skipped.csv` in the repository root):

```bash
python Tools/localization_pipeline.py --debug --metrics-file metrics.json --skipped-file skipped.csv
```

Pass `--archive-logs` to archive each run directory under
`TranslationLogs/<commit>/<timestamp>` for later investigation. Commit these
directories to the repository so post-mortem reviews can trace issues back to
the exact code version.

This writes `localization_metrics.json` (or the path provided via
`--metrics-file`). Each step records start/end timestamps and per-language
results. A successful run looks like:

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

### Legacy Argos metrics

Argos-specific translation metrics (`translate_argos.py`, `translations/run_index.json`, and per-run `skipped.csv`) are documented in [Docs/Localization-Argos-Legacy.md](Docs/Localization-Argos-Legacy.md). These artifacts are optional and outside the default CI localization path.

### Token fixer

Record how many placeholders were restored or reordered and capture token
mismatch details by supplying `--metrics-file` to `fix_tokens.py`:

```bash
python Tools/fix_tokens.py Resources/Localization/Messages/Turkish.json --metrics-file fix_tokens_metrics.json
```

Each run appends an entry to `fix_tokens_metrics.json` in the repository root
summarising counts and listing mismatched tokens:

```json
[
  {
    "timestamp": "2024-02-20T12:00:03Z",
    "tokens_restored": 4,
    "tokens_reordered": 1,
    "token_mismatches": 0,
    "mismatches": []
  }
]
```

When mismatches occur the `mismatches` array contains objects with `file`,
`key`, `missing`, and `extra` fields so problematic entries can be traced
quickly.

The localization pipeline runs `summarize_token_stats.py` after tokens are
fixed to highlight hashes with mismatched or reordered tokens. If any language
reports a non-zero `token_mismatches` count in the metrics emitted by
`fix_tokens.py`, the pipeline terminates with a failure status.

### Log analysis

Use `analyze_translation_logs.py` to summarise unresolved issues across
translations:

```bash
python Tools/analyze_translation_logs.py
```

By default, the script reads `metrics.json` and `skipped.csv` from
the repository root. Override these paths to analyse a specific run directory:

```bash
python Tools/analyze_translation_logs.py --run-dir translations/fr/2025-05-16
```

The script lists token mismatches or placeholder-only entries and exits
non-zero when problems remain so CI and contributors can investigate.

For a focused view of hashes with mismatched or reordered tokens recorded in
`metrics.json`, run:

```bash
python Tools/summarize_token_stats.py --run-dir translations/fr/2025-05-16 --top 20
```

This prints a table of problematic hashes; add `--csv output.csv` to write the
results to disk. Translation logs now record token mismatches with short hash
IDs and suggested fixes. To aggregate recurring issues directly from a log
file, provide the log path instead of `--run-dir`:

```bash
python Tools/summarize_token_stats.py translations/de/<timestamp>/translate.log
```

## Troubleshooting & Notes

- **Argos fallback troubleshooting**: See [Docs/Localization-Argos-Legacy.md](Docs/Localization-Argos-Legacy.md).
- **Missing SDKs**: `dotnet` may be absent on minimal systems; ensure the .NET SDK is installed before running checks.
- **.NET 6 runtime**: The project targets .NET 6; install `dotnet-runtime-6.0` from Microsoft's package feed even if newer SDKs are present.
- **Token mismatches**: If `fix_tokens.py --check-only` fails, repair placeholders and rerun verification before committing.
