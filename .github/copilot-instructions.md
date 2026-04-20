# CommandSynergy Development Guidelines

Auto-generated from all feature plans. Last updated: 2026-04-19

## Active Technologies
- .NET 10 / C# 14 + ASP.NET Core Blazor Web App, Interactive Auto render mode, MudBlazor, Parquet.Net, typed HttpClient for Scryfall, xUnit, bUnit, FluentAssertions (001-commander-deck-architect)
- Local Parquet snapshot files for authoritative card metadata plus derived in-memory search index data (001-commander-deck-architect)
- .NET 10 / C# 14 + ASP.NET Core Blazor Web App with Interactive Auto render mode, (002-deck-import-export)
- Browser localStorage for persisted imported decks, active deck selection, and import (002-deck-import-export)

- .NET 10 / C# 14 + ASP.NET Core Blazor Web App, Interactive Auto render mode, MudBlazor, Parquet.Net, xUnit, bUnit (001-build-commander-architect)

## Project Structure

```text
src/
CommandSynergy.slnx
CommandSynergy/
CommandSynergy.Client/
CommandSynergy.Domain/
CommandSynergy.Application/
CommandSynergy.Infrastructure/
CommandSynergy.Ingestion/

tests/
CommandSynergy.Domain.Tests/
CommandSynergy.Application.Tests/
CommandSynergy.Infrastructure.Tests/
CommandSynergy.WebUI.Tests/
```

## Commands

dotnet restore src/CommandSynergy.slnx
dotnet build src/CommandSynergy.slnx
dotnet test src/CommandSynergy.slnx

## Code Style

.NET 10 / C# 14: Follow standard conventions and Clean Architecture dependency direction

## Recent Changes
- 002-deck-import-export: Added .NET 10 / C# 14 + ASP.NET Core Blazor Web App with Interactive Auto render mode,
- 001-commander-deck-architect: Added .NET 10 / C# 14 + ASP.NET Core Blazor Web App, Interactive Auto render mode, MudBlazor, Parquet.Net, typed HttpClient for Scryfall, xUnit, bUnit, FluentAssertions
- 001-commander-deck-architect: **Implementation complete** – all 53 tests passing; Clean Architecture layers fully wired; Parquet-backed card metadata with bulk `oracle_cards` ingestion and read-only runtime fallback (using `long? LastSyncedUtcTicks` for Parquet.Net 5.x compatibility); commander eligibility basis enum; AnalysisPanel component with loading/empty/error/ready states; OWASP review complete (all high-severity issues resolved or waived).


## Implementation Notes
- **Parquet.Net 5.2.0 compatibility**: Do NOT use `DateTimeOffset?` or `DateTimeOffset` in Parquet row types – use `long?` (UTC ticks) and convert in mapping code.
- **AnalysisPanel state order**: Check `IsLoading` first, then `HasError`, then `Analysis is null`, then ready. Putting null-check before HasError causes the error state to be invisible when `Analysis` is null.
- **Atomic Parquet upserts**: Load all rows → filter by CardId → append new row → write to `.tmp` → `File.Move(overwrite: true)`.
- **Bulk snapshot refresh**: Use `CommandSynergy.Ingestion` to download Scryfall `oracle_cards` and replace `cards.parquet` atomically; runtime fallback in `CardMetadataQueryService` is read-only and must not mutate Parquet during user requests.

<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->
