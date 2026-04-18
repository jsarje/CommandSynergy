using CommandSynergy.Application.Contracts;

namespace CommandSynergy.Application.Abstractions;

/// <summary>
/// Provides an extensibility point for enriching deck analysis results with advice.
/// </summary>
public interface IDeckAdviceService
{
    /// <summary>
    /// Enriches an existing analysis result with advice or recommendations.
    /// </summary>
    Task<DeckAnalysisResponseContract> EnrichAsync(DeckSnapshotContract deckSnapshot, DeckAnalysisResponseContract analysis, CancellationToken cancellationToken = default);
}