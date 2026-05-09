# Bloodcraft Agent Guide

This file is for Codex and other coding agents working in this repository. Keep changes narrow, evidence-led, and specific to Bloodcraft's V Rising mod runtime.

## Start Here

- Before acting, restate the exact problem, main non-goals, and the condition that should make you stop instead of pushing forward.
- Check the current branch and worktree before editing. Do not rebase, push, merge, delete, retarget, or rewrite branches unless explicitly asked.
- Prefer small, local changes that follow the existing Bloodcraft patterns. Avoid broad rewrites, style-only churn, or generic architecture cleanups.
- Treat logs and runtime failures as evidence to classify, not as an invitation to provide open-ended Discord tech support.

## Build And Verification

- Run the repository bootstrap before any `dotnet` build or test work:

  ```bash
  bash .codex/install.sh
  ```

- On this Windows machine, default `bash` may resolve to WSL without a distro. If that happens, use Git Bash:

  ```powershell
  & 'C:\Program Files\Git\bin\bash.exe' .codex/install.sh
  ```

- Use this verification ladder unless the task is explicitly docs-only:
  - `git diff --check`
  - `.codex/install.sh`
  - `./.codex/run-harness.ps1 -Profile bloodcraft-smoke -Action run` when runtime confidence is needed
- The smoke harness is configured in `.codex/harness.settings.json` and targets `VRisingDedicatedServerCodex`. Treat the harness receipt and server logs as the source of truth for startup classification.
- Keep Codex tooling, probes, harness settings, and agent-only tests under `.codex/`.

## V Rising Modding Constraints

- Preserve BepInEx, Harmony, VampireCommandFramework, VampireReferenceAssemblies, and V Rising lifecycle assumptions unless the task is specifically to change them.
- Be careful around static initialization, world access, IL2CPP registration, and server readiness. Do not move runtime lookups earlier without proving the startup path still works.
- For startup/log issues, identify the first meaningful failure chain and separate Bloodcraft-owned failures from stale installs, dependency mismatches, server-world readiness, or adjacent mod failures.
- Avoid importing patterns from other mods wholesale. Use adjacent repos as evidence or precedent only when the Bloodcraft code and harness support the change.

## Release And Metadata Boundaries

- Follow `.github/CONTRIBUTING.md` for release policy.
- Keep the canonical version plain `X.Y.Z` in `Bloodcraft.csproj`, `thunderstore.toml`, and `CHANGELOG.md`.
- Do not commit branch-derived `-pre` or `-ft.*` versions. Those are CI outputs only.
- Defer final README, changelog, and Thunderstore wording until after build and harness validation when a feature branch is still being stabilized.

## Workflow And Review Guidance

- Workflow-specific review rules live under `.github/instructions/*.instructions.md`; consult them instead of duplicating policy here.
- For GitHub Actions changes, prefer minimal reliability fixes and use YAML-aware validation. Do not run `bash -n` against workflow YAML.
- For shell installer changes, verify through `.codex/install.sh` and limit shell linting to real shell scripts.

## Stop Conditions

- Stop if remote or GitHub state cannot be verified for a branch/merge-train task.
- Stop if the available evidence cannot distinguish Bloodcraft-owned behavior from environment, dependency, or adjacent-mod behavior.
- Stop if verification fails in a way that would require broadening beyond the requested scope.
- Stop before advising public release, Thunderstore publication, or support messaging when the harness or logs do not support the claim.
