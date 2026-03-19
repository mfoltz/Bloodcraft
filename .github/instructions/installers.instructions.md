---
applyTo: ".codex/**/*.sh,**/install*.sh,**/*.bash"
---

When reviewing shell installers and setup scripts in this repository:

- Focus on portability, interpreter version assumptions, quoting, explicit failure handling, and clear user-facing messages.
- Prioritize comments about likely runtime failures over stylistic shell preferences.
- For Python-related setup, check Python 3 assumptions, pip and ensurepip availability, fallback behavior, and whether errors are actionable.
- Avoid suggesting broad rewrites to different tools or shells unless the current code is likely to fail in realistic environments.
- Prefer minimal fixes that preserve the existing script structure.
- Avoid speculative comments about edge cases unless there is a plausible failure path.
