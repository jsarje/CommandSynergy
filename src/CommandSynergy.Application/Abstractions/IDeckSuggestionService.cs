using CommandSynergy.Application.Contracts;

namespace CommandSynergy.Application.Abstractions;

/// <summary>
/// Produces commander-aware deck suggestions for the active workspace.
/// </summary>
public interface IDeckSuggestionService
{
    /// <summary>
    /// Returns the highest-ranked suggestion candidates for the supplied deck state.
    /// </summary>
    Task<DeckSuggestionsResponseContract> GetSuggestionsAsync(DeckSuggestionsRequestContract request, CancellationToken cancellationToken = default);
}
