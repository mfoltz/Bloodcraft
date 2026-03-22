---
applyTo: ".github/workflows/**/*.yml,.github/workflows/**/*.yaml"
---

When reviewing GitHub Actions workflows in this repository:

- Focus on trigger correctness, path filters, checkout depth and history assumptions, merge-base and diff logic, matrix behavior, and whether failures are diagnosable.
- Prioritize under-checking, skipped checks, incorrect event assumptions, and brittle shell logic over cosmetic suggestions.
- Avoid style-only comments.
- Prefer simple, explicit Bash over clever one-liners when reliability is at stake.
- For new sanity or guard workflows, verify that initial branch pushes, pull_request events, and shallow clones are handled intentionally.
- Suggest minimal fixes rather than broad workflow rewrites unless the current approach is likely to fail.
- Before local workflow or tooling checks in the repo-managed environment, run `bash .codex/install.sh` so the expected tooling is available.
- Do not recommend `bash -n` against `.github/workflows/*.yml`; Bash syntax checking is not applicable to YAML workflow files.
- If shell validation is still needed, limit `bash -n` or `shellcheck` to real `.sh` files or extracted shell snippets.
- Use an appropriate YAML-aware validator for workflow files.
- If a local YAML validator dependency is missing, report it as an environment or tooling limitation rather than a workflow syntax failure.
