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

## Containers

- GitHub Actions publishes two GHCR images from `main`: `ghcr.io/jsarje/commandsynergy-web` and `ghcr.io/jsarje/commandsynergy-ingestion`.
- Both containers should point `CardMetadata__SnapshotDirectory` at the same mounted directory so the web app and scheduled ingestion jobs read and write the same Parquet snapshot.

```yaml
services:
  synergy-sphere:
    container_name: synergy-sphere
    image: ghcr.io/jsarje/commandsynergy-web:0    
    user: "0:0"
    environment:
      ASPNETCORE_HTTP_PORTS: "8080"
      DisableHttpsRedirection: "true"
      CardMetadata__SnapshotDirectory: /data/card-metadata
    expose:
      - "8080"
    volumes:
      - shared-data:/data/card-metadata
    restart: unless-stopped

  ingestion:
    image: ghcr.io/jsarje/commandsynergy-ingestion:0
    profiles: [ "manual" ]
    user: "0:0"
    environment:
      CardMetadata__SnapshotDirectory: /data/card-metadata
    volumes:
      - shared-data:/data/card-metadata
      
  caddy:
    image: caddy:latest
    restart: unless-stopped
    ports:
      - "80:80"     # Required for HTTP -> HTTPS redirects & SSL challenges
      - "443:443"   # HTTPS
    volumes:
      - $PWD/conf:/etc/caddy:ro
      - caddy_data:/data      # Crucial: Persists Let's Encrypt certs between restarts
      - caddy_config:/config
    depends_on:
      - synergy-sphere

  ofelia:
    image: mcuadros/ofelia:latest
    command: daemon --docker
    depends_on:
      - synergy-sphere
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock:ro
    labels:
      ofelia.job-run.refresh-card-metadata.schedule: "@weekly"
      ofelia.job-run.refresh-card-metadata.image: "ghcr.io/jsarje/commandsynergy-ingestion:main"
      ofelia.job-run.refresh-card-metadata.environment: "CardMetadata__SnapshotDirectory=/data/card-metadata"
      ofelia.job-run.refresh-card-metadata.volume: "shared-data:/data/card-metadata"

volumes:
  shared-data:
    name: shared-data
  caddy_data:
    name: caddy_data
  caddy_config:
    name: caddy_config
```

1. Save the example as `docker-compose.yml`, then pull the published images with `docker compose pull`.
2. Seed the shared volume once with `docker compose --profile manual run --rm ingestion`.
3. Start the long-running services with `docker compose up -d blazor-app ofelia`.
4. Keep the explicit `name: shared-data` entry so Ofelia reuses the same named volume instead of a Compose-generated alias.

## Next steps & contribution

If you'd like to contribute, run the tests and follow the development and security guidelines in [CONTRIBUTING.md](CONTRIBUTING.md).

## License

See the repository LICENSE file.
