# Translation Support Tasks

These tasks implement the requirements from `translation-support-prd.md`.

:::task{title="Create placeholder message files for all languages", owner="@dev", due="2025-08-10", status="done"}
1. Copy `Resources/Localization/Messages/English.json` to new files for each language (Brazilian, French, etc.).
2. Ensure hashes match the English file structure.
3. Add new files as `<EmbeddedResource>` in `Bloodcraft.csproj`.
:::

:::task{title="Verify csproj embedding", owner="@dev", due="2025-08-10", status="done"}
1. Confirm every language file under `Resources/Localization/Messages` is listed in the project file.
2. Build the project to ensure embedded resources load without errors.
:::

:::task{title="Translate Spanish message file", owner="@dev", due="2025-08-12", status="open"}
1. Provide Spanish translations for all entries in `Spanish.json`.
2. Run `Tools/CheckTranslations` to verify completeness.
:::

:::task{title="Translate Brazilian Portuguese messages", owner="@dev", due="2025-08-13", status="open"}
1. Populate `Brazilian.json` with Portuguese translations.
2. Validate with the checker tool.
:::

:::task{title="Translate French messages", owner="@dev", due="2025-08-13", status="open"}
1. Fill `French.json` with French translations and verify hashes.
:::

:::task{title="Translate German messages", owner="@dev", due="2025-08-14", status="open"}
1. Translate `German.json` and confirm with `Tools/CheckTranslations`.
:::

:::task{title="Translate Hungarian messages", owner="@dev", due="2025-08-14", status="open"}
1. Translate `Hungarian.json` and run the checker.
:::

:::task{title="Translate Italian messages", owner="@dev", due="2025-08-15", status="open"}
1. Provide Italian text in `Italian.json` and verify.
:::

:::task{title="Translate Japanese messages", owner="@dev", due="2025-08-15", status="open"}
1. Update `Japanese.json` with translations and run the checker.
:::

:::task{title="Translate Korean messages", owner="@dev", due="2025-08-16", status="open"}
1. Use `Koreana.json` for Korean translations and validate hashes.
:::

:::task{title="Translate Latin American Spanish messages", owner="@dev", due="2025-08-16", status="open"}
1. Translate `Latam.json` fully and check for missing entries.
:::

:::task{title="Translate Polish messages", owner="@dev", due="2025-08-17", status="open"}
1. Fill `Polish.json` with translations and run `Tools/CheckTranslations`.
:::

:::task{title="Translate Russian messages", owner="@dev", due="2025-08-17", status="open"}
1. Provide Russian translations in `Russian.json` and verify.
:::

:::task{title="Translate Simplified Chinese messages", owner="@dev", due="2025-08-18", status="open"}
1. Populate `SChinese.json` and run the checker tool.
:::

:::task{title="Translate Traditional Chinese messages", owner="@dev", due="2025-08-18", status="open"}
1. Translate `TChinese.json` and verify completeness.
:::

:::task{title="Translate Thai messages", owner="@dev", due="2025-08-19", status="open"}
1. Provide Thai translations in `Thai.json` and run the checker.
:::

:::task{title="Translate Turkish messages", owner="@dev", due="2025-08-19", status="open"}
1. Translate `Turkish.json` fully and validate.
:::

:::task{title="Translate Ukrainian messages", owner="@dev", due="2025-08-20", status="open"}
1. Update `Ukrainian.json` with translations and run `Tools/CheckTranslations`.
:::

:::task{title="Translate Vietnamese messages", owner="@dev", due="2025-08-20", status="open"}
1. Fill `Vietnamese.json` with Vietnamese text and verify hashes.
:::

:::task{title="Migrate remaining HandleServerReply calls", owner="@dev", due="2025-08-22", status="open"}
1. Search for `LocalizationService.HandleServerReply` usages and replace with `LocalizationService.Reply`.
2. Run `Tools/GenerateMessageTranslations` and then `Tools/CheckTranslations`.
:::

:::task{title="Finalize word-replacement feature", owner="@dev", due="2025-08-23", status="open"}
1. Decide whether to enable or remove the commented word replacement logic in `LocalizationService.cs`.
2. Update README to reflect the decision.
:::

:::task{title="Document translation workflow in README", owner="@dev", due="2025-08-24", status="done"}
1. Add instructions for `GenerateMessageTranslations` and `CheckTranslations`.
2. Document how to add new translations and reload messages.
:::

