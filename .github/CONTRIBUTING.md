# Contributing Notes

## Release process

- Treat the version in `Bloodcraft.csproj`, `thunderstore.toml`, and the current `CHANGELOG.md` entry as the canonical repo-owned version, and keep it as plain `X.Y.Z` only; any `-pre` or `-ft.*` suffixes are derived by CI and are not committed to those files.
- Pushes to `main` publish the shared prerelease tag `v<canonical>-pre` only when no existing GitHub Release already covers that canonical version.
- Pushes to `codex/feature-testing` publish disposable snapshot tags `v<canonical>-ft.<runNumber>`.
- `pull_request` runs targeting `main` are validation-only; they verify the canonical version and build, but do not publish.
- Branch-derived prerelease and feature-testing versions are workflow outputs only and must never be committed back into repo metadata files.

## Review/testing note

- Local YAML validation warnings against `.github/workflows/release.yml` should be interpreted carefully.
- Command used during review/testing:

  ```bash
  python3 - <<'PY'
  import yaml
  PY
  ```

  Warning: `python3` is present, but this command currently fails because `import yaml` raises `ModuleNotFoundError: No module named 'yaml'` due to missing PyYAML. This is an environment/tooling dependency issue, not evidence that `.github/workflows/release.yml` is invalid YAML.
