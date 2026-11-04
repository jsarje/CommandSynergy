---
name: imported-deck-library
description: Use when changing ImportedDeckLibraryState persistence, active-deck selection, deck replacement, removal, or clearing.
---

# Verify imported deck library state

Run this check when you change `ImportedDeckLibraryState` or a caller that changes its save, selection, removal, or clear behavior.

The library must keep deck IDs unique without regard to case, and its active deck ID must be null or refer to a saved deck. A dangling active ID makes the workspace select a deck that the library does not contain.

## Run the check

From the repository root, run:

```powershell
dotnet test tests\CommandSynergy.WebUI.Tests\CommandSynergy.WebUI.Tests.csproj --filter FullyQualifiedName~ImportedDeckLibraryStateModelTests
```

`ImportedDeckLibraryStateModelTests` explores the real state service, not a copy of its transition logic. It starts with an empty library and explores every reachable state through paths of up to five commands from a fixed set: save either of two decks with or without activation, activate a saved deck or none, remove either deck, and clear the library. It checks both invariants after every command and reports the shortest command sequence that breaks one.

The check covers serial calls to `SaveImportedDeckAsync`, `SetActiveDeckAsync`, `RemoveImportedDeckAsync`, and `ClearAsync`. It uses two fixed deck IDs, only activates a deck already in the library, and stops at five commands. It does not cover browser storage, serialization, concurrent calls, arbitrary deck IDs, or libraries with more than two decks.

## Read a failure

Read the failing assertion and its command sequence, then replay the sequence through `ImportedDeckLibraryState`. If the real state violates an invariant after an allowed command, treat it as a product defect. If the test assumes a command the callers do not allow, fix the command precondition instead of weakening the invariant.

Do not change product code to silence a counterexample unless the intended behavior changed. If the behavior did change, update the invariant and the command set to match the new contract, then rerun the bounded check.

## Keep the check in sync

Coverage record: `src\CommandSynergy.Client\Services\ImportedDeckLibraryState.cs`, methods `SaveImportedDeckAsync`, `SetActiveDeckAsync`, `RemoveImportedDeckAsync`, and `ClearAsync`.

Last confirmed against source commit `b89d3444736181cd46adf9329d9bb449d59e475a`.

When one of the covered methods changes, inspect its new behavior against the test's commands and invariants, then run the check. Move the sync commit forward only after you confirm that the test still describes the code at that commit.
