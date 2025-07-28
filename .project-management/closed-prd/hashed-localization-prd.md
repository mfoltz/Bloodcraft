# Hashed Localization PRD

## Problem Statement
Current message localization requires manually managing keys for every string, which is error-prone and difficult to maintain. The goal is to introduce a deterministic hashing approach to map English text to localized translations and streamline configuration and tooling around it.

## Objectives
- Derive localization keys automatically from message text
- Allow configurable language selection for plugin messages
- Provide tools to generate/update translation files and check for missing entries
- Begin migrating command replies to the new localization API

## Features & Acceptance Criteria
1. **Hashed-message localization**
   - `LocalizationService` computes a hash for each English message and uses JSON dictionaries under `Resources/Localization/Messages/<language>.json`.
   - `Reply(ChatCommandContext, string, params object[])` looks up the hash and falls back to English when missing.
   - Dictionary files are embedded in the project.
   - Acceptance: Replies display translated text when provided, otherwise English.
2. **PluginLanguage configuration**
   - New config entry `PluginLanguage` accessible via `ConfigService.PluginLanguage`.
   - `LocalizationService` loads dictionaries based on this setting.
   - Acceptance: Changing `PluginLanguage` reloads messages in the selected language.
3. **Automated translation file generator**
   - Tool scans project for reply strings, computes hashes, and updates `Messages/English.json` plus placeholders for other languages.
   - Documented usage in README.
   - Acceptance: Running the tool populates or updates translation files without removing existing translations.
4. **Command migration**
   - Representative commands refactored to use `LocalizationService.Reply`.
   - Acceptance: Commands still produce replies correctly with fallback behavior.
5. **Translation completeness checker**
   - Console utility lists missing translations across languages.
   - Acceptance: Running the checker highlights hashes without translations for each language.

## Timeline Estimate
- Implementation and initial migration: ~1 week
- Tooling and README updates: ~2 days

## Stakeholders
- Project Maintainers
- Community Translators

