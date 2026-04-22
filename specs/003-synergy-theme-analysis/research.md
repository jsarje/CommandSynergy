# Research: Synergy Scoring & Deck Theme Analysis

**Feature**: 003-synergy-theme-analysis  
**Date**: 2026-04-21  
**Status**: Complete — all NEEDS CLARIFICATION resolved

---

## 1. Scryfall `oracle_cards` Bulk Feed — Keywords Field

**Decision**: Add `Keywords: IReadOnlyList<string>` to `ScryfallCardDocument`.

**Rationale**: The Scryfall `oracle_cards` bulk JSON includes a `keywords` array per card (e.g., `["Flying", "Lifelink"]`). This field captures rules keywords that are not always present in oracle text (e.g., "Flying" may appear as a standalone keyword line rather than in the rules text body). Using both `keywords` and `oracle_text` for pattern matching gives higher recall. The field is mapped in the Scryfall document model and passed through to `CardProfile.ThemeSignals` computation at ingestion time.

**Alternatives considered**:
- Oracle text only: Lower recall for keyword-only effects. Rejected.
- Scryfall `produced_mana` / `color_indicator`: Not relevant to theme matching. N/A.

---

## 2. EDHREC JSON API Response Structure

**Decision**: Parse `container.json_dict.cardlists[*].cardviews[*]` for per-card synergy data.

**Rationale**: Live fetch of `https://json.edhrec.com/pages/commanders/krenko-mob-boss.json` confirmed the structure. The relevant fields per `cardview` are:
- `id` — Scryfall card UUID (matches `CardProfile.CardId`)
- `synergy` — float in range [−1, 1]; EDHREC's proprietary score (card's inclusion rate with this commander minus the card's baseline inclusion rate across all decks)
- `inclusion` / `num_decks` — deck count for this commander context
- `potential_decks` — denominator for inclusion rate calculation

The `taglinks` array on the top-level response also provides commander-level theme tags with deck counts (e.g., `{"slug":"goblins","value":"Goblins","count":8275}`), useful for future commander-theme enrichment but out of scope for this feature.

**Alternatives considered**:
- Parse only the "High Synergy Cards" cardlist (`tag: "highsynergycards"`): Misses cards in other lists (Top Cards, Creatures, etc.) that are also relevant for blending. Rejected — iterating all cardlists provides complete coverage.
- Use EDHREC's `inclusion` / `potential_decks` ratio directly: This is equivalent to raw play rate, not synergy. EDHREC's pre-computed `synergy` field already normalises against baseline, which is the correct signal. Accepted.

**EDHREC JSON skeleton** (confirmed):
```json
{
  "container": {
    "json_dict": {
      "cardlists": [
        {
          "header": "High Synergy Cards",
          "tag": "highsynergycards",
          "cardviews": [
            {
              "id": "5bac033c-dc4e-40a0-b103-4892e4b50249",
              "name": "Goblin Warchief",
              "sanitized": "goblin-warchief",
              "synergy": 0.72,
              "inclusion": 33711,
              "num_decks": 33711,
              "potential_decks": 38489,
              "trend_zscore": -0.21
            }
          ]
        }
      ]
    }
  }
}
```

---

## 3. EDHREC Commander Slug Generation

**Decision**: `slug = Regex.Replace(name.ToLowerInvariant(), @"[^a-z0-9\s-]", "").Trim().Replace(' ', '-')`

**Rationale**: Observed patterns across 10+ EDHREC commander pages:
- "Krenko, Mob Boss" → `krenko-mob-boss`
- "Atraxa, Praetors' Voice" → `atraxa-praetors-voice`
- "The Ur-Dragon" → `the-ur-dragon`
- "Thassa's Oracle" → `thasas-oracle` (apostrophe removed)
- "Korvold, Fae-Cursed King" → `korvold-fae-cursed-king`

The transformation is: lowercase → strip `[^a-z0-9 -]` → trim → spaces to hyphens → collapse `--` to `-`.

**SSRF mitigation**: After slug generation, validate against allowlist regex `^[a-z0-9][a-z0-9-]{1,80}[a-z0-9]$` before constructing the HTTP request URL. The base URL `https://json.edhrec.com/pages/commanders/` is pinned in `appsettings.json` under `Edhrec:BaseUrl`; user-supplied input never contributes to the URL.

---

## 4. Parquet.Net 5.2.0 — ThemeSignals Column

**Decision**: Store `ThemeSignals` as `Dictionary<string, decimal>?` using the same pattern as `PlayRateByCommander`.

**Rationale**: `Dictionary<string, decimal>?` is already serialised and deserialised correctly by Parquet.Net 5.2.0 in the existing `ParquetCardMetadataRow`. No additional packages or workarounds are needed. Theme names (dictionary keys) are short ASCII strings (max ~30 chars); values are `decimal` in [0, 1]. The column is nullable; cards with no theme matches will store `null`.

**Alternatives considered**:
- Serialising to a JSON string column (`string?`): Would require a custom deserialiser at read time. Rejected in favour of the native dictionary pattern already proven in the codebase.
- Separate Parquet file for theme signals: Unnecessary complexity; increases ingestion steps. Rejected.

---

## 5. Theme Taxonomy — ~35 Commander Themes

**Decision**: 34 themes defined statically in `ThemeTaxonomy` (~35 per spec); not user-configurable at runtime. Expanded from an initial 20-theme baseline following spec clarification (session 2026-04-21).

**Rationale**: The spec explicitly states "theme taxonomy is static at runtime — user-defined custom themes are out of scope." Themes were derived from:
1. EDHREC `taglinks` vocabulary (top tags across hundreds of commander pages)
2. MTG Comprehensive Rules keyword action list
3. Commander-specific community taxonomy (EDHREC theme pages, community guides)
4. Spec clarification — oracle/keyword-detectable themes only; meta-labels (Budget, Casual, cEDH, Vorthos) and 1v1-format archetypes (Tron, Delver, Stoneblade) excluded

The taxonomy covers the most common Commander archetypes and aligns with EDHREC's own tag vocabulary, which aids consistency when blending EDHREC synergy signals. Niche mechanics (Cycling, Cascade, Energy, Morph, Mutate) are deferred to a future iteration.

**Canonical theme list** (34 themes):

| # | Theme Name | Detection Approach | Notes |
|---|-----------|-------------------|-------|
| 1 | Ramp | Keywords: Landfall, oracle: "search your library for a land", "add {", "additional land" | High recall via oracle patterns |
| 2 | Card Draw / Wheels | Keywords: Draw, oracle: "draw a card", "draw cards", "each player draws", "wheel" | "Wheels" was a prior coverage gap; now included |
| 3 | Spellslinger | Oracle: "instant or sorcery", "cast an instant", "copy target instant", type: Instant/Sorcery density | |
| 4 | Combo | Oracle: tutor density ("search your library"), Storm keyword, "untap target permanent", "take an extra turn", "exile target player's library" — **lower-confidence proxy signals; documented in `ThemeDefinition`** | See Section 5a below |
| 5 | Control / Stax | Keywords: Flash, oracle: "counter target spell", "destroy target", "exile target", "tap all" | Stax was a prior coverage gap; now included |
| 6 | Pillowfort | Oracle: "can't attack you", "must attack", "you don't lose the game", "prevent all combat damage" | New addition |
| 7 | Tribal | Oracle/type: "tribal" OR "kindred" (dual detection — see Section 5b); type: creature subtype synergy | |
| 8 | Tokens | Oracle: "create a", "token", "populate" | New addition |
| 9 | Voltron | Keywords: Equip, oracle: "attach to", "equip", "commander deals combat damage" | New addition |
| 10 | Aggro | Keywords: Haste, First Strike, oracle: "deal combat damage", "attack each combat" | |
| 11 | Enchantments | Keywords: Enchant, oracle: "enchantment you control", type: Enchantment | New addition |
| 12 | Artifacts | Oracle: "artifact you control", "artifact enters", type: Artifact density | New addition |
| 13 | Equipment | Keywords: Equip, oracle: "equipment you control", type: Equipment | New addition |
| 14 | Auras | Keywords: Enchant creature, oracle: "aura", "enchanted creature" | New addition |
| 15 | +1/+1 Counters | Keywords: Proliferate, Bolster, oracle: "+1/+1 counter" | |
| 16 | Counters Matter | Oracle: "counter on it", "remove a counter", "counter of any kind" | New addition — broader than +1/+1 |
| 17 | Reanimator | Oracle: "return target creature card from your graveyard", "put onto the battlefield from a graveyard" | Graveyard split — see Section 5c |
| 18 | Aristocrats | Keywords: Sacrifice, oracle: "sacrifice a creature", "whenever a creature dies", "loses life" | Graveyard split — see Section 5c |
| 19 | Self-Mill / Dredge | Keywords: Dredge, oracle: "mill", "put the top … into your graveyard", "fills graveyard" | Graveyard split — see Section 5c |
| 20 | Blink / Flicker | Oracle: "exile … return it", "flicker", "leaves the battlefield and returns" | New addition |
| 21 | Life Gain | Oracle: "you gain life", "gain life equal", keywords: Lifelink density | New addition |
| 22 | Mill | Oracle: "mill", "put … into target player's graveyard from top of library" | New addition |
| 23 | Lands Matter | Keywords: Landfall, oracle: "land enters", "land you control", "basic land" effects | New addition |
| 24 | Land Destruction | Oracle: "destroy target land", "return target land", "sacrifice a land" | |
| 25 | Extra Turns | Oracle: "take an extra turn", "additional turn" | |
| 26 | Extra Combats | Oracle: "additional combat phase", "untap all creatures", "attacks again" | |
| 27 | Infect / Poison | Keywords: Infect, Poisonous, Toxic, oracle: "poison counter" | |
| 28 | Group Hug / Group Slug | Oracle: "each player draws", "each player gains", "each opponent loses" | |
| 29 | Superfriends | Type: Planeswalker density, oracle: "loyalty counter", "planeswalker you control" | |
| 30 | Sacrifice | Oracle: "sacrifice a permanent", "whenever you sacrifice" | |
| 31 | Discard | Oracle: "discard a card", "discard your hand", "each player discards" | |
| 32 | Burn | Oracle: "deals damage to any target", "deals … damage to each opponent", type: Instant/Sorcery + damage | |
| 33 | Hatebears | Oracle: "can't be countered", "players can't", "opponents can't", type: Creature with static denial | New addition; Hatebears was a prior coverage gap |
| 34 | Goodstuff | **No oracle signals** — computed as residual: cards with no other theme signal ≥ 0.10 and converted set-legal rarity (Rare/Mythic) / high power proxy | Lowest confidence; last-resort label |

**Section 5a — Combo proxy signals (lower confidence)**  
"Combo" cannot be reliably detected from individual card text alone. The following proxy signals are used and documented in `ThemeDefinition.SignalConfidence = Low`:
- Tutor / "search your library" density ≥ 4 cards in deck
- Storm keyword present
- Untap-engine patterns: "untap target permanent", "untap all creatures"
- Win-condition patterns: "exile target player's library", "player loses the game"

Signal confidence is documented as `Low` in `ThemeDefinition` and surfaced in the UI alongside the theme name.

**Section 5b — Tribal / Kindred dual detection**  
Display name: **"Tribal"** — matches EDHREC URL slugs and overwhelming player vocabulary.  
Detection covers both oracle text eras:
- Pre-2023 cards: `\btribal\b` in oracle text and type line
- Post-2023 cards (rules update): `\bkindred\b` in oracle text and type line  
Both patterns are combined with `|` in the compiled Regex.

**Section 5c — Graveyard strategy split (three themes)**  
Per spec clarification, graveyard strategies are three separate themes with distinct signal patterns:
- **Reanimator**: Returns permanents from graveyard to battlefield. Key oracle patterns: "return … from your graveyard … to the battlefield", "put onto the battlefield from a graveyard"
- **Aristocrats**: Sacrifice-loop life drain. Key oracle patterns: "sacrifice a creature", "whenever a creature dies", "loses life"; keyword: Sacrifice
- **Self-Mill / Dredge**: Fills own graveyard as resource. Key patterns: "mill N", "put top N … into graveyard"; keyword: Dredge

These three themes may overlap on individual cards (e.g., a Dredge creature that also sacrifices) — cards contribute to all matching themes simultaneously.

---

## 6. Synergy Score Blending — 70/30 Split

**Decision**: `final_score = 0.7 × theme_score + 0.3 × edhrec_avg`

**Rationale**:
- Theme score is derived from local, auditable oracle text patterns. It is deterministic and always available.
- EDHREC synergy is crowd-sourced and reflects real-world play data, which is a high-quality signal but dependent on an external API.
- A 70/30 split ensures the local analysis is authoritative while EDHREC nudges scores for commanders with rich play data (e.g., popular commanders like Krenko have thousands of data points; obscure commanders may have few).
- When EDHREC data is unavailable (no commander selected, API timeout, invalid slug), `final_score = theme_score` with no degradation in UX.

**Alternatives considered**:
- 50/50 split: Gives too much weight to EDHREC, creating a hard dependency and reducing discriminatory power for niche commanders. Rejected.
- 90/10 split: EDHREC contribution too small to be meaningful. Rejected.

---

## 7. EDHREC Caching Strategy

**Decision**: `IMemoryCache` keyed by commander slug, TTL = 15 minutes, backed by the existing `DeckAnalysisCache` infrastructure pattern.

**Rationale**: EDHREC data changes slowly (deck statistics update daily at most). A 15-minute TTL matches the existing analysis cache duration and prevents hammering the external API during a deckbuilding session. The cache is scoped to the server process; no persistence needed.

**Error handling**:
- HTTP timeout (10 s) → log Warning, return empty `EdhrecCommanderData` with `IsAvailable = false`
- HTTP 4xx/5xx → log Warning, return empty (no retry; assume commander not found on EDHREC)
- Invalid slug (fails allowlist) → log Warning, return empty immediately (no HTTP call made)
- JSON parse error → log Warning, return empty

---

## 8. Regex Strategy — NonBacktracking Engine

**Decision**: Use `RegexOptions.Compiled | RegexOptions.NonBacktracking | RegexOptions.IgnoreCase` for all oracle text pattern matching.

**Rationale**: .NET 7+ introduced the `NonBacktracking` engine (`RE2`-style). For oracle text matching, patterns are simple phrase patterns (no catastrophic backtracking possible with standard patterns), but using `NonBacktracking` provides O(n) time guarantees. Cards with unusually long oracle text (e.g., Sagas, modal DFC) are protected. Patterns are compiled once at static construction of `ThemeTaxonomy` and reused across all card analyses.

---

## 9. UI Component Strategy

**Decision**: New `ThemeAnalysisPanel.razor` as a standalone MudBlazor component, co-located with the existing `AnalysisPanel`.

**Rationale**: Keeps theme analysis concerns separate from bracket analysis. The existing `AnalysisPanel` shows bracket level + basic synergy; `ThemeAnalysisPanel` adds ranked theme chips, commander alignment indicator, synergy score bar, and off-theme card list. Both panels can be displayed in the same workspace layout. State management follows the existing `IsLoading → HasError → IsEmpty → Ready` pattern.

---

## 10. No Keyword Data in Scryfall Face Documents

**Finding**: Multi-face cards (`ScryfallCardFaceDocument`) do not have a `keywords` field in Scryfall's API — keywords are on the root `ScryfallCardDocument` only.

**Decision**: When computing theme signals for multi-face cards, use the root `Keywords` and concatenate oracle text from all faces. The `ScryfallCardMapper` already concatenates face oracle text patterns; the same approach applies for keywords.
