using CommandSynergy.Application.Abstractions;
using CommandSynergy.Application.Contracts;

namespace CommandSynergy.Application.Cards;

/// <summary>
/// Orchestrates search requests against the current card catalog.
/// </summary>
public sealed class CardSearchService(ICardCatalogGateway cardCatalogGateway) : ICardSearchCoreService
{
    /// <inheritdoc />
    public async Task<CardSearchResponseContract> SearchAsync(CardSearchQueryContract request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return new CardSearchResponseContract
            {
                SnapshotVersion = await cardCatalogGateway.GetSnapshotVersionAsync(cancellationToken).ConfigureAwait(false),
                Results = Array.Empty<CardSearchResultContract>(),
            };
        }

        var results = await cardCatalogGateway.SearchAsync(request, cancellationToken).ConfigureAwait(false);
        var snapshotVersion = await cardCatalogGateway.GetSnapshotVersionAsync(cancellationToken).ConfigureAwait(false);

        return new CardSearchResponseContract
        {
            SnapshotVersion = snapshotVersion,
            Results = results,
        };
    }
}
