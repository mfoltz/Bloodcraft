# Hashed Localization Tasks

These tasks implement the requirements outlined in `hashed-localization-prd.md`.

:::task{title="Add hashed-message localization support", owner="@dev", due="2025-08-03"}
1. Create `Resources/Localization/Messages` with `English.json` and placeholders for other languages.
2. Embed these JSON files in `Bloodcraft.csproj`.
3. Update `LocalizationService` around lines 244-284 to load translations keyed by a deterministic hash and add a `Reply(ctx, string, params object[])` method.
:::

:::task{title="Add PluginLanguage configuration", owner="@dev", due="2025-08-03"}
1. Add a `PluginLanguage` entry alongside the existing `LanguageLocalization` config.
2. Provide `ConfigService.PluginLanguage` (lazy-loaded).
3. Use this value when loading dictionaries in `LocalizationService`.
4. Document the new option in the README.
:::

:::task{title="Automated translation file generator", owner="@dev", due="2025-08-05"}
1. Create `Tools/GenerateMessageTranslations.cs`.
2. Scan for calls to `LocalizationService.HandleReply` or `ctx.Reply`.
3. Compute hashes for all discovered strings and update `Resources/Localization/Messages/English.json`.
4. Ensure placeholder entries are produced for other languages.
:::

:::task{title="Begin migrating commands to LocalizationService.Reply", owner="@dev", due="2025-08-05"}
1. Refactor commands such as `Commands/WeaponCommands.cs` to call `LocalizationService.Reply` with the original English text.
2. Confirm that missing translations fall back to English.
:::

:::task{title="Translation completeness checker", owner="@dev", due="2025-08-05"}
1. Implement `Tools/CheckTranslations.cs` that loads all language files and reports hashes present in English but missing elsewhere.
2. Mention this tool in the README under the Localization section.
:::

