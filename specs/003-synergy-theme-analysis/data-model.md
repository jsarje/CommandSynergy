# Data Model: Synergy Scoring & Deck Theme Analysis

**Feature**: 003-synergy-theme-analysis  
**Date**: 2026-04-21

---

## Overview

This feature adds a theme-analysis layer on top of the existing card metadata and analysis pipeline. The primary data changes are:

1. **`CardProfile`** gains a `ThemeSignals` dictionary (pre-computed at ingestion, stored in Parquet)
2. **New domain types** (`ThemeDefinition`, `ThemeTaxonomy`, `DeckTheme`, `CommanderAlignment`, `ThemeAnalysis`) represent analysis results
3. **`SynergyAssessment`** gains theme-derived score fields
4. **New infrastructure** (`EdhrecClient`, `EdhrecCommanderDocument`) handles the optional secondary signal

---

## Domain Layer

### Modified: `CardProfile` — add ThemeSignals

**File**: `src/CommandSynergy.Domain/Cards/CardProfile.cs`

```
New property:
  ThemeSignals: IReadOnlyDictionary<string, decimal>
    - Keys: theme names from ThemeTaxonomy (e.g., "Graveyard Recursion")
    - Values: signal strength [0.0, 1.0]
    - Empty dictionary if no themes match (never null at runtime; Parquet null maps to empty)
    - Populated by ThemeMatchingService during ingestion
    - Read-only at analysis time
```

### Modified: `SynergyAssessment` — add theme-derived fields

**File**: `src/CommandSynergy.Domain/Analysis/SynergyAssessment.cs`

```
New fields on existing record:
  ThemeScore: decimal          // 0–100 theme-based coherence (before EDHREC blend)
  FinalScore: decimal          // 0–100 after EDHREC blend (= ThemeScore if no EDHREC data)
  QualitativeLabel: string     // "Pile" | "Unfocused" | "Developing" | "Focused" | "Tuned"
```

Existing fields preserved:
- `SynergyScore` (decimal 0–100) — kept for backward compatibility; will be superseded by `FinalScore` in responses
- `CommanderSpecificHits` — kept
- `StapleOverloadIndicators` — kept

---

### New: `ThemeDefinition`

**File**: `src/CommandSynergy.Domain/Analysis/ThemeDefinition.cs`

```
sealed record ThemeDefinition
  Name: string                         // e.g., "Graveyard Recursion"
  Description: string                  // Human-readable description for UI display
  KeywordPatterns: IReadOnlyList<string>  // Scryfall keyword matches (case-insensitive)
  OracleTextPatterns: IReadOnlyList<Regex>  // Pre-compiled regex patterns for oracle text
  TypePattern: Regex?                  // Optional type-line pattern
  KeywordWeight: decimal               // Weight per keyword hit (default 0.4)
  OracleTextWeight: decimal            // Weight per oracle pattern hit (default 0.25)
  TypeWeight: decimal                  // Weight for type pattern hit (default 0.3)
```

Signal scoring per card: `score = Clamp(sum_of_matched_weights, 0.0, 1.0)`

---

### New: `ThemeTaxonomy`

**File**: `src/CommandSynergy.Domain/Analysis/ThemeTaxonomy.cs`

```
static class ThemeTaxonomy
  Default: IReadOnlyList<ThemeDefinition>   // ~35 pre-defined themes (34 canonical), initialised at static ctor
  GetByName(name): ThemeDefinition?        // Lookup by theme name
```

All ~35 `ThemeDefinition` instances are constructed with `RegexOptions.Compiled | RegexOptions.NonBacktracking | RegexOptions.IgnoreCase` and stored as static fields. Thread-safe after static initialisation.

**Theme count notes (from spec clarification 2026-04-21)**:
- Graveyard strategies split into three separate themes: "Reanimator", "Aristocrats", "Self-Mill / Dredge"
- "Combo" included with proxy signals (`SignalConfidence = Low`); documented in `ThemeDefinition`
- "Tribal" display name used; detection covers both `\btribal\b` and `\bkindred\b` oracle text
- "Hatebears", "Wheels" (part of Card Draw / Wheels), and "Stax" (part of Control / Stax) now included
- Niche mechanics (Cycling, Cascade, Energy, Morph, Mutate) deferred to future iteration
- See `research.md` Section 5 for the full canonical theme list with detection patterns

---

### New: `DeckTheme`

**File**: `src/CommandSynergy.Domain/Analysis/DeckTheme.cs`

```
sealed record DeckTheme
  Name: string                            // Theme name from ThemeTaxonomy
  Description: string                     // Theme description
  Strength: decimal                       // Normalised [0.0, 1.0] — deck-level aggregate signal
  StrengthLabel: string                   // "Strong" (>=0.5) | "Moderate" (>=0.25) | "Supporting" (<0.25)
  ContributingCardIds: IReadOnlyList<string>  // CardIds of cards with ThemeSignals[this.Name] > 0
  ContributingCardCount: int              // Count of contributing cards
```

---

### New: `CommanderAlignment`

**File**: `src/CommandSynergy.Domain/Analysis/CommanderAlignment.cs`

```
enum AlignmentLevel
  None        // No commander in deck
  Low         // deck_strength < 0.25 for commander's top theme
  Moderate    // 0.25 <= deck_strength < 0.5
  Strong      // deck_strength >= 0.5

sealed record CommanderAlignment
  Level: AlignmentLevel
  CommanderTopTheme: string?              // Commander's strongest theme name (null if no signals)
  DeckStrengthForCommanderTheme: decimal  // Deck's strength in that theme
  EvidenceCardIds: IReadOnlyList<string>  // Top cards supporting commander's theme in the 99
```

---

### New: `ThemeAnalysis`

**File**: `src/CommandSynergy.Domain/Analysis/ThemeAnalysis.cs`

```
sealed record ThemeAnalysis
  RankedThemes: IReadOnlyList<DeckTheme>   // All themes with Strength > 0, ranked descending
  PrimaryThemes: IReadOnlyList<DeckTheme>  // Themes with Strength >= 0.15
  OffThemeCardIds: IReadOnlyList<string>   // Cards with no ThemeSignals > 0.10
  CommanderAlignment: CommanderAlignment
  AnalysedCardCount: int
  IsInsufficient: bool                     // True if fewer than 20 cards in deck
  AnalysedAtUtc: DateTimeOffset
```

---

## Application Layer

### New: `ThemeMatchingService`

**File**: `src/CommandSynergy.Application/Analysis/ThemeMatchingService.cs`

```
sealed class ThemeMatchingService
  ComputeThemeSignals(CardProfile profile) -> IReadOnlyDictionary<string, decimal>
    // Used at ingestion time to compute signals before Parquet write
    // Also used to re-compute signals for cards fetched live from Scryfall
    // Input: CardProfile with OracleText, Keywords, TypeLine
    // Output: dictionary of theme name -> signal strength [0,1]
    // Empty dict if no themes match
```

---

### New: `ThemeAnalysisService`

**File**: `src/CommandSynergy.Application/Analysis/ThemeAnalysisService.cs`

```
sealed class ThemeAnalysisService
  AnalyseAsync(
    Deck deck,
    IReadOnlyDictionary<string, CardProfile> cardProfiles,
    EdhrecCommanderData? edhrecData,
    CancellationToken ct) -> Task<(ThemeAnalysis Analysis, SynergyAssessment Synergy)>

  private:
    AggregateThemeStrengths(deck, cardProfiles) -> Dictionary<string, decimal>
    ComputeSynergyScore(themeStrengths, onThemeCount, totalCount) -> decimal
    ApplyEdhrecBlend(themeScore, edhrecData, cardProfiles) -> decimal
    DetermineCommanderAlignment(commanderProfile, themeStrengths) -> CommanderAlignment
    BuildDeckThemes(themeStrengths, cardProfiles) -> IReadOnlyList<DeckTheme>
    CollectOffThemeCards(deck, cardProfiles, themeStrengths) -> IReadOnlyList<string>
```

---

## Infrastructure Layer

### Modified: `ParquetCardMetadataRow`

**File**: `src/CommandSynergy.Infrastructure/CardMetadata/ParquetCardMetadataStore.cs`

```
New column:
  ThemeSignals: Dictionary<string, decimal>?
    // Null for cards ingested before this feature (treated as empty at read time)
    // Populated by ThemeMatchingService during bulk import
```

Mapping:
- Write: `ThemeSignals = profile.ThemeSignals.Count > 0 ? new Dictionary<string,decimal>(profile.ThemeSignals) : null`
- Read: `ThemeSignals = row.ThemeSignals is { Count: > 0 } d ? d : ImmutableDictionary<string,decimal>.Empty`

### Modified: `CardMetadataRecord`

**File**: `src/CommandSynergy.Infrastructure/CardMetadata/ParquetCardMetadataStore.cs`

```
New property:
  ThemeSignals: IReadOnlyDictionary<string, decimal>?
```

---

### Modified: `ScryfallCardDocument`

**File**: `src/CommandSynergy.Infrastructure/Scryfall/ScryfallClient.cs`

```
New property:
  [JsonPropertyName("keywords")]
  Keywords: IReadOnlyList<string>   // default Array.Empty<string>()
```

---

### New: `EdhrecCommanderDocument`

**File**: `src/CommandSynergy.Infrastructure/Edhrec/EdhrecCommanderDocument.cs`

Deserialization model for `https://json.edhrec.com/pages/commanders/{slug}.json`:

```
sealed record EdhrecCommanderDocument
  Container: EdhrecContainer?

sealed record EdhrecContainer
  JsonDict: EdhrecJsonDict?

sealed record EdhrecJsonDict
  Cardlists: IReadOnlyList<EdhrecCardlist>

sealed record EdhrecCardlist
  Header: string
  Tag: string
  Cardviews: IReadOnlyList<EdhrecCardview>

sealed record EdhrecCardview
  Id: string           // Scryfall UUID
  Name: string
  Sanitized: string    // EDHREC slug
  Synergy: decimal     // [-1, 1]
  Inclusion: int
  NumDecks: int
  PotentialDecks: int
  TrendZscore: decimal
```

JSON property names use snake_case via `[JsonPropertyName]` attributes.

```
sealed record EdhrecCommanderData   // Application-layer flattened result
  CommanderSlug: string
  IsAvailable: bool
  CardSynergies: IReadOnlyDictionary<string, decimal>   // CardId -> synergy [-1,1]
  FetchedAtUtc: DateTimeOffset

  static Empty(slug): EdhrecCommanderData   // IsAvailable = false
```

---

### New: `EdhrecClient`

**File**: `src/CommandSynergy.Infrastructure/Edhrec/EdhrecClient.cs`

```
sealed class EdhrecClient
  private readonly HttpClient httpClient
  private readonly ILogger<EdhrecClient> logger
  private static readonly Regex SlugAllowlist = new Regex(@"^[a-z0-9][a-z0-9-]{1,80}[a-z0-9]$",
    RegexOptions.Compiled | RegexOptions.NonBacktracking)

  GetCommanderDataAsync(commanderSlug: string, ct: CancellationToken) -> Task<EdhrecCommanderData>
    // 1. Validate slug against SlugAllowlist — return Empty on failure (SSRF guard)
    // 2. GET {BaseUrl}pages/commanders/{slug}.json
    // 3. Deserialise EdhrecCommanderDocument
    // 4. Flatten all cardlists.cardviews into CardId -> synergy dict
    // 5. Return EdhrecCommanderData with IsAvailable = true
    // Error handling: timeout / HttpRequestException / JsonException -> log Warning, return Empty

  static string BuildSlug(cardName: string) -> string
    // Lowercase -> strip [^a-z0-9 -] -> trim -> replace ' ' with '-' -> collapse "--"
```

**HttpClient registration**: `HttpClientName = "Edhrec"`, base URL from `Edhrec:BaseUrl` configuration, 10 s timeout, `User-Agent: CommandSynergy/0.1`, `AddStandardResilienceHandler()` (reuses existing pattern from `ScryfallServiceCollectionExtensions`).

---

## Validation Rules

| Rule | Context | Behaviour on Violation |
|------|---------|----------------------|
| Deck must have >= 20 cards | ThemeAnalysisService.AnalyseAsync | Returns ThemeAnalysis with IsInsufficient = true; no themes computed |
| Commander slot must be flagged | Commander alignment | CommanderAlignment with Level = None |
| Card with no matching themes | ThemeMatchingService | Returns empty dictionary; card counted as off-theme |
| Missing card metadata | ThemeAnalysisService | Card skipped in theme aggregation; counted in off-theme |
| EDHREC fetch fails | ThemeAnalysisService | FinalScore = ThemeScore; no error surfaced to user (graceful degradation) |
| Invalid EDHREC slug | EdhrecClient | Returns EdhrecCommanderData.Empty immediately; no HTTP call |

---

## State Transitions

```
Deck workspace state:
  < 20 cards     -> ThemeAnalysis.IsInsufficient = true  -> UI: "Add more cards" placeholder
  >= 20 cards,
  no commander   -> CommanderAlignment.Level = None       -> UI: alignment section hidden
  >= 20 cards,
  commander set  -> full ThemeAnalysis + alignment        -> UI: ready state
  commander set,
  EDHREC loading -> ThemeAnalysis ready with ThemeScore   -> UI: score shows with "Enhancing..." indicator
  EDHREC loaded  -> ThemeAnalysis ready with FinalScore   -> UI: score updated silently
```

---

## Parquet Schema Delta

| Column | Type | Change | Notes |
|--------|------|--------|-------|
| `ThemeSignals` | `Dictionary<string, decimal>?` | ADD | Null for pre-feature rows; treated as empty at read time |

No columns removed. Backward compatible — existing Parquet files without `ThemeSignals` read cleanly with null for that column.
