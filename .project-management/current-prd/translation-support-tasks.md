# Translation Support Tasks

These tasks break down the work described in `translation-support-prd.md`.

- [x] Create placeholder message files for all languages  
  Copy English.json to new files for each language (Brazilian, French, etc.), ensure structure/hashes match, and add as <EmbeddedResource> in Bloodcraft.csproj.  
  (Owner: @dev, Due: 2025-08-10)

- [x] Verify csproj embedding  
  Confirm all language files are embedded and build loads resources without errors.  
  (Owner: @dev, Due: 2025-08-10)

- [ ] Translate Spanish message file  
  Provide Spanish translations for all entries in Spanish.json and verify with Tools/CheckTranslations.  
  (Owner: @dev, Due: 2025-08-12)

- [ ] Translate Brazilian Portuguese messages  
  Populate Brazilian.json with Portuguese, validate using the checker tool.  
  (Owner: @dev, Due: 2025-08-13)

- [ ] Translate French messages  
  Fill French.json and verify hashes/structure.  
  (Owner: @dev, Due: 2025-08-13)

- [ ] Translate German messages  
  Translate German.json and confirm with checker.  
  (Owner: @dev, Due: 2025-08-14)

- [ ] Translate Hungarian messages  
  Translate Hungarian.json and run the checker tool.  
  (Owner: @dev, Due: 2025-08-14)

- [ ] Translate Italian messages  
  Provide Italian text in Italian.json and verify.  
  (Owner: @dev, Due: 2025-08-15)

- [ ] Translate Japanese messages  
  Update Japanese.json and run the checker.  
  (Owner: @dev, Due: 2025-08-15)

- [ ] Translate Korean messages  
  Use Koreana.json for Korean, validate hashes/structure.  
  (Owner: @dev, Due: 2025-08-16)

- [ ] Translate Latin American Spanish messages  
  Translate Latam.json fully and check for missing entries.  
  (Owner: @dev, Due: 2025-08-16)

- [ ] Translate Polish messages  
  Fill Polish.json and verify with checker tool.  
  (Owner: @dev, Due: 2025-08-17)

- [ ] Translate Russian messages  
  Provide Russian in Russian.json and verify.  
  (Owner: @dev, Due: 2025-08-17)

- [ ] Translate Simplified Chinese messages  
  Populate SChinese.json and run checker tool.  
  (Owner: @dev, Due: 2025-08-18)

- [ ] Translate Traditional Chinese messages  
  Translate TChinese.json and verify completeness.  
  (Owner: @dev, Due: 2025-08-18)

- [ ] Translate Thai messages  
  Provide Thai in Thai.json and run checker.  
  (Owner: @dev, Due: 2025-08-19)

- [ ] Translate Turkish messages  
  Translate Turkish.json fully and validate.  
  (Owner: @dev, Due: 2025-08-19)

- [ ] Translate Ukrainian messages  
  Update Ukrainian.json and run checker tool.  
  (Owner: @dev, Due: 2025-08-20)

- [ ] Translate Vietnamese messages  
  Fill Vietnamese.json and verify structure/hashes.  
  (Owner: @dev, Due: 2025-08-20)

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
  Implement ProtectTokens/UnprotectTokens helpers and document usage.  
  (Owner: @dev, Due: 2025-08-25)

:::task{title="Add token protection helpers", owner="@dev", due="2025-08-25", status="done"}
1. Implement `LocalizationHelpers.ProtectTokens` and `LocalizationHelpers.UnprotectTokens` with indexed markers and a token map.
2. Extend the README with an example showing how to use them during translation.
:::

