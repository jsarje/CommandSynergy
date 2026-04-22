# Tasks: Synergy Scoring & Deck Theme Analysis

**Input**: Design documents from `/specs/003-synergy-theme-analysis/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, quickstart.md, contracts/theme-analysis-api.yaml

**Tests**: Automated tests are required for taxonomy matching, synergy score calculation, commander alignment, unavailable-metadata handling, API contract changes, UI state handling, end-to-end analysis verification, security controls, and performance budgets called out in NFR-001 through NFR-004.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this belongs to (e.g. [US1], [US2], [US3])
- Include exact file paths in descriptions

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Align configuration, fixtures, and test support for theme-analysis implementation.

- [X] T001 Add EDHREC configuration defaults and user-agent settings in src/CommandSynergy/appsettings.json and src/CommandSynergy/appsettings.Development.json
- [X] T002 [P] Add representative focused, unfocused, and commander-misaligned deck fixtures under tests/CommandSynergy.Application.Tests/Analysis/Fixtures/
- [X] T003 [P] Add shared theme-analysis test data builders in tests/CommandSynergy.Application.Tests/Analysis/ThemeAnalysisTestData.cs and tests/CommandSynergy.WebUI.Tests/Components/ThemeAnalysisTestData.cs
- [X] T004 [P] Align infrastructure and web UI test projects for theme-analysis HTTP mocking and component coverage in tests/CommandSynergy.Infrastructure.Tests/CommandSynergy.Infrastructure.Tests.csproj and tests/CommandSynergy.WebUI.Tests/CommandSynergy.WebUI.Tests.csproj

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Establish shared domain types, contracts, metadata persistence, and ingestion hooks required by every story.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T005 Create foundational theme-analysis domain types in src/CommandSynergy.Domain/Analysis/ThemeDefinition.cs, src/CommandSynergy.Domain/Analysis/ThemeTaxonomy.cs, src/CommandSynergy.Domain/Analysis/DeckTheme.cs, src/CommandSynergy.Domain/Analysis/CommanderAlignment.cs, and src/CommandSynergy.Domain/Analysis/ThemeAnalysis.cs
- [X] T006 [P] Extend card metadata and synergy models with theme-signal and score fields in src/CommandSynergy.Domain/Cards/CardProfile.cs and src/CommandSynergy.Domain/Analysis/SynergyAssessment.cs
- [X] T007 [P] Extend deck analysis contracts for theme analysis and enhanced synergy fields in src/CommandSynergy.Application/Contracts/DeckWorkspaceContracts.cs
- [X] T008 [P] Add theme-matching service scaffolding and application registration in src/CommandSynergy.Application/Analysis/ThemeMatchingService.cs and src/CommandSynergy.Application/DependencyInjection.cs
- [X] T009 [P] Add backward-compatible ThemeSignals parquet mapping in src/CommandSynergy.Infrastructure/CardMetadata/ParquetCardMetadataStore.cs
- [X] T010 [P] Add Scryfall keyword ingestion and bulk-import theme-signal population hooks in src/CommandSynergy.Infrastructure/Scryfall/ScryfallClient.cs and src/CommandSynergy.Ingestion/Program.cs
- [X] T011 Implement EDHREC options, safe HTTP registration, and dependency wiring in src/CommandSynergy.Infrastructure/DependencyInjection.cs and src/CommandSynergy/Program.cs

**Checkpoint**: Foundation ready - user stories can now be implemented in priority order or in parallel where capacity allows

---

## Phase 3: User Story 1 - Identify Deck Themes (Priority: P1) 🎯 MVP

**Goal**: Detect and rank the deck's dominant themes using commander-weighted primary-theme direction, suppress results for undersized decks, and refresh theme analysis as the workspace changes.

**Independent Test**: Submit a known graveyard-focused deck and a no-clear-theme deck, confirm the ranked themes and placeholder states match the spec, and verify the analysis refreshes after deck edits.

### Tests for User Story 1

- [X] T012 [P] [US1] Add taxonomy signal coverage for the canonical theme list in tests/CommandSynergy.Domain.Tests/Analysis/ThemeTaxonomyTests.cs
- [X] T013 [P] [US1] Add per-card theme signal tests for oracle text, keywords, and type-line matching in tests/CommandSynergy.Application.Tests/Analysis/ThemeMatchingServiceTests.cs
- [X] T014 [P] [US1] Add deck-level ranking, commander-weighted primary-theme, and insufficient-card analysis tests in tests/CommandSynergy.Application.Tests/Analysis/ThemeAnalysisServiceTests.cs
- [X] T015 [P] [US1] Add bUnit coverage for loading, error, insufficient, and ready theme states in tests/CommandSynergy.WebUI.Tests/Components/ThemeAnalysisPanelTests.cs

### Implementation for User Story 1

- [X] T016 [US1] Implement canonical theme definitions and signal scoring in src/CommandSynergy.Domain/Analysis/ThemeTaxonomy.cs and src/CommandSynergy.Application/Analysis/ThemeMatchingService.cs
- [X] T017 [US1] Implement deck theme aggregation, commander-weighted ranking, and insufficient-card handling in src/CommandSynergy.Application/Analysis/ThemeAnalysisService.cs
- [X] T018 [US1] Extend deck analysis orchestration to populate themeAnalysis on every workspace re-analysis in src/CommandSynergy.Application/Analysis/DeckAnalysisService.cs and src/CommandSynergy.Client/Services/DeckWorkspaceClient.cs
- [X] T019 [US1] Add the theme analysis panel component and accessible state markup in src/CommandSynergy/Components/Decks/ThemeAnalysisPanel.razor and src/CommandSynergy/Components/Decks/ThemeAnalysisPanel.razor.css
- [X] T020 [US1] Render theme analysis alongside the existing deck summary and preserve stale-result messaging during refresh in src/CommandSynergy/Components/Decks/DeckWorkspace.razor and src/CommandSynergy/Components/Decks/DeckWorkspaceViewModel.cs

**Checkpoint**: User Story 1 should now identify dominant themes with commander-weighted ranking and show the required loading, empty, error, and ready states independently of later stories

---

## Phase 4: User Story 2 - View Synergy Score (Priority: P1)

**Goal**: Compute and display a 0-100 thematic synergy score with qualitative labelling and hide the score when the deck is too small.

**Independent Test**: Compare a tightly focused 100-card deck with a random card pile, confirm the focused deck scores at least 30 points higher, and verify undersized decks show guidance instead of a score.

### Tests for User Story 2

- [X] T021 [P] [US2] Add score calibration tests for focused, unfocused, and undersized deck configurations in tests/CommandSynergy.Application.Tests/Analysis/ThemeAnalysisServiceTests.cs
- [X] T022 [P] [US2] Add endpoint regression tests for themeScore, finalScore, qualitativeLabel, and insufficient-deck responses in tests/CommandSynergy.WebUI.Tests/Endpoints/DeckAnalysisEndpointTests.cs
- [X] T023 [P] [US2] Add contract serialization coverage for enhanced synergy payloads in tests/CommandSynergy.WebUI.Tests/ContractSerializationTests.cs

### Implementation for User Story 2

- [X] T024 [US2] Implement theme coherence scoring, qualitative labels, and summary generation in src/CommandSynergy.Application/Analysis/ThemeAnalysisService.cs and src/CommandSynergy.Domain/Analysis/SynergyAssessment.cs
- [X] T025 [US2] Extend deck analysis response mapping and cache-safe score propagation in src/CommandSynergy.Application/Analysis/DeckAnalysisService.cs and src/CommandSynergy.Infrastructure/Analysis/DeckAnalysisCache.cs
- [X] T026 [US2] Update analysis summary UI to surface final score, label, and insufficient-card suppression in src/CommandSynergy/Components/Decks/AnalysisPanel.razor and src/CommandSynergy/Components/Decks/DeckWorkspace.razor

**Checkpoint**: User Story 2 should now expose a coherent synergy score and qualitative verdict without depending on the later card-exploration or commander-alignment work

---

## Phase 5: User Story 3 - Explore Cards by Theme (Priority: P2)

**Goal**: Let users inspect which cards drive each detected theme, which cards are currently off-theme, and when a card is off-theme because metadata is unavailable.

**Independent Test**: Expand a detected theme, confirm every listed card includes a contribution reason, verify multi-theme cards appear under each matching theme, confirm off-theme cards are grouped separately, and verify cards missing metadata surface a dedicated unavailable-data indicator.

### Tests for User Story 3

- [X] T027 [P] [US3] Add contributor-reason and off-theme grouping tests in tests/CommandSynergy.Application.Tests/Analysis/ThemeAnalysisServiceTests.cs
- [X] T028 [P] [US3] Add bUnit coverage for theme expansion, multi-theme cards, and off-theme rendering in tests/CommandSynergy.WebUI.Tests/Components/ThemeAnalysisPanelTests.cs
- [X] T042 [P] [US3] Add unavailable-metadata coverage for off-theme cards in tests/CommandSynergy.Application.Tests/Analysis/ThemeAnalysisServiceTests.cs and tests/CommandSynergy.WebUI.Tests/Components/ThemeAnalysisPanelTests.cs

### Implementation for User Story 3

- [X] T029 [US3] Extend deck-theme and theme-analysis contracts to carry contributor reasons and off-theme identities in src/CommandSynergy.Domain/Analysis/DeckTheme.cs, src/CommandSynergy.Domain/Analysis/ThemeAnalysis.cs, and src/CommandSynergy.Application/Contracts/DeckWorkspaceContracts.cs
- [X] T030 [US3] Build per-theme contributor explanations and off-theme card collection in src/CommandSynergy.Application/Analysis/ThemeAnalysisService.cs
- [X] T031 [US3] Add expandable theme details, contribution explanations, and off-theme sections in src/CommandSynergy/Components/Decks/ThemeAnalysisPanel.razor and src/CommandSynergy/Components/Decks/ThemeAnalysisPanel.razor.css
- [X] T043 [US3] Carry unavailable-metadata reasons through theme-analysis results and render a dedicated unavailable-data indicator in src/CommandSynergy.Application/Analysis/ThemeAnalysisService.cs, src/CommandSynergy.Domain/Analysis/ThemeAnalysis.cs, src/CommandSynergy.Application/Contracts/DeckWorkspaceContracts.cs, and src/CommandSynergy/Components/Decks/ThemeAnalysisPanel.razor

**Checkpoint**: User Story 3 should now turn abstract themes into actionable keep-or-cut guidance without requiring commander-specific enhancements

---

## Phase 6: User Story 4 - Commander-Anchored Alignment (Priority: P2)

**Goal**: Show how well the 99 support the commander's plan and optionally blend EDHREC synergy without blocking local analysis.

**Independent Test**: Analyse a deck whose commander and 99 are aligned, a deck whose 99 fights the commander's plan, and a deck with no commander, then confirm the alignment indicator and graceful EDHREC fallback match the spec.

### Tests for User Story 4

- [X] T032 [P] [US4] Add commander-alignment level tests for matching, conflicting, and commanderless decks in tests/CommandSynergy.Application.Tests/Analysis/ThemeAnalysisServiceTests.cs
- [X] T033 [P] [US4] Add EDHREC slug validation, response parsing, and fallback tests in tests/CommandSynergy.Infrastructure.Tests/Edhrec/EdhrecClientTests.cs
- [X] T034 [P] [US4] Add endpoint regression coverage for commanderAlignment and EDHREC-enhanced final scores in tests/CommandSynergy.WebUI.Tests/Endpoints/DeckAnalysisEndpointTests.cs

### Implementation for User Story 4

- [X] T035 [US4] Implement EDHREC commander documents and resilient client access in src/CommandSynergy.Infrastructure/Edhrec/EdhrecCommanderDocument.cs and src/CommandSynergy.Infrastructure/Edhrec/EdhrecClient.cs
- [X] T036 [US4] Integrate commander alignment computation and optional EDHREC blending in src/CommandSynergy.Application/Analysis/ThemeAnalysisService.cs and src/CommandSynergy.Application/Analysis/DeckAnalysisService.cs
- [X] T037 [US4] Surface commander alignment indicators and EDHREC-degraded messaging in src/CommandSynergy/Components/Decks/ThemeAnalysisPanel.razor and src/CommandSynergy/Components/Decks/DeckWorkspace.razor

**Checkpoint**: User Story 4 should now evaluate whether the 99 support the commander while keeping local theme analysis available when EDHREC data is absent or rejected

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Validate documentation, performance, security, and end-to-end quality across all delivered stories.

- [X] T038 [P] Update README.md and specs/003-synergy-theme-analysis/quickstart.md with ingestion refresh steps, EDHREC fallback behavior, and theme-analysis usage guidance
- [X] T039 [P] Add performance regression coverage for full-deck and single-card-update analysis budgets in tests/CommandSynergy.Application.Tests/Performance/ThemeAnalysisPerformanceTests.cs and response-time assertions for representative 100-card analysis requests in tests/CommandSynergy.WebUI.Tests/Endpoints/DeckAnalysisIntegrationTests.cs
- [X] T040 [P] Add security and serialization regressions for slug allowlisting and theme payload compatibility in tests/CommandSynergy.Infrastructure.Tests/Edhrec/EdhrecClientTests.cs and tests/CommandSynergy.WebUI.Tests/ContractSerializationTests.cs
- [ ] T041 Validate telemetry and logging coverage for theme-analysis execution and EDHREC fallback paths in src/CommandSynergy.Application/Analysis/DeckAnalysisService.cs and src/CommandSynergy.Infrastructure/Edhrec/EdhrecClient.cs
- [X] T044 [P] Add end-to-end integration coverage for known focused, unfocused, and commander-misaligned deck fixtures against /api/deck/analyse, including initial-view-ready payload assertions used by the deck workspace in tests/CommandSynergy.WebUI.Tests/Endpoints/DeckAnalysisIntegrationTests.cs
- [X] T045 Document the OWASP threat review, mitigation evidence, and disposition of any high-severity findings in specs/003-synergy-theme-analysis/checklists/security-review.md
- [X] T046 [P] Add accessibility regression coverage for ARIA roles, labels, and initial-view visibility of top-theme contributors and off-theme reasons in tests/CommandSynergy.WebUI.Tests/Components/ThemeAnalysisPanelTests.cs
- [X] T047 Validate quickstart execution for both local-only analysis and EDHREC-degraded fallback in specs/003-synergy-theme-analysis/quickstart.md

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - blocks all user stories
- **User Story 1 (Phase 3)**: Depends on Foundational completion - delivers the MVP theme-detection experience
- **User Story 2 (Phase 4)**: Depends on Foundational completion and builds on the shared analysis outputs stabilized in US1
- **User Story 3 (Phase 5)**: Depends on Foundational completion and can follow once ranked themes are available from US1
- **User Story 4 (Phase 6)**: Depends on Foundational completion and can layer onto the shared analysis service after US1's base aggregation work exists
- **Polish (Phase 7)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational - no dependency on other user stories and establishes the commander-weighted base ranking required by FR-007
- **User Story 2 (P1)**: Can start after Foundational, but it benefits from the base theme-analysis pipeline introduced in US1
- **User Story 3 (P2)**: Depends on US1's ranked-theme outputs and remains independently testable once those outputs exist
- **User Story 4 (P2)**: Depends on US1's commander-weighted aggregation pipeline and remains independently testable without US3's expanded card-exploration UI

### Within Each User Story

- Tests MUST be written and fail before implementation
- Domain and contract changes before orchestration changes
- Application services before endpoint or client integration
- UI wiring after service outputs are stable
- Complete the checkpoint for each story before broadening scope

### Parallel Opportunities

- Setup tasks marked [P] can run in parallel
- Foundational model, contract, parquet, and ingestion tasks marked [P] can run in parallel
- Test tasks within each story marked [P] can run in parallel
- US3 and US4 can proceed in parallel once US1 is complete and the base analysis pipeline is stable
- Polish tasks marked [P] can run in parallel after the targeted stories are implemented

---

## Parallel Example: User Story 1

```text
Task: "Add taxonomy signal coverage for the canonical theme list in tests/CommandSynergy.Domain.Tests/Analysis/ThemeTaxonomyTests.cs"
Task: "Add per-card theme signal tests for oracle text, keywords, and type-line matching in tests/CommandSynergy.Application.Tests/Analysis/ThemeMatchingServiceTests.cs"
Task: "Add bUnit coverage for loading, error, insufficient, and ready theme states in tests/CommandSynergy.WebUI.Tests/Components/ThemeAnalysisPanelTests.cs"

Task: "Add the theme analysis panel component and accessible state markup in src/CommandSynergy/Components/Decks/ThemeAnalysisPanel.razor and src/CommandSynergy/Components/Decks/ThemeAnalysisPanel.razor.css"
Task: "Extend deck analysis orchestration to populate themeAnalysis on every workspace re-analysis in src/CommandSynergy.Application/Analysis/DeckAnalysisService.cs and src/CommandSynergy.Client/Services/DeckWorkspaceClient.cs"
```

## Parallel Example: User Story 2

```text
Task: "Add endpoint regression tests for themeScore, finalScore, qualitativeLabel, and insufficient-deck responses in tests/CommandSynergy.WebUI.Tests/Endpoints/DeckAnalysisEndpointTests.cs"
Task: "Add contract serialization coverage for enhanced synergy payloads in tests/CommandSynergy.WebUI.Tests/ContractSerializationTests.cs"

Task: "Implement theme coherence scoring, qualitative labels, and summary generation in src/CommandSynergy.Application/Analysis/ThemeAnalysisService.cs and src/CommandSynergy.Domain/Analysis/SynergyAssessment.cs"
Task: "Update analysis summary UI to surface final score, label, and insufficient-card suppression in src/CommandSynergy/Components/Decks/AnalysisPanel.razor and src/CommandSynergy/Components/Decks/DeckWorkspace.razor"
```

## Parallel Example: User Story 4

```text
Task: "Add commander-weighting and alignment-level tests for matching, conflicting, and commanderless decks in tests/CommandSynergy.Application.Tests/Analysis/ThemeAnalysisServiceTests.cs"
Task: "Add EDHREC slug validation, response parsing, and fallback tests in tests/CommandSynergy.Infrastructure.Tests/Edhrec/EdhrecClientTests.cs"

Task: "Implement EDHREC commander documents and resilient client access in src/CommandSynergy.Infrastructure/Edhrec/EdhrecCommanderDocument.cs and src/CommandSynergy.Infrastructure/Edhrec/EdhrecClient.cs"
Task: "Surface commander alignment indicators and EDHREC-degraded messaging in src/CommandSynergy/Components/Decks/ThemeAnalysisPanel.razor and src/CommandSynergy/Components/Decks/DeckWorkspace.razor"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational
3. Complete Phase 3: User Story 1
4. **STOP and VALIDATE**: Confirm ranked themes, insufficient-card messaging, refresh behavior, and UI states work independently
5. Demo the base theme-detection experience before layering in score calibration or commander alignment

### Incremental Delivery

1. Complete Setup + Foundational → theme-analysis foundation ready
2. Add User Story 1 → Test independently → MVP ready
3. Add User Story 2 → Test independently → synergy-score increment ready
4. Add User Story 3 → Test independently → card-exploration increment ready
5. Add User Story 4 → Test independently → commander-alignment increment ready
6. Finish with Polish → documentation, security, accessibility, and performance hardened

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together
2. Once Foundational is complete:
   - Developer A: User Story 1 taxonomy, aggregation, and panel work
   - Developer B: User Story 2 score calculation and endpoint or cache propagation
   - Developer C: User Story 4 EDHREC client and commander-alignment work
3. Add User Story 3 after the shared UI payloads are stable, or assign it in parallel once US1 is merged

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] labels map tasks to specific user stories for traceability
- Each user story remains independently testable at its checkpoint
- Verify tests fail before implementing
- Preserve the AnalysisPanel state-order rule: loading before error before insufficient or null before ready
- Keep EDHREC integration non-blocking so local theme analysis remains authoritative
- Avoid vague tasks, same-file parallel conflicts, and cross-story work that breaks independent delivery