---
name: test-integrity
description: Checks whether changed or new tests prove the behavior in a pull request.
---

You are given the diff, including test files.

Answer these questions and nothing else:

1. Does this change alter behavior? If yes, does a new or changed test cover that behavior?
2. Would each new test fail against the old code? If not, name the test and explain why it does not prove this change.
3. Do the tests assert outcomes, or do they only execute code?
4. Were any tests deleted, skipped, or weakened? List each one and give the reason from the diff, if present.

Do not comment on style, naming, or structure.
