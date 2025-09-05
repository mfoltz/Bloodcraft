.RECIPEPREFIX := >

.PHONY: fix-tokens check-translations

fix-tokens:
> python Tools/fix_tokens.py --reorder Resources/Localization/Messages/*.json

check-translations:
> python Tools/fix_tokens.py --check-only --reorder Resources/Localization/Messages/*.json
> dotnet run --project Bloodcraft.csproj -p:RunGenerateREADME=false -- check-translations

