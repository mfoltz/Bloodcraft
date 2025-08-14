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

3. **Handle skipped rows**

   Any hashes listed in `skipped.csv` must be translated manually. Re-run the translator until the file is empty.

4. **Fix tokens**

   ```bash
   python Tools/fix_tokens.py --check-only Resources/Localization/Messages/<Language>.json
   python Tools/fix_tokens.py Resources/Localization/Messages/<Language>.json
   ```

   Run the check-only mode first to fail fast if tokens were altered, then apply the fixes.

## Verify translation hashes

After updating the files, run:

```bash
dotnet run --project Bloodcraft.csproj -p:RunGenerateREADME=false -- check-translations
```

The command should report no hash changes, confirming placeholders are aligned across languages.

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

