using CommandSynergy.Application.Contracts;

namespace CommandSynergy.Application.Abstractions;

/// <summary>
/// Validates commander deck legality for a submitted deck snapshot.
/// </summary>
public interface IDeckValidationService
{
    /// <summary>
    /// Validates the supplied deck snapshot against commander rules.
    /// </summary>
    Task<DeckValidationResponseContract> ValidateAsync(DeckSnapshotContract deckSnapshot, CancellationToken cancellationToken = default);
}