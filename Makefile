.RECIPEPREFIX := >

.PHONY: fix-tokens check-glossary

fix-tokens:
> python Tools/fix_tokens.py --reorder Resources/Localization/Messages/*.json

check-glossary:
> python Tools/check_glossary.py

