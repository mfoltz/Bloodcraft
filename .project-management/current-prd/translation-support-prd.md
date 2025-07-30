# Translation Support Completion PRD

## Problem Statement
While hashed-message localization and configuration for plugin language selection have been implemented, only minimal English and Spanish translation files exist and many server replies bypass the localization service. Additional languages are not embedded, and tooling is not used consistently to ensure completeness. The project requires full coverage of translations and migration of raw messages to maintain a multilingual user experience.

## Objectives
- Provide message JSON files for all supported languages and embed them in the project.
- Translate existing English messages, expanding beyond the single Spanish entry.
- Replace remaining `HandleServerReply` usages with hashed lookup replies.
- Decide on the word-replacement feature: complete or remove unused code.
- Regularly use translation tools to maintain file completeness.

## Features & Acceptance Criteria
1. **Complete Message Files**
   - Create `<language>.json` under `Resources/Localization/Messages` for each language supported by the base game (English, French, German, etc.).
   - Embed these files via `Bloodcraft.csproj`.
   - Acceptance: On selecting any supported language via `PluginLanguage`, messages load without file-not-found errors.
2. **Translate Strings**
   - Populate new message files with translations for existing English strings, starting with Spanish.
   - Acceptance: At least one additional language besides English has full translations verified by `Tools/CheckTranslations`.
3. **Migrate Remaining Messages**
   - Refactor systems (e.g., `WeaponSystem`, `QuestSystem`) to use `LocalizationService.Reply` or equivalent hashed replies.
   - Acceptance: Running `Tools/GenerateMessageTranslations` captures all user-facing messages, and no raw English strings remain in server replies.
4. **Word Replacement Decision**
   - Either enable and document the per-word translation feature or remove the dead code.
   - Acceptance: The codebase contains no unused localization logic.
5. **Tooling Usage**
   - Document and regularly run `Tools/GenerateMessageTranslations` and `Tools/CheckTranslations` as part of the development workflow.
   - Acceptance: README includes clear instructions and the tools run without errors in CI or local development.
6. **Token Protection**
   - Use `LocalizationHelpers.Protect` before sending strings to translators and call `LocalizationHelpers.Unprotect` on the returned text.
   - Keep `English.json` unchanged; apply the helpers only when creating other language files so rich-text tags and placeholders remain intact.
   - Acceptance: README and tasks describe this workflow and translated JSON files preserve tokens exactly.

## Timeline Estimate
- Message file creation and embedding: 1 day
- Spanish translation and tooling validation: 2 days
- Migration of remaining messages: 2 days
- Word replacement decision and cleanup: 1 day

## Stakeholders
- Project Maintainers
- Community Translators
