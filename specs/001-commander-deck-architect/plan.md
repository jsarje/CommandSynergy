# Implementation Plan: Commander Synergy Sphere

**Branch**: `[001-build-commander-architect]` | **Date**: 2026-04-17 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-commander-deck-architect/spec.md`

## Summary

Build the domain and infrastructure foundation for a Commander deck architect on the existing .NET
10 Blazor Web App baseline by introducing Clean Architecture projects for Domain, Application, and
Infrastructure while keeping the current server project as the WebUI host and the existing client
project for Interactive Auto client services. Phase 1 prioritizes authoritative commander-rules
validation, bracket calculation, synergy scoring, Scryfall-backed card metadata ingestion, and a
derived client search index generated from server-owned Parquet data; MudBlazor workspace features
and custom card animations are intentionally deferred until these services are stable.

## Technical Context

**Language/Version**: .NET 10 / C# 14  
**Primary Dependencies**: ASP.NET Core Blazor Web App, Interactive Auto render mode, MudBlazor,
System.Net.Http.Json, Parquet.Net, xUnit, bUnit, FluentAssertions  
**Storage**: Authoritative local Parquet card metadata snapshots on the server, in-memory or
distributed cache for derived search artifacts and analysis inputs, client-side session storage for
draft workspace state  
**Testing**: xUnit for domain and application logic, ASP.NET Core integration tests for host and
JSON endpoints, bUnit for Razor components, focused infrastructure tests for Scryfall and Parquet
adapters  
**Target Platform**: ASP.NET Core hosted Blazor Web App with Interactive Auto and WebAssembly
client interactivity on modern desktop and tablet browsers
**Project Type**: web-app  
**Performance Goals**: card search results in <200ms p95 after the client search index is loaded;
deck validation and bracket or synergy recomputation in <2s p95; drag-and-drop pile moves reflect
locally in <100ms perceived latency  
**Constraints**: domain rules and scoring remain authoritative on the server; client does not query
Parquet files directly; Scryfall outages must degrade gracefully through cached data; loading,
empty, validation, error, and recovery UX states are mandatory; OWASP-aware controls are required
for external HTTP access, input handling, dependency management, and state mutation  
**Scale/Scope**: initial release supports one active deck-building session per user, a full
Commander-legal card catalog, bracket and synergy analysis for 100-card decks, and a single hosted
web application with cleanly separated layers
**Security/Privacy Review**: no secrets in source control; outbound Scryfall access is wrapped in
typed clients with timeout, retry, and response validation; antiforgery remains enabled for server
mutations; request validation, dependency updates, logging of external failures, and SSRF-aware
URL handling apply to all infrastructure integrations

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Pre-Research Gate

- PASS: .NET quality approach is explicit. New work is isolated into Domain, Application, and
  Infrastructure projects with nullable reference types, analyzers, and standard ASP.NET Core DI.
- PASS: Test layers are explicit. Domain and application logic are covered with xUnit, Blazor UI
  with bUnit, and host or endpoint behavior with integration tests.
- PASS: UX state coverage is defined. Planning explicitly includes loading, empty, validation,
  error, and recovery states for search, deck validation, analysis, and workspace actions.
- PASS: Performance budgets are explicit for search, validation, analysis, and drag-and-drop.
- PASS: Security review scope is defined for Scryfall HTTP calls, cached metadata, user state, and
  dependency selection with OWASP-aware controls.
- PASS: No constitutional exceptions are required for this plan.

### Post-Design Gate

- PASS: Design keeps pure commander rules, bracket logic, and synergy scoring in Domain or
  Application boundaries and avoids leaking HTTP or UI concerns into those layers.
- PASS: Phase 1 artifacts prioritize core logic and infrastructure before MudBlazor-heavy UI work,
  which aligns with the constitution's testability and performance requirements.
- PASS: The selected Parquet strategy preserves server authority while still meeting fast client
  search expectations through a derived search artifact instead of browser-side Parquet parsing.
- PASS: Contract design exposes only the minimum JSON surface needed for client search, validation,
  and analysis, keeping sensitive logic and data preparation server-side.
- PASS: No unresolved clarification remains that would block Phase 2 task generation.

## Project Structure

### Documentation (this feature)

```text
specs/001-commander-deck-architect/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── deck-workspace-api.yaml
└── tasks.md
```

### Source Code (repository root)

```text
src/
├── CommandSynergy.slnx
├── CommandSynergy/                     # WebUI host and Razor component shell
│   ├── Components/
│   ├── Program.cs
│   └── CommandSynergy.csproj
├── CommandSynergy.Client/              # Interactive Auto client bootstrap and client-only services
│   ├── Program.cs
│   └── CommandSynergy.Client.csproj
├── CommandSynergy.Domain/              # Pure commander rules, entities, value objects
├── CommandSynergy.Application/         # Use cases, orchestration, service abstractions
└── CommandSynergy.Infrastructure/      # Scryfall client, Parquet access, caching, adapters

tests/
├── CommandSynergy.Domain.Tests/
├── CommandSynergy.Application.Tests/
├── CommandSynergy.Infrastructure.Tests/
└── CommandSynergy.WebUI.Tests/
```

**Structure Decision**: Extend the current two-project solution instead of replacing it. The
existing `src/CommandSynergy` project becomes the WebUI host for Razor components and server-side
composition, `src/CommandSynergy.Client` remains the Interactive Auto client entry point, and new
Domain, Application, and Infrastructure class libraries are added beside them so dependency
direction stays Clean Architecture-compliant with minimal disruption to the current baseline.

## Complexity Tracking

No constitutional violations or exceptional complexity waivers are currently required.
