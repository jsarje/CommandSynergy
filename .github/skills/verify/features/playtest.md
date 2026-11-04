# Playtest a deck

Playtest opens a Goldfish view of the current mainboard, deals an opening hand, and lets you draw cards or take mulligans.

## Sub-features

- `playtest-open` opens the playtest dialog from the workspace app bar.
- `playtest-opening-hand` deals up to seven mainboard cards.
- `playtest-draw` moves a card from the library to the hand.
- `playtest-mulligan` redraws and enters the bottom-card choice flow.
- `playtest-close` returns to the workspace.

## How to get to it (user POV)

- Add cards to the mainboard or open a saved deck with a mainboard.
- Choose `Playtest` in the workspace app bar.
- Choose `Draw from top` or `Take mulligan`.
- Choose `Back to deck` to close the dialog.

## Driving it with Browser canvas

Preconditions:

- The verify skill's Doctor check passes.
- Open a deck with at least eight mainboard cards. The first hand contains up to seven cards, so an eight-card mainboard leaves a card to draw.

- **Open playtest.** Choose `Playtest`. The dialog title is `Goldfish`, and its `Hand` count equals seven for a mainboard with at least seven cards.
- **Draw a card.** Choose `Draw from top`. The `Hand` count increases by one and the `Library` count decreases by one.
- **Take a mulligan.** Start a fresh playtest and choose `Take mulligan`. The interface asks you to choose cards to put on the bottom. Choose `Put on bottom` for the required number of cards, then verify the phase returns to play.
- **Close playtest.** Choose `Back to deck`. The dialog closes and the workspace remains visible.
- **Proof.** Capture the opening hand count, the draw action and both changed counts, the mulligan choice, and the returned workspace.

## Gotchas

- Playtest uses only cards assigned to `mainboard`. Commander-zone cards do not count toward the library.
- A mainboard with seven or fewer cards has no card to draw after its opening hand. Do not use that deck to claim the draw path works.
- The opening hand is shuffled. Verify counts and state changes, not exact card order.
- Playtest state is transient and does not change the saved deck.
