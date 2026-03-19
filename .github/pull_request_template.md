## What changed
- 

## Why
- 

## Invariants protected
- State what still remains true if this change partially fails or is retried.
- Call out protections against data loss or duplication in relevant flows.
- Examples for this codebase: entitlement and progression data remain preserved, starter-kit or reward grants do not duplicate on retry, and failed sync steps do not corrupt persisted player state.

## Acceptance checks
- List 3-6 concrete scenarios you verified.
- Prefer specific end-to-end or behavioral checks over generic statements like "tested locally".

## AI usage
- AI usage: None / assisted drafting / assisted implementation.
