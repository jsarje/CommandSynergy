# Research: Commander Synergy Sphere

## Decision 1: Use the existing server project as the WebUI host and add explicit Clean Architecture class libraries

- **Decision**: Keep `src/CommandSynergy` as the ASP.NET Core Blazor Web App host, keep
  `src/CommandSynergy.Client` for Interactive Auto client services, and add
  `CommandSynergy.Domain`, `CommandSynergy.Application`, and `CommandSynergy.Infrastructure` as new
  projects in the solution.
- **Rationale**: This preserves the working .NET 10 Interactive Auto baseline, minimizes migration
  cost, and still gives strict dependency boundaries for domain logic, use cases, and external
  integrations.
- **Alternatives considered**:
  - Rename the host to a new `WebUI` project immediately: rejected because it adds churn before the
    feature foundation exists.
  - Keep all code in the current server project with folders only: rejected because it weakens
    dependency enforcement and makes testing boundaries easier to break.

## Decision 2: Make commander legality, bracket scoring, and synergy scoring server-authoritative

- **Decision**: Put commander rules and scoring logic in Domain and Application layers and execute
  them through server-owned services and JSON endpoints instead of trusting client-side only
  computation.
- **Rationale**: Authoritative server evaluation prevents divergence between render modes, keeps
  business rules testable, and limits tampering risk for bracket and synergy outcomes.
- **Alternatives considered**:
  - Run all calculations in WebAssembly for maximum local responsiveness: rejected because it risks
    inconsistent rules and duplicates logic across server and client paths.
  - Keep the rules inside Razor components: rejected because it couples UI updates to domain logic
    and undermines testability.

## Decision 3: Treat Parquet as the authoritative local bulk store, but ship a derived lightweight client search index

- **Decision**: Read and refresh Parquet data on the server through Infrastructure services, then
  generate a reduced search artifact for the Interactive Auto client instead of querying Parquet
  directly in the browser.
- **Rationale**: Browser-side Parquet parsing is a poor fit for WASM memory budgets and startup
  costs, while a curated search artifact preserves fast client search and keeps the authoritative
  dataset server-owned.
- **Alternatives considered**:
  - Query Parquet directly in WebAssembly: rejected because of startup cost, memory pressure, and
    browser compatibility risk.
  - Route every card search to the server: rejected for the first interaction path because it adds
    unnecessary latency to a high-frequency deck-building workflow.

## Decision 4: Use Scryfall as the upstream metadata source behind typed Infrastructure clients with caching and graceful degradation

- **Decision**: Implement typed `HttpClient`-based Scryfall adapters in Infrastructure, cache bulk
  metadata snapshots and lookup results, and allow analysis and search to continue from cached
  snapshots during upstream outages.
- **Rationale**: This keeps external HTTP concerns out of Domain and Application, reduces repeated
  Scryfall dependency pressure, and supports the constitution's availability and failure-state
  expectations.
- **Alternatives considered**:
  - Call Scryfall directly from components: rejected because it scatters HTTP concerns and weakens
    security and resiliency controls.
  - Depend only on Scryfall live queries with no local cache: rejected because it makes search and
    deck analysis too fragile and too slow.

## Decision 5: Phase implementation domain-first, then infrastructure, then UI workspace behavior

- **Decision**: Deliver in this order: solution and project scaffolding, domain entities and rules,
  application use cases, infrastructure for Scryfall and Parquet, minimal JSON contracts, and only
  then MudBlazor workspace components and card animations.
- **Rationale**: The commander rules engine, bracket engine, and synergy scoring are the product's
  correctness core and should be proven before drag-and-drop polish.
- **Alternatives considered**:
  - Build the MudBlazor dashboard first: rejected because it risks locking the UI to unstable data
    contracts and under-tested business rules.
  - Build infrastructure before domain: rejected because the integration layer needs stable domain
    models and service abstractions to target.

## Decision 6: Use layered automated tests aligned with the constitution

- **Decision**: Use xUnit for Domain and Application tests, targeted integration tests for host and
  Infrastructure adapters, and bUnit for WebUI component behavior.
- **Rationale**: This gives direct proof for commander rule enforcement and scoring logic while also
  covering UI state transitions and integration failure handling.
- **Alternatives considered**:
  - Only end-to-end UI tests: rejected because they are too slow and too coarse to prove core deck
    logic safely.
  - Only unit tests: rejected because external data adapters, JSON contracts, and Razor interaction
    states still need integration coverage.