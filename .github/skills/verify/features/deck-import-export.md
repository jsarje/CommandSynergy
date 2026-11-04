# Import and export decks

The Deck Library accepts pasted decklists in supported plaintext formats and generates a text preview for the current workspace.

## Sub-features

- `deck-import` parses a pasted decklist and adds it to the browser library.
- `deck-format` detects or accepts a Moxfield Text, ManaBox Text, or Generic Plaintext format.
- `deck-duplicate` offers an update-or-copy choice when a deck with the same name is imported again.
- `deck-export` generates a preview in the selected export format.

## How to get to it (user POV)

- Open `Deck Library` in the workspace app bar.
- Expand `Import a deck`, choose a source format, paste a list, and choose `Import deck`.
- Expand `Export`, choose a target format, and choose `Generate preview`.

## Driving it with Browser canvas

Preconditions:

- The verify skill's Doctor check passes.
- The browser origin is fresh.
- Scryfall is reachable because card names absent from the local snapshot are resolved through card search.

- **Open import.** Choose `Open Deck Library`, then expand `Import a deck`. If Browser canvas does not expose a reference for the accordion header, use the visible-text click from the verify skill with `Import a deck`.
- **Import a list.** Keep the source format at `Auto-detect`, enter `1 Sol Ring` in `Paste deck`, and choose `Import deck`. The library count increases to one and the saved row is named `Sol Ring`. The app may show search and import diagnostics while it resolves the card.
- **Resolve a duplicate.** Import the same list again. Choose `Update existing deck` in the `duplicate-import-resolution` region. Verify that the library count stays at one and the row is still named `Sol Ring`.
- **Open the imported deck.** If `Sol Ring` is not selected, choose `Select` on its row. Choose `Open selected deck` and confirm that the workspace mainboard contains `Sol Ring`.
- **Set a commander.** Search for `Shu Yun, the Silent Tempest` in the workspace and choose `Set as commander` on its legal card result. Confirm that the command zone shows Shu Yun.
- **Preview an export.** Expand `Export`, keep the default `moxfield-text` target, and choose `Generate preview`. Confirm that the `Export preview` field contains both `Sol Ring` and `Shu Yun, the Silent Tempest`.
- **Proof.** Save the action and result snapshots for the import, duplicate decision, opening the saved deck, setting the commander, and export preview. Check localStorage after import and after reload to prove the imported deck persists.

## Gotchas

- An export preview is shown in a read-only field. This component does not download a file.
- Import resolves each card through the search service. With no local snapshot, that path needs Scryfall network access.
- Do not count an unresolved import as a successful card import. Read the import diagnostics and confirm the imported row and card entries.
- Duplicate imports require a user choice. Do not treat the duplicate warning as a completed update or copy.
- The accordion header is rendered as a `div` without a button role, so Browser canvas may not return a click reference for it.
