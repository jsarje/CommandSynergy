# Tasks: Commander Synergy Sphere

**Input**: Design documents from `/specs/001-commander-deck-architect/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, quickstart.md, contracts/deck-workspace-api.yaml

**Tests**: Automated tests are required for behavior changes, rules validation, analysis services, JSON contracts, and interactive workspace behavior.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish the Clean Architecture solution layout, package references, and baseline test projects.

- [X] T001 Update solution entries in src/CommandSynergy.slnx for Domain, Application, Infrastructure, and test projects
- [X] T002 [P] Create src/CommandSynergy.Domain/CommandSynergy.Domain.csproj with net10.0, nullable, and analyzer settings
- [X] T003 [P] Create src/CommandSynergy.Application/CommandSynergy.Application.csproj with references to src/CommandSynergy.Domain/CommandSynergy.Domain.csproj
- [X] T004 [P] Create src/CommandSynergy.Infrastructure/CommandSynergy.Infrastructure.csproj with references to src/CommandSynergy.Application/CommandSynergy.Application.csproj and src/CommandSynergy.Domain/CommandSynergy.Domain.csproj plus Parquet.Net and System.Net.Http.Json packages
- [X] T005 [P] Create tests/CommandSynergy.Domain.Tests/CommandSynergy.Domain.Tests.csproj, tests/CommandSynergy.Application.Tests/CommandSynergy.Application.Tests.csproj, tests/CommandSynergy.Infrastructure.Tests/CommandSynergy.Infrastructure.Tests.csproj, and tests/CommandSynergy.WebUI.Tests/CommandSynergy.WebUI.Tests.csproj with xUnit, bUnit, and FluentAssertions packages

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core shared contracts, dependency registration, and metadata infrastructure that MUST be complete before any user story work can be implemented.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T006 Create shared deck workspace DTO contracts in src/CommandSynergy.Application/Contracts/DeckWorkspaceContracts.cs matching specs/001-commander-deck-architect/contracts/deck-workspace-api.yaml
- [X] T007 [P] Create metadata and analysis option models in src/CommandSynergy.Application/Configuration/CardMetadataOptions.cs and src/CommandSynergy.Application/Configuration/BracketOptions.cs
- [X] T008 [P] Create application service abstractions in src/CommandSynergy.Application/Abstractions/ICardSearchService.cs, src/CommandSynergy.Application/Abstractions/IDeckValidationService.cs, src/CommandSynergy.Application/Abstractions/IDeckAnalysisService.cs, and an extension point in src/CommandSynergy.Application/Abstractions/IDeckAdviceService.cs
- [X] T009 Implement typed Scryfall client scaffolding with resiliency and validation in src/CommandSynergy.Infrastructure/Scryfall/ScryfallClient.cs and src/CommandSynergy.Infrastructure/Scryfall/ScryfallServiceCollectionExtensions.cs
- [X] T010 Implement Parquet snapshot loading and derived search index generation skeleton in src/CommandSynergy.Infrastructure/CardMetadata/ParquetCardMetadataStore.cs and src/CommandSynergy.Infrastructure/CardMetadata/SearchIndexSnapshotBuilder.cs
- [X] T011 Implement application and infrastructure dependency registration in src/CommandSynergy.Application/DependencyInjection.cs, src/CommandSynergy.Infrastructure/DependencyInjection.cs, and update src/CommandSynergy/Program.cs
- [X] T012 Create integration test coverage for DI wiring, typed HTTP client registration, and contract serialization in tests/CommandSynergy.Infrastructure.Tests/DependencyInjectionTests.cs and tests/CommandSynergy.WebUI.Tests/ContractSerializationTests.cs

**Checkpoint**: Foundation ready - user story implementation can now begin in priority order

---

## Phase 3: User Story 1 - Build A Legal Commander Deck (Priority: P1) 🎯 MVP

**Goal**: Let a user search cards, assemble a commander deck, and receive authoritative legality feedback for deck size, singleton, color identity, companion rules, and multi-face card handling.

**Independent Test**: Search for a commander and supporting cards, submit a deck snapshot with valid and invalid combinations, and verify the API returns correct legality results and user-readable findings.

### Tests for User Story 1

- [X] T013 [P] [US1] Add commander rules unit tests in tests/CommandSynergy.Domain.Tests/Rules/CommanderRulesTests.cs for 100-card, singleton, color identity, companion, and MDFC validation
- [X] T014 [P] [US1] Add deck aggregate unit tests in tests/CommandSynergy.Domain.Tests/Decks/DeckAggregateTests.cs for commander selection, entry management, and pile assignment invariants
- [X] T015 [P] [US1] Add application service tests for deck validation and card search orchestration in tests/CommandSynergy.Application.Tests/Decks/DeckValidationServiceTests.cs and tests/CommandSynergy.Application.Tests/Cards/CardSearchServiceTests.cs
- [X] T016 [P] [US1] Add host integration tests for GET /api/cards/search and POST /api/decks/validate in tests/CommandSynergy.WebUI.Tests/Endpoints/CardSearchEndpointTests.cs and tests/CommandSynergy.WebUI.Tests/Endpoints/DeckValidationEndpointTests.cs

### Implementation for User Story 1

- [X] T017 [P] [US1] Create domain entities and value objects in src/CommandSynergy.Domain/Decks/Deck.cs, src/CommandSynergy.Domain/Decks/DeckEntry.cs, src/CommandSynergy.Domain/Decks/Pile.cs, src/CommandSynergy.Domain/Cards/CardProfile.cs, and src/CommandSynergy.Domain/Cards/CardFaceProfile.cs
- [X] T018 [P] [US1] Create legality result models in src/CommandSynergy.Domain/Rules/ValidationFinding.cs and src/CommandSynergy.Domain/Rules/DeckValidationResult.cs
- [X] T019 [US1] Implement commander rules domain service in src/CommandSynergy.Domain/Rules/CommanderRules.cs
- [X] T020 [US1] Implement card search and deck validation application services in src/CommandSynergy.Application/Cards/CardSearchService.cs and src/CommandSynergy.Application/Decks/DeckValidationService.cs
- [X] T021 [US1] Implement Scryfall-to-domain mapping and Parquet-backed metadata queries in src/CommandSynergy.Infrastructure/Scryfall/ScryfallCardMapper.cs and src/CommandSynergy.Infrastructure/CardMetadata/CardMetadataQueryService.cs
- [X] T022 [US1] Implement minimal JSON endpoints for card search and deck validation in src/CommandSynergy/Endpoints/CardSearchEndpoints.cs and src/CommandSynergy/Endpoints/DeckValidationEndpoints.cs
- [X] T023 [US1] Add loading, empty, validation, and recovery state handling models for deck-building responses in src/CommandSynergy.Application/Decks/DeckWorkspaceStateFactory.cs
- [X] T024 [US1] Add security, logging, and performance instrumentation for search and validation in src/CommandSynergy.Infrastructure/Observability/CardSearchLoggingDecorator.cs and src/CommandSynergy.Infrastructure/Observability/DeckValidationLoggingDecorator.cs

**Checkpoint**: At this point, User Story 1 should be fully functional and testable independently as the MVP

---

## Phase 4: User Story 2 - Analyze Power And Synergy (Priority: P2)

**Goal**: Calculate and explain bracket and synergy outcomes for a submitted deck using the 2026 bracket model and commander-specific versus staple-based card signals.

**Independent Test**: Submit a deck with weighted high-impact cards and known staple patterns, then verify bracket level, factor explanations, and synergy interpretations match expected outcomes.

### Tests for User Story 2

- [X] T025 [P] [US2] Add bracket engine unit tests in tests/CommandSynergy.Domain.Tests/Analysis/BracketEngineTests.cs for weight mapping, factor accumulation, and 1-5 level boundaries
- [X] T026 [P] [US2] Add synergy scoring unit tests in tests/CommandSynergy.Application.Tests/Analysis/SynergyScoringServiceTests.cs for commander-specific hits, staple overload detection, and score normalization
- [X] T027 [P] [US2] Add application service tests for analysis orchestration in tests/CommandSynergy.Application.Tests/Analysis/DeckAnalysisServiceTests.cs
- [X] T028 [P] [US2] Add host integration tests for POST /api/decks/analyze in tests/CommandSynergy.WebUI.Tests/Endpoints/DeckAnalysisEndpointTests.cs

### Implementation for User Story 2

- [X] T029 [P] [US2] Create analysis domain models in src/CommandSynergy.Domain/Analysis/BracketAssessment.cs, src/CommandSynergy.Domain/Analysis/BracketFactor.cs, and src/CommandSynergy.Domain/Analysis/SynergyAssessment.cs
- [X] T030 [P] [US2] Create weighted bracket configuration and game changer catalog support in src/CommandSynergy.Application/Analysis/GameChangerCatalog.cs and src/CommandSynergy.Infrastructure/Configuration/BracketCatalogLoader.cs
- [X] T031 [US2] Implement bracket calculation service in src/CommandSynergy.Application/Analysis/BracketCalculationService.cs
- [X] T032 [US2] Implement synergy scoring service and explanation builder in src/CommandSynergy.Application/Analysis/SynergyScoringService.cs and src/CommandSynergy.Application/Analysis/AnalysisExplanationBuilder.cs
- [X] T033 [US2] Implement deck analysis endpoint and orchestration in src/CommandSynergy/Endpoints/DeckAnalysisEndpoints.cs and src/CommandSynergy.Application/Analysis/DeckAnalysisService.cs
- [X] T034 [US2] Add analysis caching, structured logging, and stale-metadata recovery handling in src/CommandSynergy.Infrastructure/Analysis/DeckAnalysisCache.cs and src/CommandSynergy.Infrastructure/Analysis/AnalysisTelemetry.cs

**Checkpoint**: At this point, User Stories 1 and 2 should both work independently with authoritative legality and analysis services

---

## Phase 5: User Story 3 - Organize And Inspect Cards In An Interactive Workspace (Priority: P3)

**Goal**: Provide an interactive deck workspace that supports pile sorting, alternate-face inspection, and at-a-glance salt indicators while staying synchronized with server-owned validation and analysis.

**Independent Test**: Load a deck into the workspace, move cards between piles, inspect a multi-face card, and confirm the workspace preserves state while surfacing search, validation, and analysis feedback.

### Tests for User Story 3

- [X] T035 [P] [US3] Add bUnit tests for workspace loading, empty, and error states in tests/CommandSynergy.WebUI.Tests/Components/DeckWorkspaceStateTests.cs
- [X] T036 [P] [US3] Add bUnit tests for pile drag-and-drop behavior in tests/CommandSynergy.WebUI.Tests/Components/PileBoardTests.cs
- [X] T037 [P] [US3] Add bUnit tests for multi-face card inspection and salt badge rendering in tests/CommandSynergy.WebUI.Tests/Components/CommanderCardTests.cs

### Implementation for User Story 3

- [X] T038 [P] [US3] Register MudBlazor and client-side workspace services in src/CommandSynergy/Program.cs and src/CommandSynergy.Client/Program.cs
- [X] T039 [P] [US3] Implement workspace shell and stateful page composition in src/CommandSynergy/Components/Pages/Home.razor and src/CommandSynergy/Components/Pages/Home.razor.cs
- [X] T040 [P] [US3] Implement drag-and-drop pile workspace components in src/CommandSynergy/Components/Decks/DeckWorkspace.razor and src/CommandSynergy/Components/Decks/PileBoard.razor, ensuring touch device support and debounced rapid-move handling
- [X] T041 [P] [US3] Implement commander card display with alternate-face handling and salt badge in src/CommandSynergy/Components/Cards/CommanderCard.razor and src/CommandSynergy/Components/Cards/CommanderCard.razor.css
- [X] T042 [US3] Implement custom CSS animation and face-toggle behavior for double-faced cards in src/CommandSynergy/Components/Cards/CommanderCard.razor.css and src/CommandSynergy/Components/Cards/CommanderCard.razor.js
- [X] T043 [US3] Implement client search-index loading and deck workspace synchronization in src/CommandSynergy.Client/Services/CardSearchIndexClient.cs and src/CommandSynergy.Client/Services/DeckWorkspaceClient.cs
- [X] T044 [US3] Connect workspace interactions to validation and analysis refresh flows with loading and recovery UX in src/CommandSynergy/Components/Decks/DeckWorkspaceViewModel.cs

**Checkpoint**: All user stories should now be independently functional, with the interactive workspace layered on top of proven domain and analysis services

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [X] T045 [P] Update feature documentation and architecture notes in README.md and specs/001-commander-deck-architect/quickstart.md
- [X] T046 Run code cleanup and namespace alignment across src/CommandSynergy.Domain, src/CommandSynergy.Application, and src/CommandSynergy.Infrastructure
- [X] T047 Validate search and analysis performance budgets with focused benchmarks in tests/CommandSynergy.Infrastructure.Tests/Performance/CardSearchPerformanceTests.cs and tests/CommandSynergy.Application.Tests/Performance/DeckAnalysisPerformanceTests.cs
- [X] T048 [P] Add additional automated regression coverage for contract compatibility and OWASP-relevant failure cases in tests/CommandSynergy.WebUI.Tests/Security/DeckWorkspaceSecurityTests.cs
- [X] T049 Review structured logging, timeout, retry, and fallback behavior across Scryfall and metadata adapters in src/CommandSynergy.Infrastructure/Scryfall and src/CommandSynergy.Infrastructure/CardMetadata
- [X] T050 Run quickstart validation and update the agent context if project structure or commands changed in .github/copilot-instructions.md

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - blocks all user stories
- **User Story 1 (Phase 3)**: Depends on Foundational completion - delivers the MVP
- **User Story 2 (Phase 4)**: Depends on User Story 1 domain and contract foundations
- **User Story 3 (Phase 5)**: Depends on User Story 1 search and validation flows and benefits from User Story 2 analysis responses
- **Polish (Phase 6)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational - no dependency on other stories
- **User Story 2 (P2)**: Depends on the deck aggregate, metadata access, and validation contract established in US1
- **User Story 3 (P3)**: Depends on US1 endpoints and client search artifact, and integrates best with US2 analysis outputs but remains independently testable once those contracts exist

### Within Each User Story

- Tests MUST be written and fail before implementation
- Domain models before application services
- Application services before endpoints and interactive UI wiring
- Core implementation before observability and recovery refinements
- Story complete before moving to the next priority unless a tracked dependency requires overlap

### Parallel Opportunities

- Setup project files in different directories marked [P] can run in parallel
- Foundational abstractions and configuration models marked [P] can run in parallel
- Domain, application, and integration test files within each story marked [P] can run in parallel
- Distinct entity and component files within a story marked [P] can run in parallel when they do not share the same file

---

## Parallel Example: User Story 1

```text
Task: "Add commander rules unit tests in tests/CommandSynergy.Domain.Tests/Rules/CommanderRulesTests.cs for 100-card, singleton, color identity, companion, and MDFC validation"
Task: "Add deck aggregate unit tests in tests/CommandSynergy.Domain.Tests/Decks/DeckAggregateTests.cs for commander selection, entry management, and pile assignment invariants"
Task: "Add application service tests for deck validation and card search orchestration in tests/CommandSynergy.Application.Tests/Decks/DeckValidationServiceTests.cs and tests/CommandSynergy.Application.Tests/Cards/CardSearchServiceTests.cs"

Task: "Create domain entities and value objects in src/CommandSynergy.Domain/Decks/Deck.cs, src/CommandSynergy.Domain/Decks/DeckEntry.cs, src/CommandSynergy.Domain/Decks/Pile.cs, src/CommandSynergy.Domain/Cards/CardProfile.cs, and src/CommandSynergy.Domain/Cards/CardFaceProfile.cs"
Task: "Create legality result models in src/CommandSynergy.Domain/Rules/ValidationFinding.cs and src/CommandSynergy.Domain/Rules/DeckValidationResult.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational
3. Complete Phase 3: User Story 1
4. **STOP and VALIDATE**: Confirm card search and commander deck legality work independently
5. Demo the legal deck-building flow before analysis or workspace polish

### Incremental Delivery

1. Complete Setup + Foundational → Clean Architecture baseline ready
2. Add User Story 1 → Test independently → MVP ready
3. Add User Story 2 → Test independently → strategic analysis ready
4. Add User Story 3 → Test independently → interactive workspace ready
5. Finish with Polish → performance, security, and regression hardening complete

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together
2. Once Foundational is complete:
   - Developer A: User Story 1 domain and endpoint implementation
   - Developer B: User Story 2 scoring services and analysis contract work after US1 contracts stabilize
   - Developer C: User Story 3 component scaffolding and bUnit tests after US1 endpoint contracts stabilize

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to a specific user story for traceability
- Each user story should remain independently testable at its checkpoint
- Verify tests fail before implementing
- Verify security, UX, and performance tasks are not skipped when the story affects them
- Commit after each task or logical group
- Avoid vague tasks, same-file parallel conflicts, and cross-story work that breaks independent delivery