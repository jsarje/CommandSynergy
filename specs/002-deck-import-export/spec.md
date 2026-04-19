# Feature Specification: Commander Deck Import And Export

**Feature Branch**: `[002-deck-import-export]`  
**Created**: 2026-04-19  
**Status**: Draft  
**Input**: User description: "lets extend the functionality so that users can import and export their existing commander decks. It should be possible to import/export in a variety of text based formats. There are a number of populate sites such as moxfield, manabox, etc so we should support known formats from those."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Import An Existing Decklist (Priority: P1)

As a Commander player, I want to import a decklist from a deckbuilding site or pasted text so that I
can continue working on an existing deck without re-entering every card manually.

**Why this priority**: Importing existing decks is the fastest path to immediate user value and makes
the current deck-building and analysis features useful for players who already manage decks
elsewhere.

**Independent Test**: Can be fully tested by importing supported text decklists from known sources
and confirming the commander, card counts, and deck sections are loaded into the workspace with any
problems clearly reported.

**Acceptance Scenarios**:

1. **Given** a user provides a decklist in a supported known text format, **When** the import is
   processed, **Then** the system creates a deck workspace containing the recognized commander and
   card entries in the correct deck sections.
2. **Given** a supported import contains lines that cannot be matched confidently, **When** the
   import completes, **Then** the system preserves all recognized cards and shows the unresolved
   lines with guidance on what needs correction.
3. **Given** an imported list is structurally valid text but produces a rules-illegal Commander
   deck, **When** the deck opens in the workspace, **Then** the system keeps the imported cards and
   shows the resulting validation issues instead of rejecting the import outright.

---

### User Story 2 - Export A Current Decklist (Priority: P2)

As a Commander player, I want to export my current deck into a format accepted by other deck tools
so that I can share it, archive it, or continue editing it in another application.

**Why this priority**: Export completes the portability loop and prevents the product from becoming a
dead end for users who move between tools.

**Independent Test**: Can be fully tested by opening an existing deck in the workspace, choosing a
supported target format, and confirming the resulting text can be copied or saved and reused in the
target tool without manual reformatting.

**Acceptance Scenarios**:

1. **Given** a user has a deck in the workspace, **When** they select a supported export format,
   **Then** the system generates text that follows that format's expected decklist structure.
2. **Given** the current deck contains information that a target format cannot represent,
   **When** the export is generated, **Then** the system tells the user what was omitted,
   flattened, or transformed.
3. **Given** a user exports a legal commander deck, **When** the output is reviewed, **Then** the
   commander and card quantities remain accurate in the exported text.

---

### User Story 3 - Use Portable Plain-Text Decklists (Priority: P3)

As a user who does not rely on a single deck site, I want a generic plain-text import and export
path so that I can move decks through copy-paste workflows even when a site-specific format is not
available.

**Why this priority**: Generic portability broadens coverage beyond named integrations and reduces
lock-in to a short list of supported sources.

**Independent Test**: Can be fully tested by importing a generic line-based decklist, editing it in
the workspace, and exporting it again in a portable text representation.

**Acceptance Scenarios**:

1. **Given** a user pastes a plain-text decklist that follows the application's supported generic
   structure, **When** the import runs, **Then** the system loads the deck without requiring a
   site-specific format.
2. **Given** a user wants to move a deck through clipboard-based workflows, **When** they export to
   the generic text format, **Then** the output remains readable and preserves the deck's essential
   structure.

### Edge Cases

- What happens when an imported list includes multiple commander-role cards such as partner pairs,
  backgrounds, companions, or format-specific commander sections?
- How does the system handle double-faced cards, alternate names, set annotations, collector
  numbers, or quantity markers that differ between supported sources?
- What happens when the imported text includes sideboards, maybeboards, category headers, notes, or
  custom tags that are not part of Commander legality?
- How does the experience behave when a user pastes malformed, oversized, duplicated, or partially
  unsupported text content?
- What happens when the export target cannot represent custom piles, analysis annotations, or other
  workspace-only metadata?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST allow users to import existing Commander decklists from text input in
  the initial supported format set for this release: Moxfield text decklists, ManaBox text
  decklists, and one generic portable plaintext format defined by this product. The system MAY add
  more formats in later releases, but unsupported formats in this release MUST be reported with
  clear guidance instead of being misidentified as supported input.
- **FR-002**: The system MUST identify the intended import format automatically when it is
  unambiguous and MUST allow the user to choose the format when the input could match more than one
  supported structure.
- **FR-003**: The system MUST parse and preserve recognized deck sections relevant to Commander deck
  exchange, including commander slots, main deck entries, and any additional sections carried by the
  source format that affect deck meaning.
- **FR-004**: The system MUST normalize imported card entries into the existing deck workspace so
  that validation, analysis, and editing continue to work immediately after import.
- **FR-005**: The system MUST keep successfully matched cards when an import is only partially
  successful and MUST show actionable diagnostics for unmatched, ambiguous, or unsupported lines.
- **FR-006**: The system MUST allow users to export the current deck into each supported target text
  format.
- **FR-007**: The system MUST generate export text that follows the chosen format's expected card
  ordering, quantity notation, and section labeling rules closely enough to be accepted without
  manual restructuring in normal use.
- **FR-008**: The system MUST warn users when a target export format cannot represent some deck or
  workspace information and MUST identify the affected information before the user copies or saves
  the result.
- **FR-009**: The system MUST preserve commander identity and card quantities across import and
  export operations for supported legal decklists.
- **FR-010**: The system MUST support round-trip workflows where a deck imported from a supported
  format can be exported again without unexpected card loss, except for limitations that were
  clearly disclosed to the user.
- **FR-011**: The system MUST treat import content as untrusted user input and MUST prevent it from
  executing scripts, markup, or other active content inside the product.

### Non-Functional Requirements

- **NFR-001**: The system MUST complete import or export of a standard Commander decklist under
  normal conditions within 3 seconds for at least 95% of acceptance-test runs.
- **NFR-002**: The system MUST define and present clear loading, empty, validation, partial-success,
  error, and recovery states for both import and export user flows.
- **NFR-003**: The system MUST enforce size limits, input sanitization, and safe handling of
  untrusted text so the feature does not expose new high-severity OWASP-relevant risks in input
  processing or file handling.
- **NFR-004**: The system MUST provide automated acceptance evidence for each supported format,
  including successful parsing or rendering fixtures, partial-failure diagnostics, and at least one
  round-trip verification path.

### Key Entities *(include if feature involves data)*

- **Deck Exchange Document**: A text representation of a commander deck in a supported import or
  export format.
- **Format Profile**: The business rules that describe how a specific supported decklist format
  structures commanders, card entries, sections, and quantities.
- **Import Result**: The outcome of processing an input decklist, including the normalized deck,
  detected format, matched cards, and any unresolved lines or warnings.
- **Export Result**: The generated text output for a chosen target format plus any warnings about
  omitted or transformed information.
- **Deck Section**: A logical grouping within a deck exchange document such as commanders, main
  deck, sideboard, maybeboard, or similar source-defined sections.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: At least 90% of users in acceptance testing can import a supported external decklist
  and open it in the workspace on their first attempt without manual re-entry.
- **SC-002**: At least 95% of the maintained sample decklists for each supported named format import
  with the correct commander assignment and total card quantities.
- **SC-003**: At least 95% of exported decklists in the supported named formats pass the maintained
  format-compatibility fixture suite and can be pasted into the documented target workflow for that
  format without manual reformatting.
- **SC-004**: When an import includes unsupported or ambiguous lines, at least 90% of users in
  acceptance testing can identify what failed and resolve the issue within 3 minutes.
- **SC-005**: No open high-severity findings remain for the delivered scope's relevant OWASP risk
  categories at release readiness review.

## Assumptions

- The first release focuses on text-based import and export only; direct account linking or live API
  synchronization with third-party deck sites is out of scope.
- The supported format catalog will cover a curated set of popular known sources, including
  Moxfield and ManaBox, and can expand over time without changing the user goal of deck
  portability.
- Existing deck validation and analysis workflows remain the source of truth after import, so an
  imported deck may still surface legality or analysis issues once loaded.
- Workspace-specific metadata such as custom piles, analysis explanations, or transient UI state may
  not be fully representable in every external text format.
- Users are exchanging one deck at a time through pasted text, uploaded text files, copied output,
  or equivalent text-based workflows.