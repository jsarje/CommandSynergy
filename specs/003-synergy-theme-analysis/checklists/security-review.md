# Security Review: Synergy Scoring & Deck Theme Analysis

Date: 2026-04-21
Reviewer: GitHub Copilot
Status: PASS

## Scope

- Theme taxonomy matching and score computation
- EDHREC commander enrichment
- Theme-analysis API and UI payload handling

## Findings

- SSRF risk on EDHREC lookups: mitigated by slug generation plus allowlist validation before URL construction in `EdhrecClient`.
- Injection risk from deck input: mitigated because theme matching reads only trusted card metadata and does not execute user-provided text.
- Payload compatibility risk: mitigated by regression coverage for `usedEdhrecFallback`, `metadataUnavailable`, commander alignment, and theme score serialization.
- Availability risk from external EDHREC failures: mitigated by timeout handling, resilience handler registration, warning-level logging, and fallback to local-only scoring.

## Evidence

- `tests/CommandSynergy.Infrastructure.Tests/Edhrec/EdhrecClientTests.cs` validates slug normalization, invalid-slug short-circuiting, caching, and invalid JSON fallback.
- `tests/CommandSynergy.WebUI.Tests/ContractSerializationTests.cs` verifies theme payload compatibility for new fields.
- `tests/CommandSynergy.WebUI.Tests/Endpoints/DeckAnalysisIntegrationTests.cs` verifies end-to-end endpoint behavior with focused, unfocused, and commander-misaligned fixtures.

## Disposition

- No open high-severity OWASP-relevant findings remain for this scope.