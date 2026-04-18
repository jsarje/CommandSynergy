# Quickstart: Commander Synergy Sphere

## Goal

Stand up the Clean Architecture skeleton for the commander deck architect, prove the commander
rules and analysis core, and expose minimal JSON contracts before beginning MudBlazor-heavy UI work.

## Prerequisites

- .NET 10 SDK installed
- Existing solution at `src/CommandSynergy.slnx`
- Network access for Scryfall integration development and sample metadata refresh

## Phase 1 Setup

1. Add class library projects under `src/` for:
   - `CommandSynergy.Domain`
   - `CommandSynergy.Application`
   - `CommandSynergy.Infrastructure`
2. Add test projects under `tests/` for:
   - `CommandSynergy.Domain.Tests`
   - `CommandSynergy.Application.Tests`
   - `CommandSynergy.Infrastructure.Tests`
   - `CommandSynergy.WebUI.Tests`
3. Reference projects so dependency flow is:
   - WebUI host -> Application, Infrastructure
   - Client -> Application abstractions or DTO contracts only
   - Application -> Domain
   - Infrastructure -> Application, Domain

## Domain-First Validation Path

1. Implement commander rules and deck aggregate behavior in Domain.
2. Add xUnit tests for:
   - 100-card validation
   - singleton enforcement
   - color identity enforcement
   - companion rule handling
   - MDFC and alternate-face legality handling
3. Implement bracket and synergy orchestration in Application.
4. Add xUnit tests for:
   - bracket factor weighting
   - bracket level mapping
   - synergy normalization
   - explanation generation for both analysis outputs

## Infrastructure Path

1. Add typed Scryfall client and configuration objects in Infrastructure.
2. Add Parquet-backed metadata adapter for authoritative local card data.
3. Add immediate write-through behavior so successful Scryfall fallback resolutions upsert cards
   into the local Parquet snapshot during normal user interactions.
4. Add commander-eligibility metadata mapping so the app can filter legal commanders before deck
   construction and still honor explicit official exceptions.
5. Add a derived search-index generator that produces a compact client artifact from Parquet data.
6. Add integration tests for:
   - Scryfall mapping and failure handling
   - Parquet snapshot loading
   - Parquet snapshot upsert behavior from Scryfall fallbacks
   - commander-eligibility metadata mapping and selection enforcement
   - search artifact generation

## Minimal Host Validation Path

1. Register Domain, Application, and Infrastructure services in `src/CommandSynergy/Program.cs`.
2. Add minimal JSON endpoints or server handlers for:
   - card search
   - deck validation
   - deck analysis
3. Add integration tests that verify endpoint wiring, serialization, and validation failures.
4. Ensure commander selection UI and server validation both reject non-eligible commanders before a
   deck can proceed as a legal Commander build.

## Ready For UI Phase When

- Domain and application tests for rules and scoring pass.
- Infrastructure can load a representative metadata snapshot and serve the derived search index.
- Infrastructure can upsert Scryfall-resolved cards into the local Parquet snapshot and reuse them
   on subsequent requests.
- Minimal host contracts for search, validation, and analysis are stable.
- Performance checks show the chosen search and analysis paths meet the plan budgets.

## Verification Commands

```powershell
dotnet restore src/CommandSynergy.slnx
dotnet build src/CommandSynergy.slnx
dotnet test src/CommandSynergy.slnx
```

## Validation Notes

1. Confirm `GET /api/cards/search?q=sol` returns a `snapshotVersion` and card summaries.
2. Confirm `POST /api/decks/validate` rejects blank commander identifiers and pathological payload sizes with `400 Bad Request` before domain execution.
3. Confirm `POST /api/decks/analyze` returns bracket and synergy payloads for a legal 100-card snapshot.
4. Confirm commander selection rejects cards that are not official Commander-eligible unless an
   explicit card-text or official-mechanic exception applies.
5. Confirm resolving a missing card through Scryfall adds it to the local Parquet snapshot so the
   next request is served locally.
6. Run the focused performance tests in `tests/CommandSynergy.Infrastructure.Tests/Performance` and `tests/CommandSynergy.Application.Tests/Performance` when search or scoring logic changes.
7. Run the security regression tests in `tests/CommandSynergy.WebUI.Tests/Security` when endpoint contracts or request validation changes.

## Deferred To UI Phase

- MudBlazor registration and theme work
- drag-and-drop pile workspace components
- custom CSS 3D card flip behavior
- high-fidelity salt-score visual treatment