# CommandSynergy

CommandSynergy is a .NET 10 Blazor Web App for building, validating, and analyzing Commander decks with server-authoritative rules and card metadata.

## Architecture

- `src/CommandSynergy` hosts the Blazor Web App, JSON endpoints, and interactive workspace shell.
- `src/CommandSynergy.Client` contains Interactive Auto client services for local workspace interactions.
- `src/CommandSynergy.Domain` contains commander rules, deck entities, and analysis models.
- `src/CommandSynergy.Application` contains search, validation, and analysis orchestration.
- `src/CommandSynergy.Infrastructure` contains Scryfall adapters, Parquet-backed metadata access, caching, and telemetry.

## Current Feature Surface

- Search a derived client-friendly card index through `GET /api/cards/search`.
- Validate commander legality through `POST /api/decks/validate`.
- Calculate bracket and synergy outputs through `POST /api/decks/analyze`.
- Use the interactive workspace to organize piles, inspect alternate faces, and keep validation and analysis synchronized with server-owned logic.

## Development

```powershell
dotnet restore src/CommandSynergy.slnx
dotnet build src/CommandSynergy.slnx
dotnet test src/CommandSynergy.slnx
```

## Quality Notes

- External Scryfall access is wrapped in a typed `HttpClient` with standard .NET resilience handlers and safe fallback behavior.
- Local Parquet metadata remains authoritative on the server, while the client receives a derived lightweight search artifact.
- Regression coverage includes domain rules, endpoint contracts, UI behavior, security-focused endpoint validation, and focused performance budget checks.