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

:::task{title="Translate Spanish", owner="@dev", due="2025-08-12", status="blocked"}
Argos Translate language data missing; cannot run translation command.
Run `python Tools/translate.py Resources/Localization/Messages/Spanish.json --to es`
:::

- [ ] Cleanup Spanish tokens
  Replace `[TOKEN_n]` markers with the correct translations and verify using `check-translations`.
  (Owner: @dev, Due: 2025-08-12)
:::task{title="Cleanup Spanish tokens", owner="@dev", due="2025-08-12", status="open"}
Run `dotnet run --project Bloodcraft.csproj -p:RunGenerateREADME=false -- check-translations`
:::

- [ ] Translate Brazilian Portuguese messages
  Populate Brazilian.json with Portuguese, validate using the checker tool.
  (Owner: @dev, Due: 2025-08-13)
:::task{title="Translate Brazilian Portuguese", owner="@dev", due="2025-08-13", status="open"}
Run `python Tools/translate.py Resources/Localization/Messages/Brazilian.json --to pt`
:::

- [ ] Translate French messages
  Fill French.json and verify hashes/structure.
  (Owner: @dev, Due: 2025-08-13)
:::task{title="Translate French", owner="@dev", due="2025-08-13", status="open"}
Run `python Tools/translate.py Resources/Localization/Messages/French.json --to fr`
:::

- [ ] Translate German messages
  Translate German.json and confirm with checker.
  (Owner: @dev, Due: 2025-08-14)
:::task{title="Translate German", owner="@dev", due="2025-08-14", status="open"}
Run `python Tools/translate.py Resources/Localization/Messages/German.json --to de`
:::

- [ ] Translate Hungarian messages
  Translate Hungarian.json and run the checker tool.
  (Owner: @dev, Due: 2025-08-14)
:::task{title="Translate Hungarian", owner="@dev", due="2025-08-14", status="open"}
Run `python Tools/translate.py Resources/Localization/Messages/Hungarian.json --to hu`
:::

- [ ] Translate Italian messages
  Provide Italian text in Italian.json and verify.
  (Owner: @dev, Due: 2025-08-15)
:::task{title="Translate Italian", owner="@dev", due="2025-08-15", status="open"}
Run `python Tools/translate.py Resources/Localization/Messages/Italian.json --to it`
:::

- [ ] Translate Japanese messages
  Update Japanese.json and run the checker.
  (Owner: @dev, Due: 2025-08-15)
:::task{title="Translate Japanese", owner="@dev", due="2025-08-15", status="open"}
Run `python Tools/translate.py Resources/Localization/Messages/Japanese.json --to ja`
:::

- [ ] Translate Korean messages
  Use Koreana.json for Korean, validate hashes/structure.
  (Owner: @dev, Due: 2025-08-16)
:::task{title="Translate Korean", owner="@dev", due="2025-08-16", status="open"}
Run `python Tools/translate.py Resources/Localization/Messages/Koreana.json --to ko`
:::

- [ ] Translate Latin American Spanish messages
  Translate Latam.json fully and check for missing entries.
  (Owner: @dev, Due: 2025-08-16)
:::task{title="Translate Latin American Spanish", owner="@dev", due="2025-08-16", status="open"}
Run `python Tools/translate.py Resources/Localization/Messages/Latam.json --to es`
:::

- [ ] Translate Polish messages
  Fill Polish.json and verify with checker tool.
  (Owner: @dev, Due: 2025-08-17)
:::task{title="Translate Polish", owner="@dev", due="2025-08-17", status="open"}
Run `python Tools/translate.py Resources/Localization/Messages/Polish.json --to pl`
:::

- [ ] Translate Russian messages
  Provide Russian in Russian.json and verify.
  (Owner: @dev, Due: 2025-08-17)
:::task{title="Translate Russian", owner="@dev", due="2025-08-17", status="open"}
Run `python Tools/translate.py Resources/Localization/Messages/Russian.json --to ru`
:::

- [ ] Translate Simplified Chinese messages
  Populate SChinese.json and run checker tool.
  (Owner: @dev, Due: 2025-08-18)
:::task{title="Translate Simplified Chinese", owner="@dev", due="2025-08-18", status="open"}
Run `python Tools/translate.py Resources/Localization/Messages/SChinese.json --to zh`
:::

- [ ] Translate Traditional Chinese messages
  Translate TChinese.json and verify completeness.
  (Owner: @dev, Due: 2025-08-18)
:::task{title="Translate Traditional Chinese", owner="@dev", due="2025-08-18", status="open"}
Run `python Tools/translate.py Resources/Localization/Messages/TChinese.json --to zh`
:::

- [ ] Translate Thai messages
  Provide Thai in Thai.json and run checker.
  (Owner: @dev, Due: 2025-08-19)
:::task{title="Translate Thai", owner="@dev", due="2025-08-19", status="open"}
Run `python Tools/translate.py Resources/Localization/Messages/Thai.json --to th`
:::

- [ ] Translate Turkish messages
  Translate Turkish.json fully and validate.
  (Owner: @dev, Due: 2025-08-19)
:::task{title="Translate Turkish", owner="@dev", due="2025-08-19", status="open"}
Run `python Tools/translate.py Resources/Localization/Messages/Turkish.json --to tr`
:::

- [ ] Translate Ukrainian messages
  Update Ukrainian.json and run checker tool.
  (Owner: @dev, Due: 2025-08-20)
:::task{title="Translate Ukrainian", owner="@dev", due="2025-08-20", status="open"}
Run `python Tools/translate.py Resources/Localization/Messages/Ukrainian.json --to uk`
:::

- [ ] Translate Vietnamese messages
  Fill Vietnamese.json and verify structure/hashes.
  (Owner: @dev, Due: 2025-08-20)
:::task{title="Translate Vietnamese", owner="@dev", due="2025-08-20", status="open"}
Run `python Tools/translate.py Resources/Localization/Messages/Vietnamese.json --to vi`
:::

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
