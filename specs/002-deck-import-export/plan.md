# Implementation Plan: Commander Deck Import And Export

**Branch**: `[002-deck-import-export]` | **Date**: 2026-04-19 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/002-deck-import-export/spec.md`

## Summary

Implement text-based deck import and export as a browser-local capability inside the existing
Blazor Web App. Supported deck documents are parsed and rendered entirely on the client, multiple
imported decks are persisted in browser localStorage so users can switch between them across
sessions, and raw imported documents never reach the server. If existing server-backed validation
or analysis is reused, it must operate only on an explicit transient working copy rather than on
the persisted imported-deck library itself.

## Technical Context

**Language/Version**: .NET 10 / C# 14  
**Primary Dependencies**: ASP.NET Core Blazor Web App with Interactive Auto render mode,
MudBlazor, existing CommandSynergy Application contracts, xUnit, bUnit, FluentAssertions, and a
browser-storage abstraction using localStorage (prefer `Blazored.LocalStorage` or equivalent thin
JS interop wrapper)  
**Storage**: Browser localStorage for persisted imported decks, active deck selection, and import
preferences; existing in-memory workspace state remains transient  
**Testing**: xUnit for import/export parsing and rendering services, bUnit for component state and
deck switching, host integration tests for wiring and browser-storage-safe behavior, security tests
for untrusted text handling  
**Target Platform**: ASP.NET Core Blazor Web App on the existing CommandSynergy host with
Interactive Auto client execution in the browser  
**Project Type**: Web application with Clean Architecture class libraries  
**Performance Goals**: Import or export of a standard 100-card Commander deck completes within 3
seconds in at least 95% of acceptance runs; persisted library hydration stays under 500 ms with 10
stored decks on a typical developer machine; switching between already persisted decks completes
without network calls and renders updated state within 250 ms in normal conditions  
**Constraints**: Imported raw deck documents and persisted imported-deck records must remain
client-side only; no automatic POST of imported documents to the server; support multiple imported
decks per user session; persist between sessions without cookies for deck payloads; sanitize all
untrusted text; define loading, empty, partial-success, validation, error, and recovery states;
keep the design compatible with existing deck validation and analysis workflows without making them
mandatory for local import/export  
**Scale/Scope**: Curated support for named text profiles such as Moxfield and ManaBox plus one
generic plaintext format; browser-local library for roughly 10 to 20 Commander decks; one active
local deck selected at a time; three core flows: import, switch, and export  
**Security/Privacy Review**: Treat imported text as untrusted input, sanitize and size-limit it,
prevent markup or script execution in diagnostics and previews, avoid storing secrets or auth data
in browser persistence, keep imported deck payloads off the server by design, avoid logging raw
deck text, and ensure storage hydration failures degrade safely to an empty local library state
with user-visible recovery messaging

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- PASS: Code quality stays within the existing .NET 10/C# 14, analyzer-enabled, nullable-enabled
  project baseline, and import/export logic will live in testable service abstractions instead of
  being embedded directly in Razor components.
- PASS: Automated proof is defined through xUnit fixture-based parser/exporter tests, bUnit UI
  state tests, and host/security regression tests for storage hydration and untrusted text.
- PASS: UX impact is explicit for first-load hydration, empty library, partial-success import,
  unsupported format selection, export warnings, storage failure, and recovery after malformed
  local state.
- PASS: Performance budgets are explicit for import/export, local library hydration, and deck
  switching without network dependency.
- PASS: Security review covers OWASP-relevant input sanitization, local persistence boundaries,
  XSS-safe rendering of diagnostics, logging discipline, and the explicit client-only handling of
  imported deck documents.
- PASS: No constitutional exception is currently required.

## Project Structure

### Documentation (this feature)

```text
specs/002-deck-import-export/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── deck-portability-contract.yaml
└── tasks.md
```

### Source Code (repository root)

```text
src/
├── CommandSynergy/                 # ASP.NET Core Blazor Web App host, pages, deck UI, endpoints
├── CommandSynergy.Client/          # Interactive Auto client services and browser storage adapters
├── CommandSynergy.Application/     # Import/export parsing, format profiles, contracts
├── CommandSynergy.Domain/          # Existing deck/card concepts reused by normalized deck models
└── CommandSynergy.Infrastructure/  # Existing server integrations, unchanged for raw import flow

tests/
├── CommandSynergy.Application.Tests/
├── CommandSynergy.WebUI.Tests/
├── CommandSynergy.Domain.Tests/
└── CommandSynergy.Infrastructure.Tests/
```

**Structure Decision**: Use the existing Blazor Web App host and client project for all user-facing
import/export work, place pure parsing and rendering logic in `CommandSynergy.Application` so it is
testable and reusable from the browser, and add browser-local persistence adapters in
`CommandSynergy.Client`. No new server import/export endpoint is planned because the feature is
explicitly constrained to keep imported decks client-side.

## Phase 0: Research Summary

- Keep import format detection, parsing, diagnostics, and export rendering in pure C# services that
  execute in the browser rather than on server endpoints.
- Persist the imported deck library in localStorage rather than cookies because deck payload sizes
  exceed practical cookie limits and must not be sent on every request.
- Separate the persisted imported-deck library from the existing server-synchronized workspace so
  library operations remain client-only and network-free.
- Support a curated initial format set with deterministic detection rules plus manual override when
  multiple profiles match.
- Preserve unresolved lines, warnings, and source-format metadata locally so users can switch decks
  and re-export without silent data loss.

## Phase 1: Design Summary

- Add a browser-local deck library model that stores multiple imported decks, the active deck
  identifier, import diagnostics, and export warnings under a versioned localStorage document.
- Introduce import/export application services and format-profile abstractions for Moxfield,
  ManaBox, and a generic plaintext format, with partial-success results instead of all-or-nothing
  rejection.
- Extend the deck UI with import, local deck switching, hydration, and export flows that clearly
  separate local-library state from any optional server-backed analysis workflow.
- Add a client storage adapter that hydrates after interactive render and safely handles corrupted
  or oversized stored payloads without breaking the rest of the app.
- Define a deck portability contract for the browser storage schema, import result shape, and
  supported text format guarantees instead of introducing a new HTTP API surface.

## Post-Design Constitution Check

- PASS: The design keeps implementation quality high by centering parsing and persistence in
  isolated services and preserving existing project boundaries.
- PASS: Tests are concrete and layered across parser fixtures, export round-trip behavior, browser
  persistence, and UI recovery states.
- PASS: UX coherence is preserved with explicit hydration, import diagnostics, export warnings, and
  local-library switching states rather than hidden background behavior.
- PASS: Performance expectations remain explicit and favor local operations that avoid unnecessary
  network traffic.
- PASS: Security stays within the constitution by treating imported text as untrusted, by avoiding
  automatic server transfer of imported deck documents, and by constraining local persistence to
  non-secret user content.

## Complexity Tracking

No constitutional violations currently require justification.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| None | N/A | N/A |
