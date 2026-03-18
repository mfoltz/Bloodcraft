# Localization Argos Legacy Guide (Optional)

> Legacy fallback only. This workflow is **optional** and **unsupported by the default CI localization path**.

Use this guide only if you intentionally choose Argos for machine-assisted translation. Routine localization updates and translation QA do not require Argos models or Argos tooling.

## Legacy scope

- `Tools/ensure_argos_model.py`
- `Tools/translate_argos.py`
- Split model archives under `Resources/Localization/Models/*`

## Preconditions

Install Argos Translate locally and verify it is callable:

```bash
argos-translate --help
```

## Reassemble/install model (session-scoped)

Models are stored as split archives and may need reconstruction before use:

```bash
python Tools/ensure_argos_model.py <iso-code>
```

Manual equivalent:

```bash
cd Resources/Localization/Models/<LANG>
cat translate-*.z[0-9][0-9] translate-*.zip > model.zip
unzip -o model.zip
unzip -p translate-*.argosmodel */metadata.json | jq '.from_code, .to_code'
argos-translate install translate-*.argosmodel
```

Confirm language pair installation:

```bash
argos-translate --from en --to <iso-code> - < /dev/null
```

## Run Argos translation

```bash
python Tools/translate_argos.py Resources/Localization/Messages/<Language>.json --to <iso-code> --batch-size 100 --max-retries 3 --log-level INFO --overwrite
```

Outputs default to `translations/<iso-code>/<timestamp>/`.

## Required QA after Argos runs

Argos users still must pass the standard localization gates:

```bash
python Tools/fix_tokens.py --check-only Resources/Localization/Messages/<Language>.json
dotnet run --project Bloodcraft.csproj -p:RunGenerateREADME=false -- check-translations --show-text
```
