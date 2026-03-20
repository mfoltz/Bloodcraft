## What changed
- 

## Why
- 

## Release/versioning note
- If this PR is workflow-only, process-only, docs-only, or otherwise does not change shipped plugin behavior, do **not** describe it as a version bump in the PR title or summary.
- Only describe a version bump when `Bloodcraft.csproj`, `thunderstore.toml`, and `CHANGELOG.md` are updated together for an intentional release-facing change.

## `minor-feature` label guidance
Use the `minor-feature` label for PRs that are intentionally narrow in scope. These PRs should usually:
- Touch only a few focused files.
- Avoid workflow, release, or config churn unless that churn is part of the intended feature.
- Include a short acceptance checklist.
- Avoid unrelated refactors while the feature is in flight.

If the repository adopts label automation later, this guidance can serve as the starting point for a scope-check allowlist.

## Invariants protected
- State what still remains true if this change partially fails or is retried.
- Call out protections against data loss or duplication in relevant flows.
- Examples for this codebase: entitlement and progression data remain preserved, starter-kit or reward grants do not duplicate on retry, and failed sync steps do not corrupt persisted player state.

## Persistence / Grants / Progression Safety Checklist
Complete this section when your change touches persistence, grants, progression, Eclipse sync, familiars, or config-driven state.

Applies especially to changes in areas such as:
- `Commands/MiscCommands.cs` starter-kit and grant flows.
- `Services/EclipseService.cs` registration, config sync, and client progress delivery.
- `Services/DataService.cs` persistence, save/load, and failure handling.
- Progression systems and utilities, including leveling, legacies, expertise, professions, and related progression state.
- Familiar systems and services, including unlocks, leveling, binding, prestige, and config-backed familiar state.

- [ ] **Partial success:** If only part of the operation succeeds, what remains applied and what is rolled back?
- [ ] **Retry safety:** Is it safe to retry after a timeout, restart, reconnect, or manual re-run?
- [ ] **Duplicate triggers:** Can duplicate events, commands, hooks, or sync messages double-apply the change?
- [ ] **Failure preservation:** What player, familiar, grant, or progression state must be preserved if the operation fails midway?
- [ ] **Diagnostics:** Is there enough logging and contextual data to diagnose who failed, where, and why?
- [ ] **Missing config/data:** What happens if config values, saved data, prefab references, unlock containers, or required records are missing?

## Acceptance checks
- List 3-6 concrete scenarios you verified.
- Keep the checklist short and focused for `minor-feature` PRs.
- Prefer specific end-to-end or behavioral checks over generic statements like "tested locally".

## AI usage
- AI usage: None / assisted drafting / assisted implementation.
