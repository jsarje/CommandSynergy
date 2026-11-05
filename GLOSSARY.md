# CommandSynergy

CommandSynergy is a Commander deckbuilding workspace for composing, importing, validating, and evaluating decks.

## Deck construction

**Deck**:
The editable Commander composition in the workspace.
_Avoid_: Decklist, when referring to the editable composition.

**Decklist**:
A card-list representation of a deck, commonly used to import or export its contents.

**Imported deck**:
A decklist brought into the workspace. “Imported” describes how it entered the workspace, not whether it has been saved.

**Saved deck**:
A deck retained in the deck library, whether it began as an import or a newly created draft.

**Working copy**:
The deck composition currently open for editing.

**Linked**:
A working copy connected to an existing saved deck, so edits are saved back to that deck.

**Detached**:
A working copy not connected to a saved deck; it must be saved to the library to be retained as one.

**Card entry**:
One distinct card identity together with its quantity in a deck.
_Avoid_: Card, when referring specifically to the quantity-bearing entry.

**Card copy**:
One counted instance of a card represented by a card entry.

**Pile**:
A user-defined grouping for organizing cards within a deck.

**Deck section**:
A role-based part of a decklist, such as Commander, Mainboard, Sideboard, Maybeboard, Companion, or Custom.
_Avoid_: Pile.

**Ambiguous import line**:
A decklist line that could refer to more than one card and has not yet been resolved.

**Unresolved import line**:
A decklist line that has not been matched to a card and remains separate from the resolved cards in the deck.

## Commander rules

**Commander**:
The card designated as the deck's leader; its color identity constrains which cards may be included.

**Companion**:
An optional, separately designated card with its own eligibility restrictions; it is not a second commander.

**Color identity**:
The set of colors associated with a card for Commander deckbuilding. The commander's color identity constrains the deck's other cards.
_Avoid_: Card color, when discussing Commander deckbuilding restrictions.

**Commander legality**:
Whether a deck conforms to Commander format rules. Legality is separate from bracket, theme, and synergy assessments.

**Singleton**:
The Commander deckbuilding rule that generally limits a deck to one copy of each card, with format-defined exceptions.

## Deck analysis

**Bracket**:
A 1–5 assessment of a deck's power and play patterns, not a universal or exact power rating.
_Avoid_: Power level, as a generic synonym for bracket.

**Theme**:
A strategic pattern supported by a deck's cards, such as Tokens, Combo, or Kindred.

**Kindred**:
A theme built around shared creature types. The app currently displays this theme as “Tribal.”
_Avoid_: Tribal as the glossary's preferred term.

**Combo theme**:
A signal that a deck appears combo-oriented; it does not confirm that a complete or deterministic combo is present.

**Theme strength**:
The strength with which a deck supports one identified theme; it is distinct from the overall synergy score.

**Synergy score**:
A 0–100 measure of how coherently a deck's cards support its themes and commander.

**Commander alignment**:
How closely a deck's detected themes align with the themes supported by its commander.

**Off-theme card**:
A card the analysis does not associate with the deck's detected themes; this is not a judgment of the card's quality.

**Game Changer**:
A card category that can inform bracket assessment. It is an analysis signal, not a Commander legality requirement.

**EDHREC enhancement**:
Optional, non-authoritative EDHREC context that can augment theme or synergy analysis; local analysis remains meaningful without it.
