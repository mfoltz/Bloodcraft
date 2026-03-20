# Contributing

## `minor-feature` label

Use the `minor-feature` label for pull requests that add or adjust a small, contributor-contained feature without widening into a broader release-sized change.

The authoritative guidance lives in the `minor-feature` section of `.github/pull_request_template.md`. Use the PR template's **What changed** and **Why** sections to keep the scope easy to review; if this document and the PR template ever diverge, follow the PR template.

Apply `minor-feature` when:
- the PR introduces a narrowly scoped feature, quality-of-life tweak, or opt-in behavior change;
- the implementation stays focused enough to describe clearly in **What changed** and **Why**; and
- follow-up work is not required across multiple unrelated systems to make the change coherent.

Expected scope:
- one small feature or one tightly related feature slice;
- limited documentation and tests needed to support that slice; and
- no bundled refactors or unrelated cleanups just to fill out the PR.

Workflow, configuration, and release churn should usually be deliberate rather than incidental. If a feature change needs workflow updates, config surface changes, migration steps, or release-note churn, include them only when they are directly required by the feature and call them out explicitly in **What changed** or **Why**.

Use `minor-feature` as the final label name in contributor docs, reviews, and PR discussions. Do not switch between `minor-feature` and earlier draft terms such as `small-feature`; keeping the terminology consistent helps reviewers match labels, automation, and the PR template guidance.
