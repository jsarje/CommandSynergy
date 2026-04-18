# Feature Specification: Commander Synergy Sphere

**Feature Branch**: `[001-build-commander-architect]`  
**Created**: 2026-04-17  
**Status**: Draft  
**Input**: User description: "Create a commander deck architect that validates deck legality,
analyzes bracket and synergy, and provides an interactive workspace for organizing and inspecting
cards."

## Clarifications

### Session 2026-04-18

- Q: Should bracket and Game Changer constraints be enforced as legality rules or treated as non-blocking analysis guidance? → A: Treat them as non-blocking analysis guidance; legal deck validation remains based on official Commander deck-construction rules.
- Q: What cards should be eligible for commander selection? → A: Allow legendary creatures by default, plus only cards or pairings explicitly permitted by printed Commander text or official Commander mechanics.
- Q: How should user-driven Scryfall metadata results populate the local Parquet snapshot? → A: Persist each successfully resolved Scryfall card immediately into the local Parquet snapshot using id-based upsert behavior.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Build A Legal Commander Deck (Priority: P1)

As a Commander player, I want to assemble a 100-card deck around a chosen commander so that I can
confidently produce a rules-legal list without manually checking every deckbuilding rule.

**Why this priority**: Legal deck construction is the core product value. Without this journey, the
application is only a card browser instead of a deck architect.

**Independent Test**: Can be fully tested by selecting a commander, adding cards, organizing them
into piles, and confirming the system identifies both valid and invalid deck states with clear rule
feedback.

**Acceptance Scenarios**:

1. **Given** a user has chosen a commander, **When** they add cards that break singleton, deck size,
   or color identity rules, **Then** the system identifies each violation and explains what must be
   corrected.
2. **Given** a user has assembled a 100-card deck that satisfies commander legality rules,
   **When** validation runs, **Then** the system confirms the deck is legal and shows a summary of
   deck composition by custom pile.
3. **Given** a card has modal or alternate faces or companion-specific restrictions, **When** the
   card is added or inspected, **Then** the system validates it using the correct commander rules and
   makes both relevant faces available to the user.

---

### User Story 2 - Analyze Power And Synergy (Priority: P2)

As a deck builder, I want bracket, power, and synergy feedback so that I can understand how my deck
fits the 2026 commander environment and where it overperforms or underperforms.

**Why this priority**: Analysis turns a legal deck into a useful strategic tool and differentiates
the product from simpler decklist builders.

**Independent Test**: Can be fully tested by loading a deck with known high-impact cards and staple
patterns, then verifying bracket output, supporting factors, and synergy scoring update after deck
changes.

**Acceptance Scenarios**:

1. **Given** a completed or partially completed deck, **When** analysis runs, **Then** the system
   returns a commander bracket from 1 to 5 with a transparent explanation of the main contributing
   factors.
2. **Given** a deck includes cards from the weighted high-impact list, **When** the analysis is
   recalculated, **Then** the reported bracket and power rationale reflect those inclusions.
3. **Given** a deck contains cards that are highly played with the selected commander or unusually
   generic for its color identity, **When** synergy is calculated, **Then** the system surfaces a
   synergy score and highlights whether cards are commander-specific fits or broad staples.

---

### User Story 3 - Organize And Inspect Cards In An Interactive Workspace (Priority: P3)

As a user refining a deck, I want a drag-and-drop workspace with rich card inspection so that I can
sort responsibilities, understand MDFCs, and quickly spot high-salt cards without leaving the main
view.

**Why this priority**: The visual workspace improves usability and supports faster iteration, but it
depends on the deck-building and analysis foundations being in place first.

**Independent Test**: Can be fully tested by moving cards between piles, viewing alternate card
faces, and confirming the workspace preserves card meaning and analysis context during interaction.

**Acceptance Scenarios**:

1. **Given** a user is reviewing a deck, **When** they drag cards between named piles such as Ramp,
   Draw, Removal, or Board Wipes, **Then** the workspace updates immediately and retains the
   assigned pile grouping.
2. **Given** a card has more than one playable face, **When** the user flips or inspects the card,
   **Then** the alternate face is revealed without losing the card's position in the workspace.
3. **Given** a card carries a salt indicator, **When** the user scans the deck workspace, **Then**
   the card's relative social risk is visible at a glance.

### Edge Cases

- What happens when a user adds an off-color land, split card, or MDFC whose alternate face changes
  legality interpretation?
- How does the system handle companion restrictions that are legal only if the full deck satisfies
  an additional rule set?
- How does the experience behave during loading, empty, validation, and recovery states when the
  local card index or external card provider is temporarily unavailable?
- What happens when bracket calculation or synergy scoring cannot evaluate a card because metadata,
  play-rate inputs, or weighting rules are missing or stale?
- How does the workspace behave when the same card is moved rapidly between piles, or when a user
  is working on a smaller touch device with drag-and-drop interactions?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST allow a user to choose a commander, search the supported card pool,
  add cards to a deck, and remove cards from a deck.
- **FR-002**: The system MUST enforce commander deck legality rules, including 100-card deck size,
  singleton restrictions where applicable, color identity validation, and companion constraints.
- **FR-002a**: The system MUST limit legality validation to official Commander deck-construction and
  card-legality rules; bracket expectations, Game Changer allowances, and pregame power matching are
  reported separately and MUST NOT invalidate an otherwise legal deck.
- **FR-002b**: The system MUST restrict commander selection to cards that are legal commanders under
  official Commander rules: legendary creatures by default, plus only cards or pairings whose
  printed text or official Commander mechanics explicitly allow them to be used as commanders.
- **FR-003**: The system MUST support modal, alternate-face, and companion-relevant cards during
  both deck validation and card inspection.
- **FR-004**: The system MUST allow users to group cards into named custom piles and move cards
  between those piles from the main workspace.
- **FR-005**: The system MUST calculate and display a commander bracket from 1 to 5 using the 2026
  official bracket framework and a weighted list of high-impact cards.
- **FR-006**: The system MUST calculate and display a synergy score that compares commander-specific
  play patterns against broadly used color staples.
- **FR-007**: The system MUST explain bracket and synergy outcomes with user-readable contributing
  factors rather than returning only raw scores.
- **FR-008**: The system MUST display card imagery and card details sufficient for deck-building,
  including alternate face viewing for applicable cards and a visible salt indicator.
- **FR-009**: The system MUST provide a deck workspace that keeps deck validation, pile assignment,
  and analysis feedback in sync after each relevant card action.
- **FR-010**: The system MUST handle unavailable or incomplete card metadata gracefully by showing
  the affected card state, preserving user work, and identifying what could not be evaluated.
- **FR-010a**: The system MUST build local card metadata over time by upserting successful
  Scryfall-resolved card results into the local Parquet snapshot during normal user interactions so
  subsequent searches, validation, and analysis can prefer local metadata and reduce repeat
  external calls.
- **FR-011**: The system MUST reserve an explicit extension point for future deck-advice features
  without requiring the first release to deliver full AI-driven recommendations.

### Non-Functional Requirements

- **NFR-001**: The system MUST define measurable performance expectations for card search, deck
  validation, drag-and-drop updates, and bracket or synergy recalculation.
- **NFR-002**: The system MUST define required UX behavior for loading, empty, validation, error,
  and recovery states across deck building, analysis, and card inspection flows.
- **NFR-003**: The system MUST define applicable security controls for input handling, dependency
  trust, external data access, data protection, and audit visibility based on relevant OWASP Top 10
  risks.
- **NFR-004**: The system MUST identify the automated test evidence required to accept rules
  validation, analysis, and interactive workspace behavior.

### Key Entities *(include if feature involves data)*

- **Deck**: A user-assembled commander list containing a selected commander, card entries, pile
  assignments, legality state, bracket outcome, and synergy outcome.
- **Card Profile**: A playable card record with identity attributes, face data, legality-relevant
  characteristics, salt indicator, and presentation metadata.
- **Deck Entry**: A card's inclusion in a deck, including quantity, assigned pile, role in
  validation, and score contributions.
- **Bracket Assessment**: The scored result that maps a deck to bracket 1 through 5, with weighted
  contributing factors and explanatory findings.
- **Synergy Assessment**: The scored result that compares deck card usage against commander-specific
  play tendencies and general color staples.
- **Pile**: A named organizational bucket within the workspace that groups cards by purpose such as
  Ramp, Draw, Removal, or Board Wipes.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can build and validate a complete 100-card commander deck, including pile
  organization, in under 10 minutes during acceptance testing.
- **SC-002**: In at least 95% of test runs, users receive updated legality, bracket, and synergy
  feedback within 2 seconds of adding, removing, or moving a card.
- **SC-003**: At least 90% of sampled invalid deck scenarios identify the correct rule violation on
  first display without requiring manual rule lookup.
- **SC-004**: At least 90% of test users can inspect alternate card faces and identify salt-marked
  cards without leaving the main workspace.
- **SC-005**: No open high-severity findings remain for the delivered scope's relevant OWASP risk
  categories at release readiness review.

## Assumptions

- Users build one commander deck at a time in the initial release; collaboration, multiplayer
  syncing, and publishing are out of scope.
- The product can rely on an authoritative external card source and a locally available searchable
  metadata index for supported card information.
- User-driven Scryfall lookups are allowed to enrich the local Parquet metadata snapshot
  incrementally through immediate id-based upserts rather than requiring a separate manual import
  workflow.
- The official commander bracket definition and weighted high-impact list are maintained as updateable
  inputs rather than hard-coded business assumptions.
- Commander brackets and Game Changers are treated as optional matchmaking and analysis guidance,
  not as legality gates for deck submission or correction.
- Commander eligibility is based on official Commander rules and exception text, so generic
  artifacts, enchantments, or other noncreature permanents are not selectable as commanders unless
  a card or official Commander mechanic explicitly says otherwise.
- AI-driven deck recommendations are out of scope for the first release beyond reserving a clear
  service boundary and visible entry point for future guidance.
- Existing infrastructure can support the stated responsiveness targets for search, validation, and
  deck analysis under normal single-user usage.