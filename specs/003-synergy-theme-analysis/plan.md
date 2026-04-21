# Implementation Plan: Synergy Scoring & Deck Theme Analysis

**Branch**: `003-synergy-theme-analysis` | **Date**: 2026-04-21 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/003-synergy-theme-analysis/spec.md`

## Summary

Adds a ~35-theme taxonomy engine that pre-computes oracle-text-derived theme signals per card at ingestion time (stored as a `Dictionary<string, decimal>?` column in `cards.parquet`). At analysis time, `ThemeAnalysisService` aggregates pre-computed signals across the deck, weights the commander at 3×, and produces ranked `DeckTheme` results, a `CommanderAlignment` indicator, and a thematic synergy score (0–100, qualitative label). An optional secondary signal blends per-card EDHREC synergy values at 30% weight (70/30 local/EDHREC split), cached 15 min in `IMemoryCache`. Results are surfaced in a new `ThemeAnalysisPanel` Blazor component alongside the existing `AnalysisPanel`.

**Key clarifications resolved (2026-04-21)**:
- Taxonomy expanded from the initial 20 to **~35 themes** (34 canonical; see `ThemeTaxonomy`) — oracle/keyword-detectable only; meta-labels and 1v1 archetypes excluded
- Graveyard strategies split into **three distinct themes**: "Reanimator", "Aristocrats", "Self-Mill / Dredge"
- **Combo** included with proxy signals (tutor density, Storm keyword, untap-engine patterns, exile-library win conditions); documented as lower-confidence in `ThemeDefinition`
- **Tribal** display name retained (EDHREC/player vocabulary); detection covers both "tribal" and "kindred" oracle text
- Niche mechanics (Cycling, Cascade, Energy, Morph, Mutate) deferred to a future iteration

## Technical Context

**Language/Version**: .NET 10 / C# 14  
**Primary Dependencies**: ASP.NET Core 10, Blazor Web App (Interactive Auto render mode), MudBlazor, Parquet.Net 5.2.0, `System.Text.RegularExpressions` (NonBacktracking engine), `Microsoft.Extensions.Http.Resilience` (EDHREC HTTP client), xUnit, bUnit, FluentAssertions  
**Storage**: Local Parquet snapshot (`cards.parquet`) — additive `ThemeSignals` column; in-memory `IMemoryCache` for EDHREC per-commander data (15-min TTL)  
**Testing**: xUnit (domain, application, infrastructure layers), bUnit (Blazor component tests), FluentAssertions  
**Target Platform**: ASP.NET Core 10 Blazor Web App, Interactive Auto render mode  
**Project Type**: web-app (extension to existing Commander deck workspace)  
**Performance Goals**: Full 100-card analysis ≤ 3 s (NFR-001); incremental update on single-card change ≤ 1 s; EDHREC fetch non-blocking — local analysis available immediately, EDHREC blend applied when cache resolves  
**Constraints**: Oracle text evaluated server-side via compiled regex only — no user-supplied free-text treated as logic (NFR-003); OWASP SSRF mitigated via slug allowlist before any HTTP call; WCAG AA accessible states required (NFR-002); Parquet column change must be backward-compatible (null treated as empty at read time)  
**Scale/Scope**: Single-user deck workspace; ~45 k cards in Parquet; ~35 static theme definitions; 100-card Commander decks  
**Security/Privacy Review**: SSRF (EDHREC slug validated against `^[a-z0-9][a-z0-9-]{1,80}[a-z0-9]$` before URL construction); Injection (all oracle text matching via server-side compiled Regex — no user input evaluated as code or query); Dependency exposure (HttpClient hardened with `AddStandardResilienceHandler()`, 10 s timeout, `User-Agent` pinned); Secrets (Edhrec:BaseUrl in `appsettings.json`, not committed secrets); Logging (Warning level for EDHREC failures, no card data logged at Info or above)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-checked post-design. All items pass.*

- **Code quality** ✅ — .NET 10 / C# 14; nullable reference types enabled; all new types are `sealed record` or `sealed class`; analyzers active; no justified deviations required
- **Tests** ✅ — xUnit unit tests for `ThemeTaxonomy`, `ThemeMatchingService`, `ThemeAnalysisService`; xUnit tests for `EdhrecClient` with mocked `HttpMessageHandler`; bUnit component tests for all four `ThemeAnalysisPanel` states (loading, empty, error, ready); integration test validating end-to-end analysis of a known deck configuration against the `/api/deck/analyse` endpoint (NFR-004)
- **UX states** ✅ — Loading (computing), Empty (< 20 cards — "Add more cards" placeholder), Error (analysis failed), Ready (results available); ARIA roles and labels required per NFR-002; `IsLoading → HasError → IsInsufficient → Ready` state evaluation order enforced (matches existing AnalysisPanel convention)
- **Performance** ✅ — Pre-computed `ThemeSignals` in Parquet eliminate per-card oracle-text scanning at runtime; commander-weighted aggregation is O(n) over deck size; EDHREC blend is async and non-blocking; budgets per NFR-001
- **Security** ✅ — SSRF: slug allowlist before HTTP; Injection: regex-only oracle text evaluation; Dependencies: resilience handler + timeout; Logging: no PII or full card data at Info+
- **Constitutional exceptions** — None

## Project Structure

### Documentation (this feature)

```text
specs/003-synergy-theme-analysis/
├── plan.md              # This file
├── research.md          # Phase 0 — technology decisions and theme taxonomy
├── data-model.md        # Phase 1 — domain types, application services, infrastructure
├── quickstart.md        # Phase 1 — developer setup and integration guide
├── contracts/
│   └── theme-analysis-api.yaml    # OpenAPI extension to /api/deck/analyse
└── tasks.md             # Phase 2 output (generated by /speckit.tasks — not this command)
```

### Source Code

```text
src/
├── CommandSynergy.Domain/
│   ├── Analysis/
│   │   ├── ThemeDefinition.cs        (new)
│   │   ├── ThemeTaxonomy.cs          (new — ~35 static theme definitions)
│   │   ├── DeckTheme.cs              (new)
│   │   ├── CommanderAlignment.cs     (new)
│   │   ├── ThemeAnalysis.cs          (new)
│   │   └── SynergyAssessment.cs      (modified — add ThemeScore, FinalScore, QualitativeLabel)
│   └── Cards/
│       └── CardProfile.cs            (modified — add ThemeSignals property)
├── CommandSynergy.Application/
│   └── Analysis/
│       ├── ThemeMatchingService.cs   (new — per-card signal computation, used at ingestion)
│       └── ThemeAnalysisService.cs   (new — deck-level aggregation, scoring, EDHREC blend)
├── CommandSynergy.Infrastructure/
│   ├── CardMetadata/
│   │   └── ParquetCardMetadataStore.cs  (modified — add ThemeSignals column, backward-compat null)
│   ├── Scryfall/
│   │   └── ScryfallClient.cs            (modified — add Keywords field to ScryfallCardDocument)
│   └── Edhrec/
│       ├── EdhrecClient.cs              (new — SSRF-guarded HTTP client, slug allowlist)
│       └── EdhrecCommanderDocument.cs   (new — deserialization model)
├── CommandSynergy/
│   └── Components/
│       └── ThemeAnalysisPanel.razor     (new — ranked themes, synergy bar, commander alignment, off-theme list)
└── CommandSynergy.Ingestion/
    └── Program.cs                       (modified — call ThemeMatchingService during bulk import)

tests/
├── CommandSynergy.Domain.Tests/
│   └── Analysis/
│       └── ThemeTaxonomyTests.cs           (new — signal patterns for all ~35 themes)
├── CommandSynergy.Application.Tests/
│   └── Analysis/
│       ├── ThemeMatchingServiceTests.cs    (new — known cards → expected signals)
│       └── ThemeAnalysisServiceTests.cs    (new — focused vs. unfocused deck scoring)
├── CommandSynergy.Infrastructure.Tests/
│   └── Edhrec/
│       └── EdhrecClientTests.cs            (new — slug validation, parse, error handling)
└── CommandSynergy.WebUI.Tests/
    └── Components/
        └── ThemeAnalysisPanelTests.cs      (new — all four UI states via bUnit)
```

**Structure Decision**: Existing Clean Architecture layout extended. Domain types added to `CommandSynergy.Domain/Analysis/`; application services to `CommandSynergy.Application/Analysis/`; infrastructure split between `CardMetadata/` (Parquet), `Scryfall/` (keywords field), and new `Edhrec/` (external client). New `ThemeAnalysisPanel.razor` co-located with existing components in `CommandSynergy/Components/`. No new projects required.
