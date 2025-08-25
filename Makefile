.RECIPEPREFIX := >

.PHONY: fix-tokens

fix-tokens:
> python Tools/fix_tokens.py --reorder Resources/Localization/Messages/*.json

