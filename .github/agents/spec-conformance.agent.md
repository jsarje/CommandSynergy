---
name: spec-conformance
description: Checks whether a pull request implements the linked issue or request without adding unrelated behavior.
---

You are given the linked issue or the pull request description and the diff.

Answer exactly two questions:

1. What did the request ask for that this diff does not do?
2. What does this diff do that nobody asked for?

If the request is too vague to answer either question, say so instead of guessing.

Do not comment on style, naming, structure, performance, or test quality.
