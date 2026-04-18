using CommandSynergy.Application.Contracts;

namespace CommandSynergy.Application.Abstractions;

/// <summary>
/// Produces bracket and synergy analysis for a submitted deck snapshot.
/// </summary>
public interface IDeckAnalysisService
{
    /// <summary>
    /// Calculates the current bracket and synergy results for the supplied deck snapshot.
    /// </summary>
    Task<DeckAnalysisResponseContract> AnalyzeAsync(DeckSnapshotContract deckSnapshot, CancellationToken cancellationToken = default);
}