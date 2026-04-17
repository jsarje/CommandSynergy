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
3. Add a derived search-index generator that produces a compact client artifact from Parquet data.
4. Add integration tests for:
   - Scryfall mapping and failure handling
   - Parquet snapshot loading
   - search artifact generation

## Minimal Host Validation Path

1. Register Domain, Application, and Infrastructure services in `src/CommandSynergy/Program.cs`.
2. Add minimal JSON endpoints or server handlers for:
   - card search
   - deck validation
   - deck analysis
3. Add integration tests that verify endpoint wiring, serialization, and validation failures.

## Ready For UI Phase When

- Domain and application tests for rules and scoring pass.
- Infrastructure can load a representative metadata snapshot and serve the derived search index.
- Minimal host contracts for search, validation, and analysis are stable.
- Performance checks show the chosen search and analysis paths meet the plan budgets.

## Verification Commands

```powershell
dotnet restore src/CommandSynergy.slnx
dotnet build src/CommandSynergy.slnx
dotnet test src/CommandSynergy.slnx
```

## Deferred To UI Phase

- MudBlazor registration and theme work
- drag-and-drop pile workspace components
- custom CSS 3D card flip behavior
- high-fidelity salt-score visual treatment