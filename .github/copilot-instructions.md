# CommandSynergy Development Guidelines

Auto-generated from all feature plans. Last updated: 2026-04-17

## Active Technologies

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

- 001-build-commander-architect: Added Clean Architecture planning for Blazor Interactive Auto, Parquet-backed card metadata, and commander analysis services

<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->
