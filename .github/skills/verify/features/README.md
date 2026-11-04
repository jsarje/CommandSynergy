# CommandSynergy verification map

This map records the user journeys for CommandSynergy's Blazor deckbuilding workspace. Read the matching feature file before driving the app.

## Baseline preconditions

- Start the app with [the verify skill](../SKILL.md) and its unique loopback port, browser instance, and empty metadata directory.
- The app has no authentication or database migrations.
- This checkout has no bundled `cards.parquet` file. Card search falls back to Scryfall, so journeys that search or import cards need network access to `api.scryfall.com`.
- The app stores imported and saved decks in browser localStorage. Do not reuse a port or browser origin that may contain user data.
- The app's EDHREC lookup is optional. Do not make a journey depend on EDHREC availability unless that journey specifically tests the enrichment.

## Driving conventions

- Start each recipe from the preconditions in its file. Use a fresh browser origin for each run.
- Use Browser canvas `read_page` and current element references. Prefer visible labels and `data-testid` values over CSS position.
- For accordion headers that have no accessible button reference, use the visible-text click in the verify skill. Do not set component state or call test-only endpoints.
- Save the action and resulting text snapshots in the run's evidence directory. Capture screenshots in the Browser canvas and preserve them through cleanup.
- Check persistence after reload for saved-library changes. Delete only the deck created for the run, then remove the specific localStorage key from that fresh origin.
- If a recipe lacks its required deck or card data, report the unmet precondition. Do not count a different path as proof.

## Features

- [Search and build a deck](./workspace.md) covers card search, commander selection, and mainboard changes.
- [Manage the deck library](./deck-library.md) covers saving, reopening, and deleting browser-local decks.
- [Import and export decks](./deck-import-export.md) covers supported plaintext formats and export previews.
- [Inspect deck analysis](./analysis.md) covers legality, bracket, synergy, power, and theme details.
- [Playtest a deck](./playtest.md) covers opening hands, draws, and mulligans.
