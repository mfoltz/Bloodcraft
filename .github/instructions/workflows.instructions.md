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
