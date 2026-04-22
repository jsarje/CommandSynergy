# CommandSynergy

CommandSynergy is a developer-focused .NET 10 Blazor Web App to build, validate, and analyze Commander (EDH) Magic: The Gathering decks using server-authoritative rules and a local Parquet-backed card metadata snapshot.

> [!NOTE]
> This repository is intended as a local developer tool and workspace. It does not provide public authentication or hosted production services.

## What it does

- Validate Commander legality and deck rules using a centralized domain ruleset.
- Provide fast card search backed by a derived, client-friendly index generated from a Parquet snapshot of Scryfall data.
- Produce deck analyses (brackets, synergy matrices, and other metrics) driven by server-side logic.
- Offer an interactive workspace UI for organizing piles and inspecting card faces while keeping analysis and validation synchronized with server logic.

## Key features

- `GET /api/cards/search` — search the derived card index.
- `POST /api/decks/validate` — validate deck legality and surface rule violations.
- `POST /api/decks/analyze` — compute bracket and synergy outputs for a deck.
- Parquet-backed metadata with a separate ingestion tool that regenerates the authoritative snapshot from Scryfall bulk data.

## Theme analysis

- Theme analysis ranks deck themes, computes a qualitative synergy score, and reports commander alignment directly in the workspace.
- The ingestion pipeline now pre-computes per-card `ThemeSignals` into the Parquet snapshot so deck analysis stays fast at request time.
- Optional EDHREC enrichment is SSRF-guarded, cached for 15 minutes, and degrades cleanly to local-only scoring when unavailable.
- The workspace keeps the last successful theme result visible while a refresh is in flight so deck edits do not blank the analysis panel.

## Architecture overview

- `src/CommandSynergy`: Blazor Web App, JSON endpoints, and interactive workspace shell.
- `src/CommandSynergy.Client`: Interactive client services for workspace integration.
- `src/CommandSynergy.Domain`: Domain rules, deck entities, and analysis models.
- `src/CommandSynergy.Application`: Search, validation, and analysis orchestration.
- `src/CommandSynergy.Infrastructure`: Scryfall adapters, Parquet metadata access, caching, and telemetry.
- `src/CommandSynergy.Ingestion`: Console tool to refresh the Parquet snapshot.

## Next steps & contribution

If you'd like to contribute, run the tests and follow the development and security guidelines in [CONTRIBUTING.md](CONTRIBUTING.md).

## License

See the repository LICENSE file.