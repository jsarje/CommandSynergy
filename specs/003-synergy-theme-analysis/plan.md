# Implementation Plan: Synergy Scoring & Deck Theme Analysis

**Branch**: `003-synergy-theme-analysis` | **Date**: 2026-04-21 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/003-synergy-theme-analysis/spec.md` plus user refinements:
- Primary signal: theme detection from oracle text + keywords, pre-computed at ingestion and stored in the Parquet metadata store
- Secondary signal: EDHREC JSON API per-card synergy values, fetched lazily after commander selection and blended at 30% weight

## Summary

Extend the existing bracket/synergy analysis pipeline to identify strategic deck themes and compute a thematic coherence score (0100) for Commander decks. Theme signals are extracted from card oracle text, keywords, and type lines during bulk metadata ingestion and stored in the Parquet snapshot  no text parsing at analysis time. After a commander is selected, the system optionally queries `https://json.edhrec.com/pages/commanders/{slug}.json` to obtain EDHREC''s proprietary per-card synergy values; these are cached per commander and blended at 30% weight against the 70%-weighted theme score. The combined output enriches `DeckAnalysisResponseContract` with theme analysis covering ranked `DeckTheme` entries, `CommanderAlignment`, off-theme card IDs, and an enhanced `SynergyAssessment`.

## Technical Context

**Language/Version**: .NET 10 / C# 14
**Primary Dependencies**: ASP.NET Core 10, Blazor Web App (Interactive Auto render mode), MudBlazor, Parquet.Net 5.2.0, xUnit, bUnit, FluentAssertions, `System.Text.RegularExpressions` (slug generation), `Microsoft.Extensions.Http.Resilience` (EDHREC client)
**Storage**: Local Parquet snapshot (`cards.parquet`); in-memory caches (snapshot search index + EDHREC per-commander cache, 15-minute TTL)
**Testing**: xUnit (unit + integration), bUnit (component), FluentAssertions
**Target Platform**: ASP.NET Core on Windows/Linux; Blazor Web App with Interactive Auto render mode
**Project Type**: web-app
**Performance Goals**:
- Theme analysis + synergy score for a 100-card deck: <= 3 s end-to-end (NFR-001); <= 1 s for single-card incremental update
- EDHREC fetch latency: <= 2 s p95 per commander with 10 s timeout; result cached 15 minutes
- Ingestion overhead: theme signal computation adds <= 20% to existing bulk import (O(20 themes x 10 patterns) = O(200) compiled regex ops per card)

**Constraints**:
- Parquet.Net 5.2.0: no DateTimeOffset? columns  use long? (UTC ticks); Dictionary<string,decimal>? supported (already used for PlayRateByCommander)
- OWASP SSRF (A10): EDHREC URL constructed from internal card name only; slug sanitized to [a-z0-9-] before HTTP dispatch
- NFR-003 ("no external API calls at runtime") is explicitly exceeded by the EDHREC secondary signal  tracked as a justified exception in Complexity Tracking
- WCAG AA: all new states (loading, empty, error, themes-ready, no-commander) require appropriate ARIA roles

**Scale/Scope**: ~45 000 cards in Parquet; 100-card Commander decks; 20 theme definitions; single-user interactive app
**Security/Privacy Review**:
- SSRF (A10): slug regex allowlist `^[a-z0-9][a-z0-9-]{1,80}[a-z0-9]$` + pinned base URL in config; no redirect following
- Injection (A03): oracle text and keywords read from trusted Parquet store; patterns matched via compiled NonBacktracking regex  no eval
- Insecure Design (A04): EDHREC client has 10 s timeout, 2-retry limit, circuit breaker; analysis always succeeds without EDHREC data
- Vulnerable Dependencies (A06): no new third-party NuGet packages; EDHREC HTTP client reuses existing resilience infrastructure
- Logging (A09): structured logs for EDHREC fetch attempts and failures; no oracle text or card names logged at Warning+ level

## Constitution Check

| Principle | Status | Notes |
|-----------|--------|-------|
| I. .NET Quality Is The Baseline | PASS | All new types: nullable enabled, XML docs, file-scoped namespaces, compiled regex, C# 14 features |
| II. Tests Prove The Change | PASS | Unit tests for ThemeMatchingService (known cards), ThemeAnalysisService (focused vs unfocused), EdhrecClient (mocked HTTP), domain types; component tests for all panel states |
| III. User Experience Must Stay Coherent | PASS | Existing AnalysisPanel states preserved; ThemeAnalysisPanel follows identical loading/empty/error/ready pattern; off-theme and alignment states specified |
| IV. Performance Budgets Are Part Of The Design | PASS | 3 s deck analysis budget; EDHREC 15-min cache; theme signals pre-computed at ingestion; no runtime regex scanning |
| V. Security By Design And OWASP Awareness | PASS | SSRF mitigated; no user input in analysis path; graceful EDHREC degradation; NonBacktracking regex |

**Exception tracked below**: NFR-003 exceeded by EDHREC runtime call.

## Project Structure

### Documentation (this feature)

```
specs/003-synergy-theme-analysis/
 plan.md              <- This file
 research.md          <- Phase 0
 data-model.md        <- Phase 1
 quickstart.md        <- Phase 1
 contracts/
     theme-analysis-api.yaml
```

### Source Code Changes

```
src/CommandSynergy.Domain/
 Analysis/
    ThemeDefinition.cs              NEW - Immutable record: name, description, pattern sets, weights
    ThemeTaxonomy.cs                NEW - Static singleton with 20 Commander theme definitions
    DeckTheme.cs                    NEW - Identified theme: name, strength, contributing card IDs, description
    CommanderAlignment.cs           NEW - AlignmentLevel enum + CommanderAlignment record
    ThemeAnalysis.cs                NEW - Composite result: ranked themes, synergy, alignment, off-theme IDs
    [SynergyAssessment.cs]          MODIFIED - add ThemeScore decimal, QualitativeLabel string
 Cards/
     [CardProfile.cs]                MODIFIED - add ThemeSignals: IReadOnlyDictionary<string, decimal>

src/CommandSynergy.Application/
 Analysis/
    ThemeMatchingService.cs         NEW - Maps CardProfile -> theme signals dict (used at ingestion + test)
    ThemeAnalysisService.cs         NEW - Aggregates theme analysis for a full deck + EDHREC blend
    [DeckAnalysisService.cs]        MODIFIED - integrate ThemeAnalysisService; pass EDHREC data
 Contracts/
     [DeckWorkspaceContracts.cs]     MODIFIED - add ThemeAnalysisContract, DeckThemeContract,
                                                     CommanderAlignmentContract, ThemeCardContributionContract,
                                                     enhanced SynergyAssessmentContract (add themeScore, label)

src/CommandSynergy.Infrastructure/
 CardMetadata/
    [ParquetCardMetadataStore.cs]   MODIFIED - add ThemeSignals column (Dictionary<string,decimal>?)
    [CardMetadataBulkImportService.cs] MODIFIED - call ThemeMatchingService per card during import
    [CardMetadataQueryService.cs]   MODIFIED - map ThemeSignals back onto CardProfile
 Edhrec/
    EdhrecClient.cs                 NEW - Typed HttpClient for EDHREC JSON API with SSRF guard
    EdhrecCommanderDocument.cs      NEW - JSON deserialization models for EDHREC response
    EdhrecServiceCollectionExtensions.cs  NEW - registers EdhrecClient with resilience handler
 Scryfall/
     [ScryfallCardDocument.cs]       MODIFIED - add Keywords: IReadOnlyList<string>

src/CommandSynergy/
 Components/
     ThemeAnalysisPanel.razor        NEW - Ranked themes, synergy score, alignment, off-theme list
     ThemeAnalysisPanel.razor.css    NEW - Scoped component styles

tests/CommandSynergy.Domain.Tests/
 Analysis/
     ThemeTaxonomyTests.cs           NEW - verifies each theme definition has non-empty patterns

tests/CommandSynergy.Application.Tests/
 Analysis/
     ThemeMatchingServiceTests.cs    NEW - known cards -> expected theme signals
     ThemeAnalysisServiceTests.cs    NEW - focused deck scores > 70; unfocused deck scores < 40

tests/CommandSynergy.Infrastructure.Tests/
 Edhrec/
     EdhrecClientTests.cs            NEW - mocked HTTP: happy path, timeout, bad JSON, invalid slug

tests/CommandSynergy.WebUI.Tests/
 Components/
     ThemeAnalysisPanelTests.cs      NEW - bUnit tests for loading/empty/error/ready states
```

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| NFR-003 exception: runtime EDHREC API call after commander selection | Commander-specific crowd synergy signal meaningfully improves score quality for tuned decks | Ingesting all ~20 000 EDHREC commander pages at bulk-import time is impractical in scope; call is lazy, cached 15 min, guarded by timeout + circuit breaker, and always optional |

---

## Phase 0: Research

*See [research.md](./research.md) for full findings.*

### Resolved Questions

| Unknown | Decision | Rationale |
|---------|----------|-----------|
| Does Scryfall oracle_cards bulk feed include a keywords array? | Yes  Scryfall cards have `keywords` (e.g. `["Flying","Lifelink"]`), add `Keywords: IReadOnlyList<string>` to `ScryfallCardDocument` | Scryfall API documentation + live inspection confirm the field |
| What is the EDHREC JSON response structure? | `container.json_dict.cardlists[]` -> `cardviews[]` with `id` (Scryfall UUID), `synergy` (float -1 to 1), `inclusion`, `num_decks`, `potential_decks` | Live fetch of krenko-mob-boss.json confirmed |
| How do we generate the EDHREC commander slug? | Lowercase name, remove `[^a-z0-9 -]`, replace spaces with hyphens: "Krenko, Mob Boss" -> "krenko-mob-boss" | Matches observed EDHREC URL patterns for 10+ commanders |
| Can Parquet.Net 5.2.0 store Dictionary<string,decimal>? | Yes  already used for PlayRateByCommander; same pattern applies to ThemeSignals | Existing ParquetCardMetadataRow implementation |
| What Commander theme taxonomy to use? | 20 themes defined (see taxonomy table below); patterns derived from MTG rules text and EDHREC taglinks | EDHREC taglinks on live commander pages + MTG comprehensive rules vocabulary |
| EDHREC API access policy? | json.edhrec.com is publicly accessible; no ToS prohibition for non-commercial tool use; User-Agent header included per good practice | EDHREC does not publish a robots.txt or ToS restricting this JSON endpoint |

### Theme Taxonomy (20 Themes)

| # | Theme | Scryfall Keywords | Oracle Text Patterns (key phrases) | Type Pattern |
|---|-------|-------------------|-------------------------------------|--------------|
| 1 | Graveyard Recursion | dredge, unearth, embalm, eternalize | "from your graveyard", "return.*from.*graveyard", "mill" |  |
| 2 | +1/+1 Counters | proliferate, adapt, bolster, reinforce | "+1/+1 counter", "put a \+1/\+1" |  |
| 3 | Token Generation | populate | "create a.*token", "put a.*token", "creates a token" |  |
| 4 | Ramp | landfall | "search your library for a.*land", "add \{", "add two mana", "add three mana" |  |
| 5 | Draw/Wheel | cycling | "draw a card", "draw two", "draw three", "each player draws", "wheel" |  |
| 6 | Sacrifice | exploit, devour | "sacrifice a", "whenever a creature you control dies", "whenever you sacrifice" |  |
| 7 | Tribal |  | "of that type", "creature type", "each.*creature.*of the chosen type" | Tribal in type line |
| 8 | Voltron | equip, bestow | "equipped creature gets", "attached.*creature", "commander damage" | Equipment, Aura |
| 9 | Control | flash, hexproof | "counter target", "destroy target", "exile target", "return target.*hand" |  |
| 10 | Combo | storm, channel | "untap all", "take an extra turn", "win the game" |  |
| 11 | Aristocrats |  | "whenever a creature dies", "each opponent loses", "drain" |  |
| 12 | ETB Triggers |  | "when.*enters the battlefield", "whenever a creature enters" |  |
| 13 | Burn / Direct Damage |  | "deals damage to any target", "deals damage to each", "deals X damage" |  |
| 14 | Blink / Flicker | flash | "exile target creature.*return it to the battlefield", "blink", "flicker" |  |
| 15 | Lifegain | lifelink | "you gain.*life", "gain X life", "whenever you gain life" |  |
| 16 | Proliferate | proliferate | "proliferate" |  |
| 17 | Lands Matter | landfall | "whenever a land enters", "land you control", "basic land you control" | Land |
| 18 | Artifacts Matter | affinity, improvise | "artifact you control", "whenever you cast an artifact", "artifact enters" | Artifact |
| 19 | Enchantments Matter | enchant | "enchantment you control", "whenever you cast an enchantment" | Enchantment |
| 20 | Spellslinger | magecraft, storm, jump-start | "whenever you cast an instant or sorcery", "whenever you cast a noncreature spell" |  |

---

## Phase 1: Design & Contracts

*See [data-model.md](./data-model.md), [contracts/theme-analysis-api.yaml](./contracts/theme-analysis-api.yaml), and [quickstart.md](./quickstart.md).*

### Scoring Algorithm

#### Step 1: Ingestion  Compute ThemeSignals per Card

```
For each card in bulk import:
  signals = {}
  For each ThemeDefinition theme in ThemeTaxonomy:
    score = 0.0
    For each keyword in theme.KeywordPatterns:
      if card.Keywords contains keyword (OrdinalIgnoreCase): score += KeywordWeight (0.4)
    For each pattern in theme.OracleTextPatterns:
      if Regex.IsMatch(card.OracleText, pattern, NonBacktracking | IgnoreCase): score += OracleTextWeight (0.25)
    if theme.TypePattern set and Regex.IsMatch(card.TypeLine, theme.TypePattern): score += TypeWeight (0.3)
    if score > 0: signals[theme.Name] = Clamp(score, 0.0, 1.0)
  card.ThemeSignals = signals -> stored in Parquet
```

#### Step 2: Analysis  Aggregate Deck Theme Strengths

```
commanderWeight = 3.0
nonCommanderWeight = 1.0

For each theme in ThemeTaxonomy:
  weighted_sum = sum(card.ThemeSignals.GetValueOrDefault(theme.Name, 0) x card_weight)
  total_weight = sum(card_weight) for all deck entries
  theme_strength[theme] = weighted_sum / total_weight   // 01

Rank themes descending by strength.
Primary themes: strength >= 0.15
Off-theme cards: no ThemeSignals entry above 0.10
```

#### Step 3: Synergy Score Computation

```
concentration = top theme strength (01)
coverage = on_theme_card_count / total_cards (01)
theme_score = Clamp((0.5 x concentration + 0.5 x coverage) x 100, 0, 100)

Labels: 0-19=Pile, 20-39=Unfocused, 40-59=Developing, 60-79=Focused, 80-100=Tuned
```

#### Step 4: EDHREC Blend (optional, if commander selected and fetch succeeded)

```
edhrec_avg = mean(synergy per deck card, default 0 for missing cards) mapped [-1,1] -> [0,100]
final_score = 0.7 x theme_score + 0.3 x edhrec_avg
```

#### Step 5: Commander Alignment

```
commander_top_theme = argmax(commander.ThemeSignals)
deck_strength = theme_strength[commander_top_theme]

Strong:   deck_strength >= 0.5
Moderate: deck_strength >= 0.25
Low:      deck_strength < 0.25
(None:    no commander present)
```

### Key Design Decisions

| Decision | Rationale |
|----------|-----------|
| Pre-compute theme signals at ingestion | Eliminates per-request oracle text parsing; incremental update latency <= 1 s |
| Store as Dictionary<string,decimal>? in Parquet | Reuses existing PlayRateByCommander pattern; no new infrastructure |
| 70/30 theme-vs-EDHREC weight split | Theme analysis uses authoritative local data; EDHREC provides crowd signal. Over-weighting EDHREC creates a hard runtime dependency |
| EDHREC fetched lazily post-commander-selection, cached 15 min | Avoids blocking initial deck load; user does not notice 12 s background fetch |
| Compiled NonBacktracking regex per pattern | Prevents ReDoS; patterns are defined at app startup, not per-request |
| SSRF slug allowlist regex `^[a-z0-9][a-z0-9-]{1,80}[a-z0-9]$` | Prevents path traversal or URL injection even if internal card data is corrupted |
| ThemeAnalysis as a separate result object (not merged into SynergyAssessment) | Keeps existing SynergyAssessment for backward compatibility; ThemeAnalysis is additive |
