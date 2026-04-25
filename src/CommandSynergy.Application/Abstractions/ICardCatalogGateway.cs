using CommandSynergy.Application.Contracts;
using CommandSynergy.Domain.Cards;

namespace CommandSynergy.Application.Abstractions;

/// <summary>
/// Provides access to searchable card summaries and authoritative card profiles.
/// </summary>
public interface ICardCatalogGateway
{
    /// <summary>
    /// Searches the current card catalog and returns compact search results.
    /// </summary>
    Task<IReadOnlyList<CardSearchResultContract>> SearchAsync(CardSearchQueryContract request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads authoritative card profiles for the supplied card identifiers.
    /// </summary>
    Task<IReadOnlyDictionary<string, CardProfile>> GetCardProfilesAsync(IEnumerable<string> cardIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the legal commander card pool used for recommendation queries.
    /// </summary>
    Task<IReadOnlyList<CardProfile>> GetCommanderLegalCardProfilesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current search snapshot version when available.
    /// </summary>
    Task<string?> GetSnapshotVersionAsync(CancellationToken cancellationToken = default);
}
