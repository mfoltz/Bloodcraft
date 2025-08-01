# Translation Support Tasks

These tasks break down the work described in `translation-support-prd.md`.

* `Tools/translate.py` processes all missing entries in a single request. Hashes that fail validation are listed at the end and require manual translation.

- [x] Create placeholder message files for all languages  
  Copy English.json to new files for each language (Brazilian, French, etc.), ensure structure/hashes match, and add as <EmbeddedResource> in Bloodcraft.csproj.  
  (Owner: @dev, Due: 2025-08-10)

- [x] Verify csproj embedding  
  Confirm all language files are embedded and build loads resources without errors.  
  (Owner: @dev, Due: 2025-08-10)

- [ ] Translate Spanish message file
  Provide Spanish translations for all entries in Spanish.json and verify with Tools/CheckTranslations.
  (Owner: @dev, Due: 2025-08-12)

- [ ] Cleanup Spanish tokens
  Replace `[TOKEN_n]` markers with the correct translations and verify using `check-translations`.
  (Owner: @dev, Due: 2025-08-12)

- [ ] Migrate remaining HandleServerReply calls  
  Replace LocalizationService.HandleServerReply with Reply, re-generate and check translations.  
  (Owner: @dev, Due: 2025-08-22)

- [ ] Finalize word-replacement feature  
  Decide on enabling/removing commented word replacement logic, update README accordingly.  
  (Owner: @dev, Due: 2025-08-23)

- [x] Document translation workflow in README  
  Add instructions for tools and describe how to add/reload translations.  
  (Owner: @dev, Due: 2025-08-24)

- [x] Add token protection helpers  
  Implement Protect/Unprotect helpers and document usage.
  (Owner: @dev, Due: 2025-08-25)

:::task{title="Add token protection helpers", owner="@dev", due="2025-08-25", status="done"}
1. Implement `LocalizationHelpers.Protect` and `LocalizationHelpers.Unprotect` with indexed markers and a token map.
2. Extend the README with an example showing how to use them during translation.
:::
