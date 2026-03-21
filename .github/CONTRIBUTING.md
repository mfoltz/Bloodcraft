# Contributing Notes

## Release process

- Treat the version in `Bloodcraft.csproj`, `thunderstore.toml`, and the current `CHANGELOG.md` entry as the canonical repo-owned version; update them together when preparing a new release.
- Pushes to `main` publish the shared prerelease tag `v<canonical>-pre`.
- Pushes to `codex/feature-testing` publish disposable snapshot tags `v<canonical>-ft.<runNumber>`.
- `pull_request` runs targeting `main` are validation-only; they verify the canonical version and build, but do not publish.
- Branch-derived prerelease and feature-testing versions are workflow outputs only and must never be committed back into repo metadata files.
