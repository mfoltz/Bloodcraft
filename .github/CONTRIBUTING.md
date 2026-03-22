# Contributing Notes

## Release process

- Treat the version in `Bloodcraft.csproj`, `thunderstore.toml`, and the current `CHANGELOG.md` entry as the canonical repo-owned version, and keep it as plain `X.Y.Z` only; any `-pre` or `-ft.*` suffixes are derived by CI and are not committed to those files.
- Pushes to `main` publish the shared prerelease tag `v<canonical>-pre` only when no existing GitHub Release already covers that canonical version.
- Pushes to `codex/feature-testing` publish disposable snapshot tags `v<canonical>-ft.<runNumber>`.
- `pull_request` runs targeting `main` are validation-only; they verify the canonical version and build, but do not publish.
- Branch-derived prerelease and feature-testing versions are workflow outputs only and must never be committed back into repo metadata files.

## Review/testing note

- When using the repo-managed environment for local workflow checks, run `bash .codex/install.sh` first so the expected tooling is available before running YAML parsing checks.
- Bash syntax checks such as `bash -n` are not applicable to `.github/workflows/*.yml` files because GitHub Actions workflows are YAML, not shell scripts.
- If shell validation is needed, run `bash .codex/shellcheck.sh` or `bash -n` only against actual `.sh` files or extracted shell snippets, not the workflow YAML itself.
- Local YAML validation warnings against `.github/workflows/release.yml` should be interpreted carefully.
- Direct parser check used during review/testing:

  ```bash
python3 - <<'PY'
import yaml
PY
  ```

  This check depends on PyYAML being installed. If `import yaml` raises `ModuleNotFoundError: No module named 'yaml'`, treat that as a missing parser dependency in the local environment, not as evidence that `.github/workflows/release.yml` is invalid YAML.
- Guarded variant that separates dependency availability from YAML content validation:

  ```bash
if ! python3 -c 'import yaml' >/dev/null 2>&1; then
  echo "Warning: PyYAML is not installed; skipping YAML parsing check."
else
  python3 - <<'PY'
import pathlib
import yaml

workflowPath = pathlib.Path('.github/workflows/release.yml')
with workflowPath.open('r', encoding='utf-8') as workflowFile:
    yaml.safe_load(workflowFile)
print(f'Parsed {workflowPath} successfully.')
PY
fi
  ```

  A missing parser dependency is a tooling/setup warning that should be reported separately from workflow syntax or YAML content defects.
