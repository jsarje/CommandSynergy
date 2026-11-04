# Search and build a deck

The workspace lets you search for cards, choose a legal commander, and add cards to the mainboard. The card search calls Scryfall when the local metadata snapshot has no match.

## Sub-features

- `workspace-search` finds cards by name.
- `workspace-commander` sets a legal commander in the command zone.
- `workspace-mainboard` adds cards to the mainboard and updates the deck count.
- `workspace-suggestions` requests commander-specific card suggestions.

## How to get to it (user POV)

- Open `/` in the browser to reach the deck workspace.
- Use the `Search commanders, staples, or role players` field and choose `Search`.
- Choose `Set as commander` on a legal card result.
- Choose `Add to deck` to place a card in the mainboard.
- Open `Suggestions` after setting a commander.

## Driving it with Browser canvas

Preconditions:

- The verify skill's Doctor check passes.
- The browser origin is fresh and the workspace has no saved deck.
- Scryfall is reachable for a card that is not in the empty local snapshot.

- **Search a card.** Read the page, type `Sol Ring` into the search input, then choose `Search`. The results include `Sol Ring`. Scryfall can return multiple printings; identify the exact `Sol Ring` result before choosing an action.
- **Add it to the mainboard.** Choose `Add to deck` for the exact `Sol Ring` result. The app bar shows `1 cards`, `Mainboard` shows `1 cards`, and the mainboard card is `Sol Ring`.
- **Set a commander.** Start a fresh workspace, search for `Shu Yun, the Silent Tempest`, and choose `Set as commander`. The briefing's `Commander` value and the command zone both show `Shu Yun, the Silent Tempest`.
- **Open suggestions.** With a commander set, choose `Open` in the `Suggestions` panel. The filter controls and `Magic suggestions` action appear. A suggestion result requires a successful suggestions request; an empty result alone does not prove that the request succeeded.
- **Proof.** Save the before and after `read_page` output. Capture the search result and the changed mainboard with Browser canvas `screenshot_page`. For a commander journey, capture the briefing and command zone after the result settles.

## Gotchas

- The local Parquet snapshot is absent. Card search uses Scryfall's autocomplete and named-card requests. If Scryfall is unavailable, report the search as blocked instead of asserting an empty result.
- The search result list can include alternate-face or printing results. Do not use the first `Add to deck` button unless its card name matches the result you intend to add.
- Setting a commander alone does not make a complete Commander deck. Do not use a one-card deck to claim that full legality or analysis works.
- `type_in_page` types into the current input. Reload the fresh workspace before starting a separate search if the canvas does not offer a clear-input action.
- Suggestion results depend on the selected commander, card metadata, and the external services configured for the app.
