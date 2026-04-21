# Feature Specification: Synergy Scoring & Deck Theme Analysis

**Feature Branch**: `003-synergy-theme-analysis`
**Created**: 2026-04-21
**Status**: Draft
**Input**: User description: "we want to expand the synergy scoring, and determining the theme of the deck list. Deck lists are analysed and the core themes of the decks identified. Synergy score is determined by analysing whether the deck list is just a randomly assembled set of cards or whether they interact and synegise together well."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Identify Deck Themes (Priority: P1)

A deckbuilder submits their Commander deck list and sees a ranked list of the deck's primary strategic themes — for example "Graveyard Recursion (Strong)", "+1/+1 Counters (Moderate)", "Ramp (Supporting)" — so they understand what the deck is actually trying to do and where its focus lies.

**Why this priority**: Theme identification is the foundation of this feature. It delivers immediate, actionable insight and is a prerequisite for meaningful synergy scoring.

**Independent Test**: Can be fully tested by submitting a known theme-focused deck (e.g., a graveyard-focused deck) and verifying the top identified theme is graveyard-related.

**Acceptance Scenarios**:

1. **Given** a deck list with 60%+ cards supporting graveyard mechanics, **When** theme analysis runs, **Then** "Graveyard Recursion" appears as the highest-ranked theme with a strength rating above 50%
2. **Given** a deck list with no dominant theme, **When** theme analysis runs, **Then** no single theme exceeds 30% strength and the result is labelled "No clear theme detected"
3. **Given** a deck with only 1–5 cards entered, **When** theme analysis runs, **Then** the panel shows an "Add more cards to see theme analysis" placeholder state
4. **Given** a deck list is updated (card added or removed), **When** the change is saved, **Then** theme analysis refreshes automatically

---

### User Story 2 - View Synergy Score (Priority: P1)

A deckbuilder views a synergy score (0–100) for their deck, accompanied by a qualitative label and brief explanation, so they can immediately understand how coherent and internally consistent their deck is.

**Why this priority**: The synergy score is the core deliverable of this feature — it answers "is this deck just a pile, or does it work as a unit?"

**Independent Test**: Can be fully tested by submitting a curated synergy-focused deck vs. a random card pile and confirming the focused deck scores significantly higher.

**Acceptance Scenarios**:

1. **Given** a 100-card deck where 80%+ of cards contribute to at least one identified theme, **When** the synergy score is computed, **Then** the score is 70 or above and the label reads "Focused" or better
2. **Given** a 100-card deck assembled from unrelated cards across many weak themes, **When** the synergy score is computed, **Then** the score is below 40 and the label reads "Unfocused" or "Low coherence"
3. **Given** the deck list changes significantly, **When** cards are added or removed, **Then** the synergy score updates to reflect the new composition
4. **Given** an empty deck or deck under 20 cards, **When** the synergy score panel is shown, **Then** the score is not displayed and a prompt to add more cards is shown instead

---

### User Story 3 - Explore Cards by Theme (Priority: P2)

A deckbuilder can expand each identified theme to see which cards in the deck contribute to it, so they can make informed decisions about which cards to keep, cut, or replace.

**Why this priority**: Theme grouping turns abstract scores into actionable decisions. Without it, the analysis gives a verdict but no guidance.

**Independent Test**: Can be fully tested by selecting a theme and verifying all listed cards contain keywords or oracle text aligned with that theme.

**Acceptance Scenarios**:

1. **Given** theme analysis has run, **When** a user selects a theme, **Then** a list of cards contributing to that theme is shown, including the card name and a brief reason why it contributes
2. **Given** a card contributes to multiple themes, **When** themes are expanded, **Then** that card appears under each applicable theme
3. **Given** a card contributes to no identified theme, **When** the user views the deck, **Then** that card is surfaced in an "Off-theme" group

---

### User Story 4 - Commander-Anchored Alignment (Priority: P2)

The system weights the commander card when determining primary theme direction and surfaces how well the 99-card deck aligns with the commander's strategic identity, so the user can evaluate whether their 99 actually supports what their commander wants to do.

**Why this priority**: In Commander, the commander defines the deck's identity — a synergy score that ignores this misses the format's core constraint.

**Independent Test**: Can be fully tested by loading a deck whose 99 contradicts the commander's theme and confirming the alignment warning is surfaced.

**Acceptance Scenarios**:

1. **Given** a commander whose primary theme is "token generation" and a 99 with <20% token-support cards, **When** analysis runs, **Then** a "Commander alignment: Low" indicator is shown
2. **Given** a commander whose primary theme matches the deck's top theme, **When** analysis runs, **Then** a "Commander alignment: Strong" indicator is shown
3. **Given** a deck list with no commander, **When** analysis runs, **Then** commander alignment analysis is skipped without error and the remaining analysis displays normally

---

### Edge Cases

- What happens when a card has no oracle text or keyword data in the metadata store? → The card is treated as contributing to no theme and counted in "Off-theme".
- How does the system handle cards that span multiple strategies (e.g., a card that generates tokens AND has graveyard recursion)? → The card contributes to all matching themes simultaneously.
- How does the experience behave during analysis computation? → A loading state is shown; previously computed results remain visible with a staleness indicator until the update completes.
- What happens if card metadata is unavailable for a card in the list? → The card is treated as "Off-theme" with an indicator that data is unavailable.
- What happens when the deck has fewer than 20 cards? → Theme and synergy panels show a "not enough cards" placeholder; no score is computed.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST identify and rank the primary strategic themes present in a deck list, drawn from a defined theme taxonomy, based on card keywords, types, and oracle text
- **FR-002**: System MUST compute a synergy score from 0 to 100 representing the deck's thematic coherence, where higher scores indicate a more focused, internally consistent deck
- **FR-003**: System MUST present a qualitative label alongside the synergy score (e.g., "Pile", "Unfocused", "Developing", "Focused", "Tuned") to provide immediate human-readable context
- **FR-004**: System MUST display the top identified themes ranked by strength, each with a relative strength indicator showing how dominant that theme is in the deck
- **FR-005**: System MUST allow users to expand a theme to see the cards contributing to it, including a brief explanation of each card's contribution
- **FR-006**: System MUST surface cards that do not contribute to any identified theme in an "Off-theme" group
- **FR-007**: The commander card MUST be weighted more heavily than non-commander cards when determining the primary theme direction
- **FR-008**: System MUST compute and display a "Commander alignment" indicator reflecting how well the 99-card deck supports the commander's strategic identity (Low / Moderate / Strong)
- **FR-009**: System MUST automatically recompute theme analysis and synergy score when the deck list changes
- **FR-010**: System MUST suppress theme analysis and synergy score display when fewer than 20 cards are present, replacing them with a prompt to add more cards

### Non-Functional Requirements

- **NFR-001**: Theme analysis and synergy score computation for a complete 100-card Commander deck MUST complete within 3 seconds; for incremental updates triggered by a single card change, within 1 second
- **NFR-002**: System MUST display distinct states for: loading (computing), empty (insufficient cards), error (analysis failed), and ready (results available); all states MUST be accessible with appropriate ARIA roles and labels
- **NFR-003**: Theme matching MUST read card names and oracle text exclusively from the trusted card metadata store — no user-supplied free-text is evaluated as executable logic; no external API calls are triggered at runtime by this feature
- **NFR-004**: Automated test evidence MUST include: unit tests for theme-matching logic against known cards, unit tests for synergy score calculation covering focused and unfocused deck configurations, component tests for all display states (loading, empty, error, ready), and at least one integration test validating end-to-end analysis of a known deck configuration

### Key Entities *(include if feature involves data)*

- **DeckTheme**: A named strategic pattern identified in the deck — theme name, strength (0–100), list of contributing card IDs, human-readable description
- **SynergyScore**: Aggregate coherence measure — numeric score (0–100), qualitative label, breakdown by contributing theme
- **CommanderAlignment**: Indicator of how well the 99-card portion supports the commander — alignment level (Low / Moderate / Strong), list of primary evidence cards
- **ThemeAnalysis**: Composite result containing: ranked list of DeckThemes, SynergyScore, CommanderAlignment (if commander present), list of off-theme card IDs, analysis timestamp

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Theme analysis for a complete 100-card Commander deck is visible to the user within 3 seconds of deck submission or update
- **SC-002**: A known theme-focused deck scores at least 30 points higher than an equal-length random card pile in synergy score, validating discriminatory power
- **SC-003**: Users can identify their deck's top 3 themes and the cards driving them without any additional navigation or documentation
- **SC-004**: Users surfaced with an "Off-theme" list can identify at least one card to cut without needing external guidance
- **SC-005**: No high-severity OWASP-relevant findings remain open for the delivered scope

## Assumptions

- Card metadata (keywords, oracle text, types, subtypes) is available in the system via the Parquet-backed card store established in feature 001
- This feature builds on the existing deck analysis infrastructure (AnalysisPanel, analysis result types) introduced in feature 001
- A theme taxonomy of approximately 15–25 named Commander themes (e.g., "Graveyard Recursion", "+1/+1 Counters", "Token Generation", "Ramp", "Draw/Wheel", "Sacrifice", "Tribal", "Voltron", "Control", "Combo") will be defined as part of this feature's implementation
- Theme taxonomy is static at runtime — user-defined custom themes are out of scope for this version
- The synergy score algorithm weights thematic concentration: decks with few strong themes score higher than decks with many weak themes
- Performance targets assume card metadata is loaded in memory at analysis time — no Parquet file reads occur during per-request analysis
- Mobile responsiveness relies on the existing MudBlazor layout infrastructure; no dedicated mobile-first design work is in scope
