# Inspect deck analysis

Deck Signals reports commander legality, bracket, synergy, power, and the top theme for the current workspace. Each metric opens a detail panel with its findings or analysis.

## Sub-features

- `analysis-validation` reports legality and validation findings.
- `analysis-bracket` shows the bracket level and its factors.
- `analysis-synergy` shows the synergy score and explanation.
- `analysis-power` shows the power score and explanation.
- `analysis-theme` shows theme signals and commander alignment when available.

## How to get to it (user POV)

- Open `/` and build or open a deck in the workspace.
- Read the `Deck signals` panel for the current state and metric values.
- Choose `Legality`, `Bracket`, `Synergy`, `Power`, or `Top Theme` to inspect its detail panel.

## Driving it with Browser canvas

Preconditions:

- The verify skill's Doctor check passes.
- Use a representative legal Commander deck with a commander and its full mainboard.
- Resolve every imported card before judging the analysis result. The local snapshot is absent in this checkout, so card resolution may use Scryfall.

- **Check validation.** Read `Deck signals` after the deck settles. A valid deck shows `Legal`. Choose the `Legality` metric and inspect its findings.
- **Check bracket.** Choose the `Bracket` metric. The detail panel shows the numeric bracket and the factors used in the assessment.
- **Check synergy.** Choose the `Synergy` metric. The detail panel shows the score and its explanation.
- **Check power.** Choose the `Power` metric. The detail panel shows the score and its explanation.
- **Check theme.** Choose `Top Theme`. If the deck produces theme analysis, the detail panel shows ranked themes and commander alignment. If the panel reports no theme data, record that result rather than inventing a theme.
- **Proof.** Capture each metric action and its corresponding detail state. Record the deck source and whether every card resolved.

## Gotchas

- A one-card workspace is not a valid Commander deck and cannot prove the full analysis journey.
- This checkout has no `cards.parquet` snapshot. If the chosen deck contains unresolved cards or fails validation, report the missing cards and the failed precondition. Do not substitute a bUnit result or a test-only API call.
- EDHREC enrichment is optional. A local analysis result can be valid without EDHREC data; report an EDHREC-specific failure only when testing that enrichment.
- Analysis refreshes after workspace changes. Wait for `Refreshing analysis` to disappear before recording metric values.
