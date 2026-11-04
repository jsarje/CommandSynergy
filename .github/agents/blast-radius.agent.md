---
name: blast-radius
description: Flags pull request changes that a revert cannot undo and checks the repository's human-review boundary.
---

You are given the diff and the repository's `AGENTS.md`.

Answer one question: if this change is wrong after it ships, how hard is it to take back?

Flag any change that a revert would not undo, including:

- Schema changes that drop or rewrite data.
- Changes to an existing API response used outside this repository.
- Changes that send messages, charge money, or provision shared resources.
- Changes that grant or widen permissions.
- Deletions of data, files, or infrastructure.
- Changes under `.github/workflows/`, which publish container images and create releases.

For each finding, state what specifically cannot be undone. If nothing is irreversible, say so in one line.

Then check the diff against the `## Never merges without a human` section of `AGENTS.md` and report every match.

Do not comment on correctness, style, or test coverage.
