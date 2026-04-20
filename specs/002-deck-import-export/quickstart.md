# Quickstart: Commander Deck Import And Export

## Goal

Deliver browser-local deck portability for Commander decklists so users can import, persist,
switch, and export multiple decks across sessions without sending imported deck documents to the
server.

## Prerequisites

- .NET 10 SDK installed
- Existing solution at `src/CommandSynergy.slnx`
- Representative text fixtures for Moxfield, ManaBox, and the generic plaintext format

## Implementation Sequence

1. Add import/export contracts and format-profile abstractions in `CommandSynergy.Application`.
2. Implement pure parser and exporter services with fixture-based tests for:
   - successful imports for each supported format
   - ambiguous-format detection and manual override behavior
   - partial-success imports with unresolved lines preserved
   - export warnings when a target format is lossy
   - at least one round-trip path per maintained format
3. Add a browser-local deck library store in `CommandSynergy.Client` that:
   - hydrates from localStorage after interactive render
   - persists multiple imported decks and the active deck id
   - guards against corrupted or oversized stored payloads
4. Extend the deck UI in `CommandSynergy` with:
   - import entry points for pasted text or uploaded text files
   - a persisted deck switcher inside the app
   - explicit empty, loading, partial-success, error, and recovery states
   - export preview and copy/download actions
5. If the feature reuses server-backed validation or analysis, wire it behind an explicit user
   action that creates a transient working copy rather than automatically synchronizing the
   persisted imported-deck library.

## Verification Commands

```powershell
dotnet restore src/CommandSynergy.slnx
dotnet build src/CommandSynergy.slnx
dotnet test src/CommandSynergy.slnx
```

## Validation Notes

1. Confirm importing a supported sample deck succeeds with no new import/export endpoint traffic.
2. Confirm imported decks remain available after a browser refresh and after reopening the app.
3. Confirm switching between two persisted decks updates the UI without a network call.
4. Confirm a partial-success import preserves recognized cards and shows unresolved lines with
   recovery guidance.
5. Confirm export preview warnings appear before copy/download when a target format omits or
   flattens metadata.
6. Confirm malformed or oversized text input is rejected safely and does not execute markup in the
   UI.
7. Confirm corrupted localStorage state falls back to a recoverable empty-library experience.

## Suggested Test Layers

1. `tests/CommandSynergy.Application.Tests/Decks`: parser, format detection, exporter, round-trip,
   and diagnostics fixtures.
2. `tests/CommandSynergy.WebUI.Tests/Components`: import dialog, deck switcher, hydration, and
   recovery-state component tests.
3. `tests/CommandSynergy.WebUI.Tests/Security`: input sanitization, payload size, and safe
   diagnostics rendering tests.

## Ready For Task Generation When

- Supported format profiles and their detection rules are fixed for the first release.
- The browser storage schema and migration approach are documented.
- The UI flow distinguishes clearly between local imported decks and any optional server-backed
  working copy.
- Test fixtures exist for each supported format and for at least one partial-failure case.