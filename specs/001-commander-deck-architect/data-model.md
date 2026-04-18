# Data Model: Commander Synergy Sphere

## Deck

- **Purpose**: Aggregate root representing a user's active commander deck draft.
- **Fields**:
  - `DeckId`: unique identifier
  - `Name`: user-defined deck name
  - `CommanderCardId`: selected commander card identifier
  - `CompanionCardId`: optional companion card identifier
  - `Format`: constrained to Commander for this feature
  - `Entries`: collection of DeckEntry records
  - `PileDefinitions`: collection of named pile definitions
  - `ValidationStatus`: current legality summary
  - `BracketAssessment`: latest bracket result
  - `SynergyAssessment`: latest synergy result
  - `LastAnalyzedUtc`: timestamp of latest analysis
- **Relationships**:
  - Has many `DeckEntry`
  - Has many `Pile`
  - Has one `BracketAssessment`
  - Has one `SynergyAssessment`
- **Validation Rules**:
  - Must contain exactly one commander before a deck can be fully validated.
  - Must not exceed 100 total cards in the commander ruleset.
  - Companion is optional but must satisfy companion-specific deck constraints when present.
- **State Transitions**:
  - `Draft` -> `Invalid` when rules fail after mutation
  - `Draft` -> `Valid` when deck satisfies commander legality
  - `Valid` or `Invalid` -> `Analyzed` once bracket and synergy services complete

## DeckEntry

- **Purpose**: Represents a card included in a deck and its current workspace role.
- **Fields**:
  - `CardId`: referenced card identifier
  - `Quantity`: integer count
  - `AssignedPileId`: optional pile identifier
  - `IsCommander`: whether the entry is the commander
  - `IsCompanion`: whether the entry is the companion
  - `RoleTags`: derived or user-assigned deck role labels
  - `ValidationFlags`: legality findings tied to this card
  - `SaltScore`: optional social-friction marker displayed in UI
- **Relationships**:
  - Belongs to one `Deck`
  - References one `CardProfile`
  - May belong to one `Pile`
- **Validation Rules**:
  - Quantity is normally 1 under singleton rules unless card-specific exceptions apply.
  - Commander and companion flags are mutually exclusive.
  - Assigned pile must exist in the owning deck.

## CardProfile

- **Purpose**: Canonical card metadata used for search, validation, and rendering.
- **Fields**:
  - `CardId`: canonical identifier
  - `OracleId`: oracle grouping identifier
  - `Name`: display name
  - `ManaCost`: formatted mana cost
  - `ManaValue`: numeric mana value
  - `TypeLine`: type summary
  - `OracleText`: rules text
  - `ColorIdentity`: normalized color identity set
  - `ColorIndicator`: optional printed-color indicator
  - `Legalities`: relevant format legality information
  - `FaceProfiles`: collection of card faces for MDFCs and alternate-face cards
  - `ImageUris`: display image references
  - `SaltScore`: optional salt metric
  - `PlayRateByCommander`: optional commander-specific popularity metrics
  - `GenericColorStapleRate`: optional baseline staple usage metrics
  - `IsCommanderEligible`: whether the card may be selected as a commander under official
    Commander rules
  - `CommanderEligibilityBasis`: eligibility source such as default legendary creature,
    card-text override, official mechanic pairing, or not eligible
  - `MetadataSource`: whether the current local record originated from bulk snapshot import,
    user-driven Scryfall enrichment, or another server-side refresh path
  - `LastSyncedUtc`: timestamp of the latest local metadata refresh for the card
- **Relationships**:
  - May be referenced by many `DeckEntry` items
  - Has many `CardFaceProfile` items
- **Validation Rules**:
  - At least one face is required.
  - Color identity must be normalized for rule comparison.
  - Commander eligibility must only be true when official Commander rules or explicit exception
    text allow the card or pairing to serve as a commander.

## CardFaceProfile

- **Purpose**: Represents an individual face of an MDFC or other multi-face card.
- **Fields**:
  - `FaceId`: face identifier or index
  - `Name`: face name
  - `ManaCost`: face mana cost
  - `TypeLine`: face type line
  - `OracleText`: face rules text
  - `ImageUri`: face image reference
  - `IsPrimaryFace`: whether this is the default rendered face
- **Relationships**:
  - Belongs to one `CardProfile`
- **Validation Rules**:
  - A multi-face card must have at least two faces.

## Pile

- **Purpose**: User-defined organizational group in the workspace.
- **Fields**:
  - `PileId`: unique identifier
  - `Name`: visible label such as Ramp or Removal
  - `SortOrder`: UI order
  - `ColorHint`: optional display accent
  - `CardCount`: derived count for display
- **Relationships**:
  - Belongs to one `Deck`
  - May contain many `DeckEntry` items
- **Validation Rules**:
  - Pile names must be unique within a deck.

## ValidationFinding

- **Purpose**: Structured legality result produced by commander rules validation.
- **Fields**:
  - `FindingId`: unique identifier
  - `Severity`: error or warning
  - `Code`: normalized rule code
  - `Message`: user-readable description
  - `AffectedCardIds`: optional related card identifiers
  - `RuleArea`: deck size, singleton, color identity, companion, or face handling
- **Relationships**:
  - Belongs to one deck validation result

## BracketAssessment

- **Purpose**: Result of the 2026 bracket engine for the current deck state.
- **Fields**:
  - `BracketLevel`: integer from 1 to 5
  - `TotalWeight`: summed weighted score
  - `ContributingFactors`: collection of bracket factor details
  - `Summary`: user-readable explanation
  - `CalculatedUtc`: timestamp
- **Relationships**:
  - Belongs to one `Deck`
  - Has many `BracketFactor`
- **Validation Rules**:
  - Level must stay within the official 1-5 bracket range.

## BracketFactor

- **Purpose**: Single weighted contributor to the bracket result.
- **Fields**:
  - `SourceCardId`: optional related card
  - `Category`: game changer, acceleration, combo pressure, consistency, or other official rule bucket
  - `Weight`: numeric contribution
  - `Explanation`: user-readable reason
- **Relationships**:
  - Belongs to one `BracketAssessment`

## SynergyAssessment

- **Purpose**: Result of comparing deck card choices against commander-specific play rates and
  generic color staples.
- **Fields**:
  - `SynergyScore`: normalized score
  - `CommanderSpecificHits`: collection of positive synergy contributors
  - `StapleOverloadIndicators`: collection of cards identified as generic staples
  - `Summary`: user-readable interpretation
  - `CalculatedUtc`: timestamp
- **Relationships**:
  - Belongs to one `Deck`
- **Validation Rules**:
  - Score range must be normalized to a documented scale before it reaches the UI.

## SearchIndexSnapshot

- **Purpose**: Server-generated lightweight metadata package delivered to the client for instant
  search.
- **Fields**:
  - `Version`: snapshot version token
  - `GeneratedUtc`: timestamp
  - `CardSummaries`: compact searchable records
  - `SourceSnapshotId`: authoritative Parquet snapshot identifier
- **Relationships**:
  - Derived from many `CardProfile` records
- **Validation Rules**:
  - Must contain only fields approved for client-side search and rendering.

## LocalMetadataSnapshot

- **Purpose**: Server-owned Parquet-backed metadata store that accumulates authoritative local card
  records from curated imports and immediate Scryfall write-through enrichment.
- **Fields**:
  - `SnapshotId`: active snapshot identifier
  - `Cards`: collection of locally stored `CardProfile`-derived records
  - `LastUpdatedUtc`: latest mutation timestamp
  - `SourcePath`: filesystem path to the active Parquet snapshot
- **Relationships**:
  - Stores many `CardProfile`-derived records
  - Feeds one or more `SearchIndexSnapshot` generations
- **Validation Rules**:
  - Upserts must be deterministic by card identifier.
  - Snapshot writes must preserve existing cards unless a newer record intentionally replaces them.