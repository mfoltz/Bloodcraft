.RECIPEPREFIX := >

.PHONY: sample-translate fix-tokens

sample-translate:
> python Tools/translate_argos.py Resources/Localization/Messages/Spanish_sample.json --to es --run-dir translations/sample_es --overwrite
> python Tools/validate_translation_run.py --run-dir translations/sample_es
> python Tools/analyze_skip_report.py translations/sample_es/skipped.csv

fix-tokens:
> python Tools/fix_tokens.py --reorder Resources/Localization/Messages/*.json

