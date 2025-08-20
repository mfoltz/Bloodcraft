.RECIPEPREFIX := >

.PHONY: sample-translate

sample-translate:
> python Tools/translate_argos.py Resources/Localization/Messages/Spanish_sample.json --to es --run-dir translations/sample_es --overwrite
> python Tools/validate_translation_run.py --run-dir translations/sample_es

