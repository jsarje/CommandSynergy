# Quickstart: Synergy Scoring & Deck Theme Analysis

**Feature**: 003-synergy-theme-analysis  
**Date**: 2026-04-21  
**Branch**: `003-synergy-theme-analysis`

---

## What This Feature Adds

1. **Theme signals pre-computed at ingestion** — `ThemeMatchingService` analyses oracle text, keywords, and type lines against a ~35-theme taxonomy (34 canonical themes) and stores results in the Parquet `cards.parquet` snapshot.
2. **ThemeAnalysis at deck analysis time** — `ThemeAnalysisService` aggregates pre-computed signals, weights the commander at 3×, and produces ranked `DeckTheme` entries, a `CommanderAlignment`, off-theme card IDs, and a thematic synergy score (0–100).
3. **Optional EDHREC blend** — after a commander is selected, `EdhrecClient` fetches `https://json.edhrec.com/pages/commanders/{slug}.json` and blends EDHREC per-card synergy values at 30% weight. This is cached 15 min and always optional (graceful degradation).
4. **`ThemeAnalysisPanel` Blazor component** — displays ranked theme chips, synergy score bar, commander alignment indicator, and off-theme card list.

---

## Prerequisites

- Branch `003-synergy-theme-analysis` checked out
- `src/CommandSynergy.slnx` builds cleanly
- Parquet snapshot at the path configured in `CardMetadataOptions:SnapshotDirectory`

---

## Step 1: Re-run Bulk Ingestion (populate ThemeSignals)

After implementing `ThemeMatchingService` and the Parquet column changes, run the ingestion console app to rebuild `cards.parquet` with `ThemeSignals` populated:

```powershell
cd src/CommandSynergy.Ingestion
dotnet run
```

Expected output:
```
info: Imported 45312 cards from https://... at 2026-04-21T...
```

Cards that previously lacked `ThemeSignals` data will now have it populated.

> **Note**: Existing `cards.parquet` files without the `ThemeSignals` column are backward compatible — the column reads as `null` and maps to an empty dictionary at runtime.

---

## Step 2: Register EdhrecClient (DI)

Add to `src/CommandSynergy.Infrastructure/DependencyInjection.cs`:

```csharp
services.AddEdhrecClient(configuration);
```

Add `Edhrec:BaseUrl` to `appsettings.json`:

```json
{
  "Edhrec": {
    "BaseUrl": "https://json.edhrec.com/",
    "UserAgent": "CommandSynergy/0.1"
  }
}
```

---

## Step 3: Wire ThemeAnalysisService into DeckAnalysisService

`DeckAnalysisService` calls `ThemeAnalysisService.AnalyseAsync` after loading card profiles:

```csharp
// After loading cardProfiles:
var edhrecData = commanderProfile is not null
    ? await edhrecCache.GetOrFetchAsync(commanderProfile.Name, ct)
    : null;

var (themeAnalysis, enhancedSynergy) = await themeAnalysisService.AnalyseAsync(
    deck, cardProfiles, edhrecData, ct);
```

The `DeckAnalysisResponseContract` is extended with a `ThemeAnalysis` property.

---

## Step 4: Run Tests

```powershell
cd ..
dotnet test src/CommandSynergy.slnx
```

All tests must pass. New test files:
- `tests/CommandSynergy.Domain.Tests/Analysis/ThemeTaxonomyTests.cs`
- `tests/CommandSynergy.Application.Tests/Analysis/ThemeMatchingServiceTests.cs`
- `tests/CommandSynergy.Application.Tests/Analysis/ThemeAnalysisServiceTests.cs`
- `tests/CommandSynergy.Infrastructure.Tests/Edhrec/EdhrecClientTests.cs`
- `tests/CommandSynergy.WebUI.Tests/Components/ThemeAnalysisPanelTests.cs`

---

## Key Types Quick Reference

| Type | Location | Purpose |
|------|----------|---------|
| `ThemeDefinition` | Domain/Analysis | Immutable record: name, patterns, weights |
| `ThemeTaxonomy` | Domain/Analysis | Static list of 20 themed definitions |
| `DeckTheme` | Domain/Analysis | Theme result: name, strength, card IDs |
| `CommanderAlignment` | Domain/Analysis | Alignment level + evidence cards |
| `ThemeAnalysis` | Domain/Analysis | Composite result from ThemeAnalysisService |
| `ThemeMatchingService` | Application/Analysis | Card → theme signals (used at ingestion) |
| `ThemeAnalysisService` | Application/Analysis | Deck → ThemeAnalysis + enhanced SynergyAssessment |
| `EdhrecClient` | Infrastructure/Edhrec | Typed HTTP client for EDHREC JSON API |
| `EdhrecCommanderData` | Infrastructure/Edhrec | Flattened per-card synergy data + cache flag |
| `ThemeAnalysisPanel` | Server/Components | Blazor component: themes, score, alignment |

---

## Synergy Score Labels

| Range | Label | Meaning |
|-------|-------|---------|
| 80–100 | Tuned | Deck is highly focused; most cards support 1–2 themes |
| 60–79 | Focused | Clear primary theme with supporting elements |
| 40–59 | Developing | Some thematic direction but room to tighten |
| 20–39 | Unfocused | Multiple weak themes; limited internal synergy |
| 0–19 | Pile | No discernible theme; random card collection |

---

## EDHREC Integration Notes

- The EDHREC JSON endpoint is `https://json.edhrec.com/pages/commanders/{slug}.json`
- Slugs are generated from `CardProfile.Name`: lowercase, remove `[^a-z0-9 -]`, spaces→hyphens
- All slugs are validated against `^[a-z0-9][a-z0-9-]{1,80}[a-z0-9]$` before HTTP dispatch
- Timeout: 10 seconds; no more than 2 retry attempts
- Cache: 15-minute TTL per commander slug using `IMemoryCache`
- If fetch fails for any reason, `FinalScore = ThemeScore` (no user-visible error)

---

## Development Tips

- **Testing theme signals**: Use `ThemeMatchingService.ComputeThemeSignals` directly with a hand-crafted `CardProfile` to verify a specific card's signals without running ingestion.
- **Inspecting Parquet**: After ingestion, use the `ParquetCardMetadataStore` in a test or console app to read back cards and check their `ThemeSignals` dictionaries.
- **EDHREC mock**: In tests, inject a mock `EdhrecClient` returning `EdhrecCommanderData.Empty` (graceful fallback) or a hand-crafted synergy dictionary to test blending logic in isolation.
- **Regex debugging**: All compiled patterns are in `ThemeTaxonomy.Default`; you can iterate them in a test to validate pattern matching against known card oracle text.
