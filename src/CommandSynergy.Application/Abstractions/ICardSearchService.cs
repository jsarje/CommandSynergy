using CommandSynergy.Application.Contracts;

namespace CommandSynergy.Application.Abstractions;

/// <summary>
/// Provides card search results for the active deck-building workflow.
/// </summary>
public interface ICardSearchService
{
    /// <summary>
    /// Searches the current metadata index for cards matching the supplied query.
    /// </summary>
    Task<CardSearchResponseContract> SearchAsync(CardSearchQueryContract request, CancellationToken cancellationToken = default);
}