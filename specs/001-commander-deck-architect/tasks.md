# Tasks: Commander Synergy Sphere

**Input**: Design documents from `/specs/001-commander-deck-architect/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, quickstart.md, contracts/deck-workspace-api.yaml

**Tests**: Automated tests are required for behavior changes, rules validation, metadata persistence, JSON contracts, security-sensitive flows, and interactive workspace behavior.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this belongs to (e.g., [US1], [US2], [US3])
- Include exact file paths in descriptions

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Align the solution, package references, and baseline test projects with the planned Clean Architecture implementation.

- [ ] T001 Update solution entries in src/CommandSynergy.slnx for the Domain, Application, Infrastructure, and test projects required by the feature plan
- [ ] T002 [P] Align src/CommandSynergy.Domain/CommandSynergy.Domain.csproj with net10.0, nullable, analyzers, and domain-only dependencies
- [ ] T003 [P] Align src/CommandSynergy.Application/CommandSynergy.Application.csproj with references to src/CommandSynergy.Domain/CommandSynergy.Domain.csproj and required package baselines
- [ ] T004 [P] Align src/CommandSynergy.Infrastructure/CommandSynergy.Infrastructure.csproj with references to src/CommandSynergy.Application/CommandSynergy.Application.csproj, src/CommandSynergy.Domain/CommandSynergy.Domain.csproj, and Parquet.Net/System.Net.Http.Json packages
- [ ] T005 [P] Align tests/CommandSynergy.Domain.Tests/CommandSynergy.Domain.Tests.csproj, tests/CommandSynergy.Application.Tests/CommandSynergy.Application.Tests.csproj, tests/CommandSynergy.Infrastructure.Tests/CommandSynergy.Infrastructure.Tests.csproj, and tests/CommandSynergy.WebUI.Tests/CommandSynergy.WebUI.Tests.csproj with xUnit, bUnit, and FluentAssertions baselines

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Establish shared contracts, configuration, metadata plumbing, and validation boundaries that block all user stories.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [ ] T006 Create and align shared deck workspace DTO contracts in src/CommandSynergy.Application/Contracts/DeckWorkspaceContracts.cs with specs/001-commander-deck-architect/contracts/deck-workspace-api.yaml
- [ ] T007 [P] Create and validate metadata and bracket option models in src/CommandSynergy.Application/Configuration/CardMetadataOptions.cs and src/CommandSynergy.Application/Configuration/BracketOptions.cs
- [ ] T008 [P] Create application abstractions in src/CommandSynergy.Application/Abstractions/ICardSearchService.cs, src/CommandSynergy.Application/Abstractions/IDeckValidationService.cs, src/CommandSynergy.Application/Abstractions/IDeckAnalysisService.cs, and src/CommandSynergy.Application/Abstractions/IDeckAdviceService.cs
- [ ] T009 [P] Implement typed Scryfall client scaffolding with resiliency and validation in src/CommandSynergy.Infrastructure/Scryfall/ScryfallClient.cs and src/CommandSynergy.Infrastructure/Scryfall/ScryfallServiceCollectionExtensions.cs
- [ ] T010 Implement Parquet snapshot loading, mutation scaffolding, and derived search-index generation baselines in src/CommandSynergy.Infrastructure/CardMetadata/ParquetCardMetadataStore.cs and src/CommandSynergy.Infrastructure/CardMetadata/SearchIndexSnapshotBuilder.cs
- [ ] T011 Implement application and infrastructure dependency registration in src/CommandSynergy.Application/DependencyInjection.cs, src/CommandSynergy.Infrastructure/DependencyInjection.cs, and src/CommandSynergy/Program.cs
- [ ] T012 Create foundational integration coverage for DI wiring, typed HTTP registration, Parquet store registration, and contract serialization in tests/CommandSynergy.Infrastructure.Tests/DependencyInjectionTests.cs and tests/CommandSynergy.WebUI.Tests/ContractSerializationTests.cs

**Checkpoint**: Foundation ready - user story implementation can now begin in priority order

---

## Phase 3: User Story 1 - Build A Legal Commander Deck (Priority: P1) 🎯 MVP

**Goal**: Let a user choose only legal commanders, assemble a Commander deck, persist locally learned card metadata, and receive authoritative legality feedback for deck size, singleton, color identity, companion rules, and multi-face handling.

**Independent Test**: Search for commander candidates, verify illegal commanders cannot be selected, build decks with valid and invalid combinations, and confirm repeated requests increasingly resolve from local Parquet metadata instead of Scryfall.

### Tests for User Story 1

- [ ] T013 [P] [US1] Add commander rules unit tests for commander eligibility, 100-card validation, singleton, color identity, companion, and MDFC handling in tests/CommandSynergy.Domain.Tests/Rules/CommanderRulesTests.cs
- [ ] T014 [P] [US1] Add deck aggregate unit tests for commander and companion selection invariants in tests/CommandSynergy.Domain.Tests/Decks/DeckAggregateTests.cs
- [ ] T015 [P] [US1] Add application service tests for commander-aware search and deck validation orchestration in tests/CommandSynergy.Application.Tests/Cards/CardSearchServiceTests.cs and tests/CommandSynergy.Application.Tests/Decks/DeckValidationServiceTests.cs
- [ ] T016 [P] [US1] Add infrastructure tests for Parquet snapshot upserts and Scryfall write-through behavior in tests/CommandSynergy.Infrastructure.Tests/CardMetadata/CardMetadataQueryServiceTests.cs and tests/CommandSynergy.Infrastructure.Tests/CardMetadata/ParquetCardMetadataStoreTests.cs
- [ ] T017 [P] [US1] Add host and component tests for commander selection rejection and validation endpoint feedback in tests/CommandSynergy.WebUI.Tests/Endpoints/CardSearchEndpointTests.cs, tests/CommandSynergy.WebUI.Tests/Endpoints/DeckValidationEndpointTests.cs, and tests/CommandSynergy.WebUI.Tests/Components/DeckWorkspaceStateTests.cs

### Implementation for User Story 1

- [ ] T018 [P] [US1] Extend card metadata models with commander eligibility and local-source fields in src/CommandSynergy.Domain/Cards/CardProfile.cs and src/CommandSynergy.Domain/Cards/CommanderEligibilityBasis.cs
- [ ] T019 [P] [US1] Update deck aggregate and entry behaviors for stricter commander and companion selection invariants in src/CommandSynergy.Domain/Decks/Deck.cs and src/CommandSynergy.Domain/Decks/DeckEntry.cs
- [ ] T020 [P] [US1] Extend validation result models for commander-selection findings in src/CommandSynergy.Domain/Rules/ValidationFinding.cs and src/CommandSynergy.Domain/Rules/DeckValidationResult.cs
- [ ] T021 [US1] Implement official commander eligibility and exception-aware legality rules in src/CommandSynergy.Domain/Rules/CommanderRules.cs
- [ ] T022 [US1] Implement commander-aware search and deck validation orchestration in src/CommandSynergy.Application/Cards/CardSearchService.cs and src/CommandSynergy.Application/Decks/DeckValidationService.cs
- [ ] T023 [US1] Implement Scryfall-to-domain commander metadata mapping and id-based Parquet upsert behavior in src/CommandSynergy.Infrastructure/Scryfall/ScryfallCardMapper.cs, src/CommandSynergy.Infrastructure/CardMetadata/ParquetCardMetadataStore.cs, and src/CommandSynergy.Infrastructure/CardMetadata/CardMetadataQueryService.cs
- [ ] T024 [US1] Preserve existing card-search and deck-validation contract shapes while implementing commander-eligibility enforcement in src/CommandSynergy.Application/Contracts/DeckWorkspaceContracts.cs, src/CommandSynergy/Endpoints/CardSearchEndpoints.cs, and src/CommandSynergy/Endpoints/DeckValidationEndpoints.cs
- [ ] T025 [US1] Implement commander-selection loading, empty, validation, and recovery states in src/CommandSynergy.Application/Decks/DeckWorkspaceStateFactory.cs, src/CommandSynergy.Client/Services/CardSearchIndexClient.cs, and src/CommandSynergy.Client/Services/DeckWorkspaceClient.cs
- [ ] T026 [US1] Add structured logging, recovery handling, and performance instrumentation for local metadata enrichment in src/CommandSynergy.Infrastructure/Observability/CardSearchLoggingDecorator.cs and src/CommandSynergy.Infrastructure/Observability/DeckValidationLoggingDecorator.cs

**Checkpoint**: User Story 1 should be fully functional and testable independently as the MVP

---

## Phase 4: User Story 2 - Analyze Power And Synergy (Priority: P2)

**Goal**: Calculate and explain bracket and synergy outcomes for a submitted deck using the official bracket framework, configured game changers, and commander-specific versus staple-based signals.

**Independent Test**: Submit a deck with known weighted cards and staple patterns, then verify bracket level, factor explanations, and synergy interpretations while ensuring missing metadata is reported without invalidating legal deck construction.

### Tests for User Story 2

- [ ] T027 [P] [US2] Add bracket engine unit tests for weight mapping, factor accumulation, and bracket boundaries in tests/CommandSynergy.Domain.Tests/Analysis/BracketEngineTests.cs
- [ ] T028 [P] [US2] Add synergy scoring unit tests for commander-specific hits, staple overload detection, and score normalization in tests/CommandSynergy.Application.Tests/Analysis/SynergyScoringServiceTests.cs
- [ ] T029 [P] [US2] Add application service tests for analysis orchestration and missing-metadata handling in tests/CommandSynergy.Application.Tests/Analysis/DeckAnalysisServiceTests.cs
- [ ] T030 [P] [US2] Add host integration tests for POST /api/decks/analyze in tests/CommandSynergy.WebUI.Tests/Endpoints/DeckAnalysisEndpointTests.cs
- [ ] T030a [P] [US2] Add bUnit tests for analysis loading, empty, error, and recovery states in tests/CommandSynergy.WebUI.Tests/Components/DeckWorkspaceStateTests.cs and tests/CommandSynergy.WebUI.Tests/Components/AnalysisPanelTests.cs

### Implementation for User Story 2

- [ ] T031 [P] [US2] Create and align analysis domain models in src/CommandSynergy.Domain/Analysis/BracketAssessment.cs, src/CommandSynergy.Domain/Analysis/BracketFactor.cs, and src/CommandSynergy.Domain/Analysis/SynergyAssessment.cs
- [ ] T032 [P] [US2] Implement weighted bracket configuration and game changer catalog support in src/CommandSynergy.Application/Analysis/GameChangerCatalog.cs and src/CommandSynergy.Infrastructure/Configuration/BracketCatalogLoader.cs
- [ ] T033 [US2] Implement bracket calculation service with non-blocking Game Changer guidance in src/CommandSynergy.Application/Analysis/BracketCalculationService.cs
- [ ] T034 [US2] Implement synergy scoring and explanation generation in src/CommandSynergy.Application/Analysis/SynergyScoringService.cs and src/CommandSynergy.Application/Analysis/AnalysisExplanationBuilder.cs
- [ ] T035 [US2] Implement deck analysis orchestration and endpoint wiring in src/CommandSynergy.Application/Analysis/DeckAnalysisService.cs and src/CommandSynergy/Endpoints/DeckAnalysisEndpoints.cs
- [ ] T035a [US2] Implement user-facing analysis loading, empty, error, and recovery states in src/CommandSynergy/Components/Decks/DeckWorkspace.razor, src/CommandSynergy/Components/Decks/DeckWorkspaceViewModel.cs, and src/CommandSynergy/Components/Decks/AnalysisPanel.razor
- [ ] T036 [US2] Add analysis caching, telemetry, and stale-metadata recovery handling in src/CommandSynergy.Infrastructure/Analysis/DeckAnalysisCache.cs and src/CommandSynergy.Infrastructure/Analysis/AnalysisTelemetry.cs

**Checkpoint**: User Stories 1 and 2 should both work independently with authoritative legality and analysis services

---

## Phase 5: User Story 3 - Organize And Inspect Cards In An Interactive Workspace (Priority: P3)

**Goal**: Provide an interactive workspace that supports pile sorting, commander-safe card selection, alternate-face inspection, and at-a-glance salt indicators while staying synchronized with validation and analysis.

**Independent Test**: Load a deck into the workspace, move cards between piles, inspect a multi-face card, attempt to choose an invalid commander, and confirm the workspace preserves state while surfacing search, validation, and analysis feedback.

### Tests for User Story 3

- [ ] T037 [P] [US3] Add bUnit tests for workspace loading, empty, error, and invalid-commander states in tests/CommandSynergy.WebUI.Tests/Components/DeckWorkspaceStateTests.cs
- [ ] T038 [P] [US3] Add bUnit tests for pile drag-and-drop behavior in tests/CommandSynergy.WebUI.Tests/Components/PileBoardTests.cs
- [ ] T039 [P] [US3] Add bUnit tests for commander card inspection, alternate-face handling, and salt badge rendering in tests/CommandSynergy.WebUI.Tests/Components/CommanderCardTests.cs

### Implementation for User Story 3

- [ ] T040 [P] [US3] Register MudBlazor and client workspace services needed for synchronized deck interactions in src/CommandSynergy/Program.cs and src/CommandSynergy.Client/Program.cs
- [ ] T041 [P] [US3] Implement workspace shell and page composition for commander selection and deck editing in src/CommandSynergy/Components/Pages/Home.razor and src/CommandSynergy/Components/Pages/Home.razor.cs
- [ ] T042 [P] [US3] Implement drag-and-drop pile workspace components with touch support in src/CommandSynergy/Components/Decks/DeckWorkspace.razor and src/CommandSynergy/Components/Decks/PileBoard.razor
- [ ] T043 [P] [US3] Implement commander card display with alternate-face handling and salt badge behavior in src/CommandSynergy/Components/Cards/CommanderCard.razor and src/CommandSynergy/Components/Cards/CommanderCard.razor.css
- [ ] T044 [US3] Implement client search-index loading and commander-safe deck synchronization in src/CommandSynergy.Client/Services/CardSearchIndexClient.cs and src/CommandSynergy.Client/Services/DeckWorkspaceClient.cs
- [ ] T045 [US3] Connect workspace interactions to validation and analysis refresh flows, including invalid commander recovery UX, in src/CommandSynergy/Components/Decks/DeckWorkspaceViewModel.cs

**Checkpoint**: All user stories should now be independently functional with the interactive workspace layered on top of proven domain and analysis services

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [ ] T046 [P] Update feature documentation and architecture notes in README.md and specs/001-commander-deck-architect/quickstart.md
- [ ] T047 Review code cleanup and namespace consistency across src/CommandSynergy.Domain, src/CommandSynergy.Application, and src/CommandSynergy.Infrastructure
- [ ] T048 Validate search, validation, analysis, and metadata-enrichment performance budgets in tests/CommandSynergy.Infrastructure.Tests/Performance/CardSearchPerformanceTests.cs and tests/CommandSynergy.Application.Tests/Performance/DeckAnalysisPerformanceTests.cs
- [ ] T049 [P] Add automated regression coverage for contract compatibility and OWASP-relevant failure cases in tests/CommandSynergy.WebUI.Tests/Security/DeckWorkspaceSecurityTests.cs
- [ ] T049a Perform an explicit OWASP-focused security review covering external Scryfall communication, Parquet snapshot persistence, request validation, and audit visibility; record findings and resolve or waive all high-severity issues before implementation sign-off
- [ ] T050 Review structured logging, timeout, retry, fallback, and file-write behavior across src/CommandSynergy.Infrastructure/Scryfall and src/CommandSynergy.Infrastructure/CardMetadata
- [ ] T051 Validate the quickstart workflow and representative metadata-enrichment path in specs/001-commander-deck-architect/quickstart.md
- [ ] T052 [P] Refresh agent context and repository guidance in .github/copilot-instructions.md after implementation changes land

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - blocks all user stories
- **User Story 1 (Phase 3)**: Depends on Foundational completion - delivers the MVP
- **User Story 2 (Phase 4)**: Depends on User Story 1 domain, metadata, and validation foundations
- **User Story 3 (Phase 5)**: Depends on User Story 1 endpoints and commander-selection/search flows and benefits from User Story 2 analysis responses
- **Polish (Phase 6)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational - no dependency on other stories
- **User Story 2 (P2)**: Depends on deck aggregate, metadata access, and validation contracts established in US1
- **User Story 3 (P3)**: Depends on US1 search, validation, and commander-selection contracts, and integrates best with US2 analysis responses

### Within Each User Story

- Tests MUST be written and fail before implementation
- Domain models before application services
- Application services before endpoints and interactive UI wiring
- Core implementation before observability and recovery refinements
- Story complete before moving to the next priority unless a tracked dependency requires overlap

### Parallel Opportunities

- Setup project alignment tasks marked [P] can run in parallel
- Foundational abstraction and configuration tasks marked [P] can run in parallel
- Test tasks within each story marked [P] can run in parallel
- Distinct model, service, and UI files within a story marked [P] can run in parallel when they do not share the same file

---

## Parallel Example: User Story 1

```text
Task: "Add commander rules unit tests for commander eligibility, 100-card validation, singleton, color identity, companion, and MDFC handling in tests/CommandSynergy.Domain.Tests/Rules/CommanderRulesTests.cs"
Task: "Add application service tests for commander-aware search and deck validation orchestration in tests/CommandSynergy.Application.Tests/Cards/CardSearchServiceTests.cs and tests/CommandSynergy.Application.Tests/Decks/DeckValidationServiceTests.cs"
Task: "Add infrastructure tests for Parquet snapshot upserts and Scryfall write-through behavior in tests/CommandSynergy.Infrastructure.Tests/CardMetadata/CardMetadataQueryServiceTests.cs and tests/CommandSynergy.Infrastructure.Tests/CardMetadata/ParquetCardMetadataStoreTests.cs"

Task: "Extend card metadata models with commander eligibility and local-source fields in src/CommandSynergy.Domain/Cards/CardProfile.cs and src/CommandSynergy.Domain/Cards/CommanderEligibilityBasis.cs"
Task: "Update deck aggregate and entry behaviors for stricter commander and companion selection invariants in src/CommandSynergy.Domain/Decks/Deck.cs and src/CommandSynergy.Domain/Decks/DeckEntry.cs"
```

## Parallel Example: User Story 2

```text
Task: "Add bracket engine unit tests for weight mapping, factor accumulation, and bracket boundaries in tests/CommandSynergy.Domain.Tests/Analysis/BracketEngineTests.cs"
Task: "Add synergy scoring unit tests for commander-specific hits, staple overload detection, and score normalization in tests/CommandSynergy.Application.Tests/Analysis/SynergyScoringServiceTests.cs"

Task: "Create and align analysis domain models in src/CommandSynergy.Domain/Analysis/BracketAssessment.cs, src/CommandSynergy.Domain/Analysis/BracketFactor.cs, and src/CommandSynergy.Domain/Analysis/SynergyAssessment.cs"
Task: "Implement weighted bracket configuration and game changer catalog support in src/CommandSynergy.Application/Analysis/GameChangerCatalog.cs and src/CommandSynergy.Infrastructure/Configuration/BracketCatalogLoader.cs"
```

## Parallel Example: User Story 3

```text
Task: "Add bUnit tests for workspace loading, empty, error, and invalid-commander states in tests/CommandSynergy.WebUI.Tests/Components/DeckWorkspaceStateTests.cs"
Task: "Add bUnit tests for pile drag-and-drop behavior in tests/CommandSynergy.WebUI.Tests/Components/PileBoardTests.cs"
Task: "Add bUnit tests for commander card inspection, alternate-face handling, and salt badge rendering in tests/CommandSynergy.WebUI.Tests/Components/CommanderCardTests.cs"

Task: "Implement workspace shell and page composition for commander selection and deck editing in src/CommandSynergy/Components/Pages/Home.razor and src/CommandSynergy/Components/Pages/Home.razor.cs"
Task: "Implement drag-and-drop pile workspace components with touch support in src/CommandSynergy/Components/Decks/DeckWorkspace.razor and src/CommandSynergy/Components/Decks/PileBoard.razor"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational
3. Complete Phase 3: User Story 1
4. **STOP and VALIDATE**: Confirm commander selection, deck legality, and local metadata write-through work independently
5. Demo the legal deck-building flow before analysis or workspace polish

### Incremental Delivery

1. Complete Setup + Foundational → Clean Architecture baseline ready
2. Add User Story 1 → Test independently → MVP ready
3. Add User Story 2 → Test independently → analysis ready
4. Add User Story 3 → Test independently → interactive workspace ready
5. Finish with Polish → performance, security, and regression hardening complete

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together
2. Once Foundational is complete:
   - Developer A: User Story 1 legality, commander selection, and metadata write-through
   - Developer B: User Story 2 bracket and synergy services after US1 contracts stabilize
   - Developer C: User Story 3 workspace and bUnit coverage after US1 endpoint contracts stabilize

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps each task to a specific user story for traceability
- Each user story should remain independently testable at its checkpoint
- Verify tests fail before implementing
- Verify security, UX, and performance tasks are not skipped when the story affects them
- Avoid vague tasks, same-file parallel conflicts, and cross-story work that breaks independent delivery