---
name: shipping-a-change
description: Updates needed when changing CommandSynergy features and the checks required before a pull request. Use before opening a pull request.
---

# Shipping a change

## Add an API endpoint

1. Add request and response contracts under `src\CommandSynergy.Application\Contracts\`.
2. Add the endpoint under `src\CommandSynergy\Endpoints\`.
3. Map the endpoint in `src\CommandSynergy\Program.cs`.
4. Add endpoint tests under `tests\CommandSynergy.WebUI.Tests\Endpoints\`.
5. Run `dotnet test src\CommandSynergy.slnx`.

## When you change this, also change that

| Change | Also update |
|---|---|
| API endpoint or response contract | `tests\CommandSynergy.WebUI.Tests\Endpoints\`, `tests\CommandSynergy.WebUI.Tests\ContractSerializationTests.cs`, the matching `specs\*\contracts\` file, and the API list in `README.md` when applicable |
| Card metadata fields or ingestion mapping | `src\CommandSynergy.Ingestion\Program.cs` and `tests\CommandSynergy.Infrastructure.Tests\CardMetadata\` |
| Imported deck persistence or selection | `tests\CommandSynergy.WebUI.Tests\Verification\ImportedDeckLibraryStateModelTests.cs`, `.github\skills\imported-deck-library\SKILL.md`, and `.github\skills\verify\features\deck-library.md` |
| Workspace interaction | `tests\CommandSynergy.WebUI.Tests\Components\DeckWorkspaceViewModelTests.cs` and the matching recipe under `.github\skills\verify\features\` |

## Done

- `dotnet restore src\CommandSynergy.slnx` exits successfully.
- `dotnet build src\CommandSynergy.slnx` exits successfully.
- `dotnet test src\CommandSynergy.slnx` exits successfully.
- Every behavior change has a test that fails without the change.
