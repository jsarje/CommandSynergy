# Implementation Plan: Commander Synergy Sphere

**Branch**: `[001-commander-deck-architect]` | **Date**: 2026-04-18 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-commander-deck-architect/spec.md`

## Summary

Build a server-authoritative Commander deck architect on the existing ASP.NET Core Blazor Web App,
with Domain rules for official Commander legality, Application services for bracket and synergy
analysis, and Infrastructure adapters for Scryfall and Parquet-backed card metadata. The current
clarifications require commander selection to follow official Commander eligibility rules and local
metadata to be enriched immediately by id-based Parquet upserts whenever normal user interactions
resolve cards through Scryfall fallback.

## Technical Context

**Language/Version**: .NET 10 / C# 14  
**Primary Dependencies**: ASP.NET Core Blazor Web App, Interactive Auto render mode, MudBlazor, Parquet.Net, typed HttpClient for Scryfall, xUnit, bUnit, FluentAssertions  
**Storage**: Local Parquet snapshot files for authoritative card metadata plus derived in-memory search index data  
**Testing**: xUnit for Domain/Application/Infrastructure, bUnit and host integration tests for WebUI/API behavior  
**Target Platform**: ASP.NET Core Blazor Web App on the existing CommandSynergy host with Interactive Auto client support  
**Project Type**: Web application with Clean Architecture class libraries  
**Performance Goals**: Search, validation, and analysis feedback within 2 seconds after deck mutations in at least 95% of test runs; search-index generation within current focused benchmark budget; reduce repeat Scryfall calls over time through immediate local write-through caching  
**Constraints**: Official Commander legality only for deck validation, bracket and Game Changers remain non-blocking guidance, commander selection must respect official commander eligibility exceptions, metadata failures must preserve user work, OWASP-aware external HTTP handling, no committed secrets, and local metadata must be incrementally enriched via id-based Parquet upserts  
**Scale/Scope**: Single-user deck-building workflow, one active Commander deck per session, 3 core service workflows (search, validate, analyze), and local metadata growth driven by normal app interactions  
**Security/Privacy Review**: Validate all inbound deck/search payloads, constrain outbound Scryfall calls to configured typed clients, avoid SSRF by fixed base URL, keep logs free of secrets, preserve availability during upstream failure with local metadata, and treat local snapshot writes as trusted server-side operations with bounded file paths and deterministic ids

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- PASS: Code quality approach remains standard .NET 10/C# 14 with analyzers, nullability, existing project formatting, and Clean Architecture dependency direction.
- PASS: Automated proof remains layered xUnit, infrastructure integration, host integration, and bUnit tests; metadata write-through and commander-eligibility changes require failing-then-passing regression coverage.
- PASS: UX impact is explicit for loading, empty, validation, error, and recovery states, especially when commander selection is rejected or metadata is missing or being enriched from Scryfall.
- PASS: Performance budgets are explicit for search, validation, analysis, and the reduction of repeat Scryfall calls through immediate local metadata persistence.
- PASS: Security review covers OWASP-relevant input validation, dependency trust, outbound HTTP restrictions, file-based snapshot writes, logging, and graceful degradation.
- PASS: No constitutional exception is currently required.

## Project Structure

### Documentation (this feature)

```text
specs/001-commander-deck-architect/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── deck-workspace-api.yaml
└── tasks.md
```

### Source Code (repository root)

```text
src/
├── CommandSynergy/                 # ASP.NET Core Blazor Web App host, components, endpoints
├── CommandSynergy.Client/          # Interactive Auto client services
├── CommandSynergy.Domain/          # Commander rules, deck and card models, bracket primitives
├── CommandSynergy.Application/     # Use cases, contracts, orchestration, options
└── CommandSynergy.Infrastructure/  # Scryfall client, Parquet access, metadata caching, logging

tests/
├── CommandSynergy.Domain.Tests/
├── CommandSynergy.Application.Tests/
├── CommandSynergy.Infrastructure.Tests/
└── CommandSynergy.WebUI.Tests/
```

**Structure Decision**: Use the existing Blazor Web App host with Domain, Application, and
Infrastructure class libraries under `src/`, plus layered test projects under `tests/`. This
matches the repository structure already established for the feature and cleanly separates
commander legality, analysis orchestration, external metadata integration, and UI behavior.

## Phase 0: Research Summary

- Use the existing server project as the host and keep business rules server-authoritative.
- Treat Parquet as the authoritative local metadata store, with Scryfall as the upstream fallback.
- Preserve bracket and Game Changer handling as analysis guidance, not legality validation.
- Enforce commander eligibility from official Commander rules: legendary creatures by default plus
  explicit card-text or official-mechanic exceptions only.
- Persist each successful Scryfall card resolution into the local Parquet snapshot immediately with
  id-based upsert semantics so normal usage reduces future external lookups.

## Phase 1: Design Summary

- Extend `CardProfile` and local snapshot records to capture commander-eligibility state and the
  metadata needed for authoritative selection filtering.
- Keep the existing deck workspace API contract stable for search, validation, and analysis while
  implementing commander-selection enforcement in Application/Domain logic and UI filtering. If
  additional commander-eligibility data is required, derive it from existing response fields or
  internal application logic rather than introducing a breaking contract change.
- Implement Parquet snapshot mutation support in Infrastructure so Scryfall fallbacks can write
  through to the local snapshot deterministically.
- Preserve current graceful degradation behavior so failed Scryfall calls do not destroy user work
  even when a card cannot yet be persisted locally.
- Perform an explicit OWASP-focused security review for Scryfall HTTP access, Parquet snapshot
  writes, API input handling, and audit visibility; implementation is not complete until no open
  high-severity findings remain for the delivered scope.

## Post-Design Constitution Check

- PASS: The design keeps .NET-first code quality expectations unchanged and uses existing project
  boundaries.
- PASS: Test impact is concrete: commander-selection eligibility, Parquet write-through upserts,
  and degraded metadata paths all require regression coverage.
- PASS: UX coherence is preserved by planning explicit invalid-selection, loading, and recovery
  states rather than relying on backend failures alone.
- PASS: Performance expectations remain explicit and are improved by reducing repeat Scryfall calls.
- PASS: Security remains within the constitution by limiting external communication to typed
  clients, validating inputs, and constraining snapshot writes to server-owned file paths.

## Complexity Tracking

No constitutional violations currently require justification.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| None | N/A | N/A |
