# Research: Commander Deck Import And Export

## Decision 1: Keep import and export processing client-side only

- **Decision**: Implement format detection, parsing, diagnostics, and export rendering as pure C#
  services executed in the browser, and do not create server import or export endpoints for this
  feature.
- **Rationale**: The feature constraint is explicit that imported decks must remain client-side.
  Client-side processing keeps raw imported documents off the server, avoids unnecessary network
  latency, and allows the app to function for import/export workflows even when the server is not
  needed.
- **Alternatives considered**:
  - Server-side import parsing endpoints: rejected because raw imported deck text would cross the
    server boundary and violate the feature constraint.
  - JavaScript-only parser implementations: rejected because they would duplicate domain-adjacent
    logic outside the existing C# test stack and make fixture-based testing harder.

## Decision 2: Persist imported decks in browser localStorage instead of cookies

- **Decision**: Store the imported deck library in a versioned localStorage document and use
  cookies only if a future feature needs small preference flags that are safe to transmit.
- **Rationale**: Commander deck payloads plus diagnostics and source text are too large for cookies
  and should not be sent on every HTTP request. localStorage has adequate capacity, stays
  browser-local, and aligns with the repo's Blazor guidance for client-side state persistence.
- **Alternatives considered**:
  - Cookies for all persisted deck data: rejected because size limits are too restrictive and the
    browser would automatically attach the data to unrelated requests.
  - Server persistence: rejected because it violates the requirement that imported decks remain
    client-side.

## Decision 3: Model imported decks as a local library separate from the active server-backed workspace

- **Decision**: Introduce a local imported-deck library that holds multiple decks, lets the user
  switch between them in the app, and treats any optional server-backed analysis as an explicit
  working-copy action rather than as automatic synchronization of the persisted library.
- **Rationale**: This preserves the client-only storage boundary while still allowing the current
  app architecture to evolve without conflating browser-local deck ownership with the existing
  stateless validation and analysis endpoints.
- **Alternatives considered**:
  - Reuse the current single in-memory workspace as the only import target: rejected because it
    cannot switch among multiple persisted decks across sessions.
  - Automatically synchronize every imported deck into the current server-backed workspace:
    rejected because it would send imported content to the server without explicit user intent.

## Decision 4: Support a curated initial format catalog with deterministic detection and manual override

- **Decision**: Start with Moxfield text, ManaBox text, and one generic portable plaintext format,
  detect the format automatically when a single profile matches, and require user selection when
  the input could map to more than one supported profile.
- **Rationale**: A curated catalog keeps the first implementation testable while still meeting the
  spec's requirement for known formats plus a generic fallback. Deterministic detection minimizes
  friction without hiding ambiguity from the user.
- **Alternatives considered**:
  - Accept arbitrary community formats in the first release: rejected because the test matrix and
    unsupported-edge handling would grow too quickly.
  - Always force the user to choose the format first: rejected because it adds avoidable friction
    for clear inputs.

## Decision 5: Preserve partial-success imports with explicit diagnostics and unresolved lines

- **Decision**: An import result will include the normalized deck, unresolved or ambiguous lines,
  and user-facing guidance, and the persisted library record will keep those diagnostics so users do
  not lose context after reload.
- **Rationale**: The feature spec requires recognized cards to survive partial failures and for the
  UI to make the remaining problems actionable. Persisting diagnostics also improves cross-session
  recovery and export transparency.
- **Alternatives considered**:
  - Reject any import with unresolved lines: rejected because it discards usable work and conflicts
    with FR-005.
  - Drop diagnostics after the initial import toast: rejected because users would lose the recovery
    path after navigation or refresh.

## Decision 6: Wrap browser storage behind a client service instead of direct component interop

- **Decision**: Introduce a client-side deck library store abstraction that owns serialization,
  schema-version migration, corruption recovery, and localStorage access, and keep Razor
  components focused on user interaction state.
- **Rationale**: A storage abstraction makes the feature easier to test, isolates JS interop or
  package-specific behavior, and provides a single place to enforce payload limits and safe
  deserialization.
- **Alternatives considered**:
  - Read and write localStorage directly from Razor components: rejected because it scatters
    persistence logic and complicates unit testing.
  - Keep imported decks only in memory: rejected because it fails the requirement to persist across
    sessions.

## Decision 7: Prove the feature with fixture-based format tests plus UI state coverage

- **Decision**: Use xUnit fixture tests for supported import/export formats and round-trip behavior,
  bUnit tests for import dialog, library switching, and recovery states, and host/security tests
  for input size limits and XSS-safe diagnostics rendering.
- **Rationale**: The feature combines structured text parsing, browser persistence, and visible UI
  states, so no single test layer is sufficient on its own.
- **Alternatives considered**:
  - Only component tests: rejected because parsing and export rendering need richer fixture
    coverage.
  - Only service-level tests: rejected because hydration, storage corruption, and recovery states
    are user-facing behavior.