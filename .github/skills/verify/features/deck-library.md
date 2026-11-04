# Manage the deck library

The Deck Library saves a workspace in this browser, reopens the last selected deck after reload, and lets you remove a saved deck with confirmation.

## Sub-features

- `library-open` opens and closes the Deck Library drawer.
- `library-save` saves a non-empty workspace under a deck name.
- `library-reopen` restores the selected deck and its cards after reload.
- `library-delete` removes a saved deck after confirmation.

## How to get to it (user POV)

- Choose `Deck Library` in the workspace app bar.
- Enter a name in `New deck name` and choose `Save to library`.
- Choose `Open selected deck` to open the active saved deck.
- Choose `Delete` on a saved deck and confirm with `Delete deck`.

## Driving it with Browser canvas

Preconditions:

- The verify skill's Doctor check passes.
- Use a fresh Browser canvas instance on a unique port.
- Add at least one card to the mainboard through the workspace search journey. `Save to library` is disabled for an empty workspace.

- **Open the library.** Choose `Open Deck Library`. The drawer is named `Deck library` and says `No saved decks yet`.
- **Save a deck.** Enter `Verify Deck <run-id>` in `New deck name`, then choose `Save to library`. The drawer shows `Library (1)`, `1 active`, `Linked`, and a saved deck row with that name.
- **Check the storage write.** Use Browser canvas `evaluate_javascript` only to read `Object.keys(localStorage)` and the value length for `command-synergy.imported-decks`. The key has a non-empty value. Do not decode or copy its compressed value.
- **Reopen the saved deck.** Reload the page. The app bar shows the saved deck name, the deck count remains non-zero, and the saved card is in the mainboard. Read the page again after the library hydration indicator disappears.
- **Delete the test deck.** Open Deck Library, choose `Delete` on the test deck, then choose `Delete deck` in the confirmation dialog. The library shows `Library (0)` and `No saved decks yet`.
- **Proof.** Capture the save action and result, the reload state, and the delete confirmation and result. Keep the localStorage observation with the same run ID.

## Gotchas

- Saved decks live in browser localStorage, not on the server. A new port gives the run a separate origin and avoids reusing a user's library.
- The app may retain an empty serialized library after the user deletes its last deck. During cleanup, remove only `command-synergy.imported-decks` from the run's fresh browser origin.
- The workspace must contain at least one card before `Save to library` becomes enabled.
- Deleting a saved deck does not clear the detached working copy. Reload after deletion and verify that the library is empty before cleaning the origin.
