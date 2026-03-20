# Contributing

## `minor-feature` label

Use the `minor-feature` label for pull requests that add or adjust a small, contributor-contained feature without widening into a broader release-sized change.

Apply `minor-feature` when:
- the PR introduces a narrowly scoped feature, quality-of-life tweak, or opt-in behavior change;
- the implementation stays focused enough to summarize clearly in the PR scope summary; and
- follow-up work is not required across multiple unrelated systems to make the change coherent.

Expected scope:
- one small feature or one tightly related feature slice;
- limited documentation and tests needed to support that slice; and
- no bundled refactors or unrelated cleanups just to fill out the PR.

Workflow, configuration, and release churn should usually be deliberate rather than incidental. If a feature change needs workflow updates, config surface changes, migration steps, or release-note churn, include them only when they are directly required by the feature and call them out explicitly in the PR scope summary.

Use `minor-feature` as the final label name in contributor docs, reviews, and PR discussions. Do not switch between `minor-feature` and earlier draft terms such as `small-feature`; keeping the terminology consistent helps reviewers match labels, automation, and the PR scope summary.
