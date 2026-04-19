# Data Model: Commander Deck Import And Export

## ImportedDeckLibraryDocument

- **Purpose**: Versioned browser-local persistence root for all imported decks owned by a user in
  this browser.
- **Fields**:
  - `SchemaVersion`: storage schema version for migrations
  - `ActiveDeckId`: identifier of the currently selected imported deck
  - `Decks`: collection of `ImportedDeckRecord` items
  - `LastSavedUtc`: timestamp of the most recent successful persistence
- **Relationships**:
  - Has many `ImportedDeckRecord`
- **Validation Rules**:
  - `SchemaVersion` must match a supported migration path.
  - `ActiveDeckId` must be null or reference an existing deck in `Decks`.

## ImportedDeckRecord

- **Purpose**: A single imported Commander deck persisted locally for later switching, editing, and
  export.
- **Fields**:
  - `ImportedDeckId`: stable client-generated identifier
  - `Name`: display name resolved from source text or user override
  - `SourceFormatId`: format profile that produced the normalized deck
  - `ImportedAtUtc`: original import timestamp
  - `LastOpenedUtc`: latest time the user activated the deck in the app
  - `OriginalDocumentText`: raw imported text retained client-side only
  - `NormalizedDeck`: normalized `PortableDeckSnapshot`
  - `Diagnostics`: collection of `ImportDiagnostic`
  - `ExportWarnings`: latest warnings generated for this deck's export preview
  - `SourceMetadata`: optional source-specific tags such as site name or declared sections
- **Relationships**:
  - Belongs to one `ImportedDeckLibraryDocument`
  - Has one `PortableDeckSnapshot`
  - Has many `ImportDiagnostic`
- **Validation Rules**:
  - `ImportedDeckId` must be unique within the library.
  - `OriginalDocumentText` must stay within the configured safe size limit.
  - `SourceFormatId` must reference a supported `FormatProfile`.

## PortableDeckSnapshot

- **Purpose**: Client-side normalized representation of an imported deck that can be rendered,
  switched, exported, and optionally projected into an explicit working copy.
- **Fields**:
  - `DeckName`: normalized deck name
  - `CommanderCardIds`: one or more commander-role card identifiers
  - `CompanionCardId`: optional companion card identifier
  - `Entries`: collection of `PortableDeckEntry`
  - `Sections`: collection of `DeckSectionState`
  - `ImportedCardCount`: total card quantity imported
  - `HasUnresolvedLines`: whether import diagnostics remain unresolved
  - `DerivedWorkspaceSnapshot`: optional projection to the existing deck workspace contract shape
- **Relationships**:
  - Has many `PortableDeckEntry`
  - Has many `DeckSectionState`
- **Validation Rules**:
  - Must preserve recognized commander-role entries separately from generic main-deck entries.
  - `ImportedCardCount` must equal the summed quantities of `Entries`.
  - `DerivedWorkspaceSnapshot` must be absent unless the user explicitly opens a working copy for
    server-backed analysis.

## PortableDeckEntry

- **Purpose**: Normalized card row within an imported or exported deck snapshot.
- **Fields**:
  - `CardId`: resolved canonical card identifier when known
  - `OriginalCardText`: original card text from the source line
  - `DisplayName`: normalized card name used in the UI
  - `Quantity`: parsed quantity
  - `SectionId`: logical section assignment such as commander, mainboard, sideboard, or maybeboard
  - `IsCommander`: whether the entry fills a commander slot
  - `IsCompanion`: whether the entry is a companion
  - `ParseConfidence`: exact, normalized, ambiguous, or unresolved
- **Relationships**:
  - Belongs to one `PortableDeckSnapshot`
- **Validation Rules**:
  - `Quantity` must be at least 1 for resolved entries.
  - `SectionId` must reference a known `DeckSectionState`.

## DeckSectionState

- **Purpose**: Preserves source-relevant deck sections during import, display, and export.
- **Fields**:
  - `SectionId`: stable logical identifier
  - `DisplayName`: user-visible section label
  - `Role`: commander, mainboard, sideboard, maybeboard, companion, or custom
  - `SortOrder`: display and export order
  - `EntryCount`: derived total entries in the section
- **Relationships**:
  - Belongs to one `PortableDeckSnapshot`
- **Validation Rules**:
  - Commander-role sections must remain distinguishable from auxiliary sections.

## FormatProfile

- **Purpose**: Business rules for a supported import/export text format.
- **Fields**:
  - `FormatId`: stable identifier such as `moxfield-text`, `manabox-text`, or `generic-plaintext`
  - `DisplayName`: user-facing format name
  - `DetectionRules`: ordered heuristics or markers used for auto-detection
  - `SectionRules`: how the format expresses commander slots and deck sections
  - `QuantityRules`: how counts are parsed and rendered
  - `ExportCapabilities`: what metadata or sections can be represented on export
- **Relationships**:
  - Can produce many `ImportedDeckRecord`
  - Can generate many `ExportPreview`
- **Validation Rules**:
  - `FormatId` must be unique.
  - Detection rules must be deterministic enough to flag ambiguous matches.

## ImportDiagnostic

- **Purpose**: User-visible issue or warning produced during parsing of imported text.
- **Fields**:
  - `DiagnosticId`: stable identifier
  - `Severity`: info, warning, or error
  - `Code`: normalized diagnostic code
  - `Message`: user-readable explanation
  - `SourceLineNumber`: original line number when applicable
  - `SourceLineText`: original text that triggered the diagnostic
  - `SuggestedAction`: concise recovery guidance
- **Relationships**:
  - Belongs to one `ImportedDeckRecord`
- **Validation Rules**:
  - Diagnostics rendered in the UI must be encoded and treated as plain text.

## ExportPreview

- **Purpose**: Prepared export result for a selected deck and target format before the user copies
  or downloads it.
- **Fields**:
  - `TargetFormatId`: destination format profile identifier
  - `DocumentText`: rendered deck text
  - `Warnings`: collection of export warnings or lossy-transform notes
  - `GeneratedUtc`: timestamp of preview generation
- **Relationships**:
  - Belongs to one `ImportedDeckRecord`
  - References one `FormatProfile`
- **Validation Rules**:
  - `DocumentText` must be generated entirely from the local normalized deck state.
  - `Warnings` must be present when the target format cannot represent imported metadata exactly.

## WorkingCopyProjection

- **Purpose**: Explicit transient projection of a local imported deck into the existing workspace
  contract shape for optional validation or analysis workflows.
- **Fields**:
  - `ProjectionId`: ephemeral identifier
  - `ImportedDeckId`: source local deck identifier
  - `DeckSnapshotContract`: normalized workspace payload shape
  - `CreatedUtc`: timestamp of explicit projection
- **Relationships**:
  - References one `ImportedDeckRecord`
- **Validation Rules**:
  - Must only be created from a direct user action.
  - Must not be stored back into the persistent browser deck library as server-owned state.