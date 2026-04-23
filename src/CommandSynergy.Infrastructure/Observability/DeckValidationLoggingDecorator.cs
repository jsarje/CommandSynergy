using CommandSynergy.Application.Abstractions;
using CommandSynergy.Application.Contracts;
using Microsoft.Extensions.Logging;

namespace CommandSynergy.Infrastructure.Observability;

/// <summary>
/// Adds structured logging around deck validation requests.
/// </summary>
public sealed class DeckValidationLoggingDecorator(IDeckValidationService inner, ILogger<DeckValidationLoggingDecorator> logger) : IDeckValidationService
{
    /// <inheritdoc />
    public async Task<DeckValidationResponseContract> ValidateAsync(DeckSnapshotContract deckSnapshot, CancellationToken cancellationToken = default)
    {
        var startedUtc = DateTimeOffset.UtcNow;
        logger.LogInformation("Validating deck {DeckId} with {EntryCount} entries", deckSnapshot.DeckId ?? "new-deck", deckSnapshot.Entries.Count);

        var response = await inner.ValidateAsync(deckSnapshot, cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Validation completed for deck {DeckId} in {ElapsedMilliseconds}ms with {FindingCount} findings",
            deckSnapshot.DeckId ?? "new-deck",
            (DateTimeOffset.UtcNow - startedUtc).TotalMilliseconds,
            response.Findings.Count);

        return response;
    }
}