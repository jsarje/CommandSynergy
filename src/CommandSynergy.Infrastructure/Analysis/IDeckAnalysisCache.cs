using CommandSynergy.Application.Contracts;

namespace CommandSynergy.Infrastructure.Analysis;

/// <summary>
/// Provides shared cache helpers for deck analysis responses.
/// </summary>
public interface IDeckAnalysisCache
{
    /// <summary>
    /// Tries to get a cached response for the supplied key.
    /// </summary>
    bool TryGet(string cacheKey, out DeckAnalysisResponseContract response);

    /// <summary>
    /// Stores a response for the supplied cache key.
    /// </summary>
    void Set(string cacheKey, DeckAnalysisResponseContract response);

    /// <summary>
    /// Creates a cache key from the submitted deck state and metadata snapshot version.
    /// </summary>
    string CreateKey(DeckSnapshotContract deckSnapshot, string? snapshotVersion);
}
