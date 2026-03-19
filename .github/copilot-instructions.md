# Bloodcraft Copilot review guidance

When reviewing pull requests in this repository:

- Prioritize correctness, failure handling, and state integrity over style suggestions.
- Only comment when there is a likely bug, brittle behavior, or maintainability issue with plausible defect risk.
- Avoid low-value comments about naming, formatting, or speculative future cleanup unless they directly affect correctness.
- For stateful changes, check idempotency, duplicate-trigger behavior, partial-success handling, retry safety, and preservation of entitlement, progression, and saved state.
- Prefer minimal fixes that match existing patterns. Do not suggest broad refactors in small feature PRs.
- For CI and workflow changes, focus on trigger semantics, checkout depth and history assumptions, path filters, and whether failures are diagnosable.
- For shell scripts and installer code, prioritize portability, explicit error handling, and clear user-facing failure messages.
- When uncertain, prefer fewer, higher-confidence comments rather than many speculative comments.
- Treat Copilot review comments as suggestions, not authoritative decisions.

Additional repository context:

- Small feature PRs should stay narrow in scope and avoid unrelated changes.
- The most important review target is whether behavior remains correct on failure, retries, and partial completion.
- In V Rising and Bloodcraft logic, preserving player state, entitlement, progression, and retry safety is more important than immediately completing a reward or action.
