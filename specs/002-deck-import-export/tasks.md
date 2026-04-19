# Tasks: Commander Deck Import And Export

**Input**: Design documents from `/specs/002-deck-import-export/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, quickstart.md, contracts/deck-portability-contract.yaml

**Tests**: Automated tests are required for import/export behavior, local browser persistence, UI state transitions, performance-sensitive workflows, and security-sensitive handling of untrusted deck text.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this belongs to (e.g., [US1], [US2], [US3])
- Include exact file paths in descriptions

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Align project references, test support, and representative fixtures for browser-local deck portability work.

- [ ] T001 Update src/CommandSynergy.Client/CommandSynergy.Client.csproj and src/CommandSynergy/CommandSynergy.csproj with the browser-storage dependency and import/export service references required by the feature plan
- [ ] T002 [P] Align tests/CommandSynergy.Application.Tests/CommandSynergy.Application.Tests.csproj and tests/CommandSynergy.WebUI.Tests/CommandSynergy.WebUI.Tests.csproj with the fixture, bUnit, and security test support needed for deck portability coverage
- [ ] T003 [P] Add representative Moxfield, ManaBox, and generic plaintext sample deck documents under tests/CommandSynergy.Application.Tests/Decks/Fixtures/
- [ ] T004 [P] Add shared fixture-loading helpers for deck portability samples in tests/CommandSynergy.Application.Tests/Decks/DeckPortabilityFixtureLoader.cs

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Establish shared portability contracts, models, format registration, and browser-local storage boundaries that block all user stories.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [ ] T005 Create and align shared deck portability DTO contracts in src/CommandSynergy.Application/Contracts/DeckPortabilityContracts.cs with specs/002-deck-import-export/contracts/deck-portability-contract.yaml
- [ ] T006 [P] Create portability abstractions in src/CommandSynergy.Application/Abstractions/IDeckImportService.cs, src/CommandSynergy.Application/Abstractions/IDeckExportService.cs, src/CommandSynergy.Application/Abstractions/IDeckFormatRegistry.cs, and src/CommandSynergy.Application/Abstractions/IWorkingCopyProjectionService.cs
- [ ] T007 [P] Create normalized portability models in src/CommandSynergy.Application/Decks/Portability/ImportedDeckLibraryDocument.cs, src/CommandSynergy.Application/Decks/Portability/ImportedDeckRecord.cs, src/CommandSynergy.Application/Decks/Portability/PortableDeckSnapshot.cs, src/CommandSynergy.Application/Decks/Portability/ImportDiagnostic.cs, and src/CommandSynergy.Application/Decks/Portability/ExportPreview.cs
- [ ] T008 [P] Implement format profile and detection scaffolding in src/CommandSynergy.Application/Decks/Portability/DeckFormatRegistry.cs, src/CommandSynergy.Application/Decks/Portability/DeckFormatDetectionService.cs, and src/CommandSynergy.Application/Decks/Portability/Formats/DeckFormatProfileBase.cs
- [ ] T009 Implement browser-local storage serialization, schema-version migration, and payload-limit scaffolding in src/CommandSynergy.Client/Services/ImportedDeckLibraryStore.cs and src/CommandSynergy.Client/Services/ImportedDeckLibrarySerializer.cs
- [ ] T010 Implement portability dependency registration in src/CommandSynergy.Application/DependencyInjection.cs, src/CommandSynergy.Client/Program.cs, and src/CommandSynergy/Program.cs
- [ ] T011 Create foundational contract-serialization and storage-safety regression coverage in tests/CommandSynergy.WebUI.Tests/ContractSerializationTests.cs and tests/CommandSynergy.WebUI.Tests/Security/DeckWorkspaceSecurityTests.cs

**Checkpoint**: Foundation ready - user story implementation can now begin in priority order or in parallel where capacity allows

---

## Phase 3: User Story 1 - Import An Existing Decklist (Priority: P1) 🎯 MVP

**Goal**: Let a user import a supported external decklist, preserve partial-success results locally, and open an explicit working copy in the existing workspace without sending raw imported documents to the server.

**Independent Test**: Paste supported Moxfield and ManaBox decklists, confirm unambiguous auto-detection or manual confirmation when needed, verify commander and section mapping, and confirm unresolved lines remain visible with recovery guidance after refresh.

### Tests for User Story 1

- [ ] T012 [P] [US1] Add unit tests for supported-format detection, ambiguity handling, and manual format confirmation in tests/CommandSynergy.Application.Tests/Decks/DeckFormatDetectionServiceTests.cs
- [ ] T013 [P] [US1] Add unit tests for partial-success imports, commander assignment, section preservation, and diagnostic persistence in tests/CommandSynergy.Application.Tests/Decks/DeckImportServiceTests.cs
- [ ] T014 [P] [US1] Add bUnit tests for import dialog, hydration, empty-state, partial-success, and corrupted-storage recovery behavior in tests/CommandSynergy.WebUI.Tests/Components/DeckPortabilityWorkflowTests.cs
- [ ] T015 [P] [US1] Add security tests for oversized payload rejection and plain-text rendering of untrusted diagnostics in tests/CommandSynergy.WebUI.Tests/Security/DeckWorkspaceSecurityTests.cs

### Implementation for User Story 1

- [ ] T016 [P] [US1] Implement Moxfield and ManaBox format profiles in src/CommandSynergy.Application/Decks/Portability/Formats/MoxfieldTextFormatProfile.cs and src/CommandSynergy.Application/Decks/Portability/Formats/ManaBoxTextFormatProfile.cs
- [ ] T017 [P] [US1] Implement import orchestration and explicit workspace projection in src/CommandSynergy.Application/Decks/Portability/DeckImportService.cs and src/CommandSynergy.Application/Decks/Portability/WorkingCopyProjectionService.cs
- [ ] T018 [P] [US1] Implement imported-deck library persistence, hydration, and active-deck switching in src/CommandSynergy.Client/Services/ImportedDeckLibraryStore.cs and src/CommandSynergy.Client/Services/ImportedDeckLibraryState.cs
- [ ] T019 [US1] Extend workspace client orchestration for imported-deck activation and validation handoff in src/CommandSynergy.Client/Services/DeckWorkspaceClient.cs and src/CommandSynergy.Application/Decks/DeckWorkspaceStateFactory.cs
- [ ] T020 [US1] Add import entry points, format confirmation, diagnostics, and recovery states in src/CommandSynergy/Components/Pages/Home.razor, src/CommandSynergy/Components/Pages/Home.razor.cs, src/CommandSynergy/Components/Decks/DeckWorkspace.razor, and src/CommandSynergy/Components/Decks/DeckWorkspaceViewModel.cs

**Checkpoint**: User Story 1 should be fully functional and testable independently as the MVP

---

## Phase 4: User Story 2 - Export A Current Decklist (Priority: P2)

**Goal**: Let a user export the current deck in supported named formats, disclose lossy transforms before copy or download, and preserve commander identity and quantities in the rendered text.

**Independent Test**: Open an existing deck in the workspace, export it as Moxfield and ManaBox text, confirm the output ordering and quantities match expectations, and verify warnings appear when workspace metadata cannot be represented.

### Tests for User Story 2

- [ ] T021 [P] [US2] Add unit tests for named-format export rendering, card ordering, quantity notation, and lossy-transform warnings in tests/CommandSynergy.Application.Tests/Decks/DeckExportServiceTests.cs
- [ ] T022 [P] [US2] Add unit tests for workspace-to-portable snapshot projection used by export flows in tests/CommandSynergy.Application.Tests/Decks/WorkingCopyProjectionServiceTests.cs
- [ ] T023 [P] [US2] Add bUnit tests for export preview, warning disclosure, and copy or download actions in tests/CommandSynergy.WebUI.Tests/Components/DeckExportPanelTests.cs

### Implementation for User Story 2

- [ ] T024 [P] [US2] Implement named-format export rendering in src/CommandSynergy.Application/Decks/Portability/DeckExportService.cs, src/CommandSynergy.Application/Decks/Portability/Formats/MoxfieldTextFormatProfile.cs, and src/CommandSynergy.Application/Decks/Portability/Formats/ManaBoxTextFormatProfile.cs
- [ ] T025 [P] [US2] Implement export-preview state management and copy or download coordination in src/CommandSynergy.Client/Services/DeckWorkspaceClient.cs and src/CommandSynergy/Components/Decks/DeckWorkspaceViewModel.cs
- [ ] T026 [US2] Add export UI, target-format selection, and warning messaging in src/CommandSynergy/Components/Decks/DeckWorkspace.razor, src/CommandSynergy/Components/Pages/Home.razor.cs, and src/CommandSynergy/Components/Decks/DeckExportPanel.razor

**Checkpoint**: User Story 2 should be independently testable by exporting an existing workspace deck without relying on unfinished follow-on stories

---

## Phase 5: User Story 3 - Use Portable Plain-Text Decklists (Priority: P3)

**Goal**: Support a generic portable plaintext format for import and export so users can round-trip decks through copy-paste workflows even when a site-specific format is unavailable.

**Independent Test**: Import a generic plaintext decklist with commander and section headers, make a small edit in the workspace, export it again as generic plaintext, and confirm the essential structure and quantities are preserved.

### Tests for User Story 3

- [ ] T027 [P] [US3] Add unit tests for generic plaintext import, export, and round-trip preservation in tests/CommandSynergy.Application.Tests/Decks/GenericPlaintextFormatProfileTests.cs
- [ ] T028 [P] [US3] Add unit tests for section-header handling, commander-role edge cases, and ambiguous generic lines in tests/CommandSynergy.Application.Tests/Decks/GenericPlaintextImportScenariosTests.cs
- [ ] T029 [P] [US3] Add bUnit tests for generic format selection, fallback detection, and clipboard-friendly output in tests/CommandSynergy.WebUI.Tests/Components/DeckPortabilityWorkflowTests.cs

### Implementation for User Story 3

- [ ] T030 [P] [US3] Implement the generic plaintext parser and exporter profile in src/CommandSynergy.Application/Decks/Portability/Formats/GenericPlaintextFormatProfile.cs
- [ ] T031 [US3] Extend format registry and import or export orchestration for generic fallback behavior in src/CommandSynergy.Application/Decks/Portability/DeckFormatRegistry.cs, src/CommandSynergy.Application/Decks/Portability/DeckImportService.cs, and src/CommandSynergy.Application/Decks/Portability/DeckExportService.cs
- [ ] T032 [US3] Extend UI hints and saved source-format metadata for portable plaintext workflows in src/CommandSynergy/Components/Pages/Home.razor, src/CommandSynergy/Components/Decks/DeckWorkspaceViewModel.cs, and src/CommandSynergy.Client/Services/ImportedDeckLibraryStore.cs

**Checkpoint**: All user stories should now be independently functional with named-format and generic plaintext portability

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Harden the feature across documentation, performance, security, and end-to-end validation.

- [ ] T033 [P] Update README.md and specs/002-deck-import-export/quickstart.md with supported formats, local-only storage behavior, recovery guidance, and export limitations
- [ ] T034 [P] Add performance regression coverage for import, export, hydration, and deck switching budgets in tests/CommandSynergy.Application.Tests/Performance/DeckPortabilityPerformanceTests.cs
- [ ] T035 [P] Add regression coverage for corrupted localStorage recovery and no-raw-document server transfer in tests/CommandSynergy.WebUI.Tests/Security/DeckWorkspaceSecurityTests.cs and tests/CommandSynergy.WebUI.Tests/Components/DeckPortabilityWorkflowTests.cs
- [ ] T036 Review logging, payload-limit enforcement, UX state consistency, and quickstart validation across src/CommandSynergy.Client/Services/ImportedDeckLibraryStore.cs, src/CommandSynergy.Application/Decks/Portability/DeckImportService.cs, src/CommandSynergy.Application/Decks/Portability/DeckExportService.cs, src/CommandSynergy/Components/Decks/DeckWorkspaceViewModel.cs, and specs/002-deck-import-export/quickstart.md

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - blocks all user stories
- **User Story 1 (Phase 3)**: Depends on Foundational completion - delivers the MVP
- **User Story 2 (Phase 4)**: Depends on Foundational completion and can be validated against the existing workspace deck flow even if later stories are not started
- **User Story 3 (Phase 5)**: Depends on Foundational completion and reuses the import/export abstractions stabilized by earlier phases
- **Polish (Phase 6)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational - no dependency on other user stories
- **User Story 2 (P2)**: Can start after Foundational - uses the existing workspace deck and remains independently testable, while also integrating cleanly with imported decks once US1 is present
- **User Story 3 (P3)**: Can start after Foundational - shares the same portability abstractions but remains independently testable through the generic plaintext path

### Within Each User Story

- Tests MUST be written and fail before implementation
- Format profiles and models before orchestration services
- Client state management before full UI integration
- Core implementation before performance, security, and polish refinements
- Complete and validate each story at its checkpoint before broadening scope

### Parallel Opportunities

- Setup tasks marked [P] can run in parallel
- Foundational abstraction and model tasks marked [P] can run in parallel
- Test tasks within each story marked [P] can run in parallel
- Format-profile implementation tasks can run in parallel when they touch separate files
- Once Foundational is complete, different user stories can proceed in parallel if team capacity allows

---

## Parallel Example: User Story 1

```text
Task: "Add unit tests for supported-format detection, ambiguity handling, and manual format confirmation in tests/CommandSynergy.Application.Tests/Decks/DeckFormatDetectionServiceTests.cs"
Task: "Add bUnit tests for import dialog, hydration, empty-state, partial-success, and corrupted-storage recovery behavior in tests/CommandSynergy.WebUI.Tests/Components/DeckPortabilityWorkflowTests.cs"
Task: "Add security tests for oversized payload rejection and plain-text rendering of untrusted diagnostics in tests/CommandSynergy.WebUI.Tests/Security/DeckWorkspaceSecurityTests.cs"

Task: "Implement Moxfield and ManaBox format profiles in src/CommandSynergy.Application/Decks/Portability/Formats/MoxfieldTextFormatProfile.cs and src/CommandSynergy.Application/Decks/Portability/Formats/ManaBoxTextFormatProfile.cs"
Task: "Implement imported-deck library persistence, hydration, and active-deck switching in src/CommandSynergy.Client/Services/ImportedDeckLibraryStore.cs and src/CommandSynergy.Client/Services/ImportedDeckLibraryState.cs"
```

## Parallel Example: User Story 2

```text
Task: "Add unit tests for named-format export rendering, card ordering, quantity notation, and lossy-transform warnings in tests/CommandSynergy.Application.Tests/Decks/DeckExportServiceTests.cs"
Task: "Add bUnit tests for export preview, warning disclosure, and copy or download actions in tests/CommandSynergy.WebUI.Tests/Components/DeckExportPanelTests.cs"

Task: "Implement named-format export rendering in src/CommandSynergy.Application/Decks/Portability/DeckExportService.cs, src/CommandSynergy.Application/Decks/Portability/Formats/MoxfieldTextFormatProfile.cs, and src/CommandSynergy.Application/Decks/Portability/Formats/ManaBoxTextFormatProfile.cs"
Task: "Implement export-preview state management and copy or download coordination in src/CommandSynergy.Client/Services/DeckWorkspaceClient.cs and src/CommandSynergy/Components/Decks/DeckWorkspaceViewModel.cs"
```

## Parallel Example: User Story 3

```text
Task: "Add unit tests for generic plaintext import, export, and round-trip preservation in tests/CommandSynergy.Application.Tests/Decks/GenericPlaintextFormatProfileTests.cs"
Task: "Add bUnit tests for generic format selection, fallback detection, and clipboard-friendly output in tests/CommandSynergy.WebUI.Tests/Components/DeckPortabilityWorkflowTests.cs"

Task: "Implement the generic plaintext parser and exporter profile in src/CommandSynergy.Application/Decks/Portability/Formats/GenericPlaintextFormatProfile.cs"
Task: "Extend UI hints and saved source-format metadata for portable plaintext workflows in src/CommandSynergy/Components/Pages/Home.razor, src/CommandSynergy/Components/Decks/DeckWorkspaceViewModel.cs, and src/CommandSynergy.Client/Services/ImportedDeckLibraryStore.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational
3. Complete Phase 3: User Story 1
4. **STOP and VALIDATE**: Confirm named-format import, partial-success diagnostics, local persistence, and explicit workspace opening work independently
5. Demo the imported-deck workflow before moving on to export or generic plaintext support

### Incremental Delivery

1. Complete Setup + Foundational → portability foundation ready
2. Add User Story 1 → Test independently → MVP ready
3. Add User Story 2 → Test independently → export-ready increment
4. Add User Story 3 → Test independently → generic portability ready
5. Finish with Polish → documentation, performance, and security hardening complete

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together
2. Once Foundational is complete:
   - Developer A: User Story 1 import profiles, local deck store, and import UI
   - Developer B: User Story 2 export rendering and preview UX
   - Developer C: User Story 3 generic plaintext profile and fallback UX

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] labels map each task to a specific user story for traceability
- Each user story remains independently testable at its checkpoint
- Verify tests fail before implementing
- Verify security, UX, and performance tasks are not skipped when the story affects them
- Avoid vague tasks, same-file parallel conflicts, and cross-story work that breaks independent delivery