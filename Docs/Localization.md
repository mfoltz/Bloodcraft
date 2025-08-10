# Localization Contributor Guide

Bloodcraft uses hash-based localization. Follow these steps when adding or editing messages.

## Replace interpolated strings

Instead of C# string interpolation, use `LocalizationService.Reply` with numbered placeholders:

```csharp
LocalizationService.Reply(ctx, "You're level {0}", level);
```

## Update language JSONs

When you add or change a message, update every JSON file under `Resources/Localization/Messages` to include the same numbered placeholders `{0}`, `{1}`, etc. so translations match the English template.

## Verify translation hashes

After updating the files, run:

```bash
dotnet run --project Bloodcraft.csproj -p:RunGenerateREADME=false -- check-translations
```

The command should report no hash changes, confirming placeholders are aligned across languages.

