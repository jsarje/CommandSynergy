# Repository guidance

## Commands

- Restore with `dotnet restore src\CommandSynergy.slnx`.
- Build with `dotnet build src\CommandSynergy.slnx`.
- Run the automated suite with `dotnet test src\CommandSynergy.slnx`.
- Run live integration tests locally with `dotnet test src\CommandSynergy.slnx --filter "Category=LocalOnly"`.
- Refresh card metadata with `dotnet run --project src\CommandSynergy.Ingestion\CommandSynergy.Ingestion.csproj`.

## Repository-specific constraints

- `cards.parquet` is authoritative at runtime. Use the ingestion project to refresh it; runtime Scryfall fallback in `CardMetadataQueryService` is read-only.
- Parquet row types use `long?` UTC ticks for timestamps. Parquet.Net does not support `DateTimeOffset` row fields.
- In `AnalysisPanel`, render `IsLoading`, then `HasError`, then the null-analysis state, then results.
- The app is a local developer tool with no authentication. Do not treat its current HTTP endpoints as a public service.
- The test suite has offline tests and local-only integration tests. If one test project cannot cover a change, search the other projects for an existing test seam before calling the behavior untestable.

## Add an API endpoint

1. Define request and response contracts under `src\CommandSynergy.Application\Contracts\`.
2. Add the endpoint under `src\CommandSynergy\Endpoints\`.
3. Map the endpoint in `src\CommandSynergy\Program.cs`.
4. Add endpoint tests under `tests\CommandSynergy.WebUI.Tests\Endpoints\` and update `tests\CommandSynergy.WebUI.Tests\ContractSerializationTests.cs` when serialized contracts change.
5. Update the matching file under `specs\*\contracts\` and the API list in `README.md` when they document the changed behavior.
6. Run `dotnet test src\CommandSynergy.slnx`.

## Recent review feedback

Recent Copilot reviews found missing mutation coverage in the imported-deck state model (#49) and a browser recipe whose steps did not meet the export precondition (#48). When changing either, cover each state mutation in the model and verify that each recipe reaches the state it claims.

## Maintenance matrix

| Change | Also update |
|---|---|
| API endpoint or response contract | `tests\CommandSynergy.WebUI.Tests\Endpoints\`, `tests\CommandSynergy.WebUI.Tests\ContractSerializationTests.cs`, the matching `specs\*\contracts\` file, and the API list in `README.md` when applicable |
| Card metadata fields or ingestion mapping | `src\CommandSynergy.Ingestion\Program.cs` and `tests\CommandSynergy.Infrastructure.Tests\CardMetadata\` |
| Imported deck persistence or selection | `tests\CommandSynergy.WebUI.Tests\Verification\ImportedDeckLibraryStateModelTests.cs`, `.github\skills\imported-deck-library\SKILL.md`, and `.github\skills\verify\features\deck-library.md` |
| Workspace interaction | `tests\CommandSynergy.WebUI.Tests\Components\DeckWorkspaceViewModelTests.cs` and the matching recipe under `.github\skills\verify\features\` |

## Documentation

Developer setup and security notes live in `CONTRIBUTING.md`. Product and design notes live in `PRODUCT.md` and `DESIGN.md`. Feature specifications and API contracts live in `specs\`. Browser verification instructions live in `.github\skills\verify\`. The repository has no separate documentation site or documentation build.

## Done means

- `dotnet restore src\CommandSynergy.slnx` exits successfully.
- `dotnet build src\CommandSynergy.slnx` exits successfully.
- `dotnet test src\CommandSynergy.slnx` exits successfully.
- Every behavior change has a test that fails without the change.

## Never merges without a human

A person has to have **read this diff** before it lands. Telling an agent "merge it when you're done" is approving a goal, not this change, so it does not count for anything on this list. Everywhere else it counts fine, which is the point of having a list.

- Any change under `.github\workflows\`.
