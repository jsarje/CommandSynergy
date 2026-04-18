using CommandSynergy.Application.Abstractions;
using CommandSynergy.Application.Contracts;
using CommandSynergy.Domain.Cards;
using CommandSynergy.Infrastructure.Scryfall;

namespace CommandSynergy.Infrastructure.CardMetadata;

/// <summary>
/// Searches the derived card index and loads authoritative card profiles with Scryfall fallback.
/// </summary>
public sealed class CardMetadataQueryService : ICardCatalogGateway
{
    private readonly ParquetCardMetadataStore metadataStore;
    private readonly SearchIndexSnapshotBuilder searchIndexSnapshotBuilder;
    private readonly ScryfallClient scryfallClient;
    private readonly ScryfallCardMapper scryfallCardMapper;

    /// <summary>
    /// Creates a card catalog gateway backed by local metadata and Scryfall fallback.
    /// </summary>
    public CardMetadataQueryService(
        ParquetCardMetadataStore metadataStore,
        SearchIndexSnapshotBuilder searchIndexSnapshotBuilder,
        ScryfallClient scryfallClient,
        ScryfallCardMapper scryfallCardMapper)
    {
        this.metadataStore = metadataStore;
        this.searchIndexSnapshotBuilder = searchIndexSnapshotBuilder;
        this.scryfallClient = scryfallClient;
        this.scryfallCardMapper = scryfallCardMapper;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, CardProfile>> GetCardProfilesAsync(IEnumerable<string> cardIds, CancellationToken cancellationToken = default)
    {
        var requestedIds = cardIds
            .Where(cardId => !string.IsNullOrWhiteSpace(cardId))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var snapshot = await metadataStore.LoadSnapshotAsync(cancellationToken).ConfigureAwait(false);
        var fromSnapshot = snapshot.Cards
            .Where(card => requestedIds.Contains(card.CardId, StringComparer.OrdinalIgnoreCase))
            .ToDictionary(
                card => card.CardId,
                card => new CardProfile
                {
                    CardId = card.CardId,
                    OracleId = card.OracleId,
                    Name = card.Name,
                    ManaCost = card.ManaCost,
                    ManaValue = card.ManaValue,
                    TypeLine = card.TypeLine,
                    OracleText = card.OracleText,
                    ColorIdentity = card.ColorIdentity,
                    ImageUri = card.ImageUri,
                    SaltScore = card.SaltScore,
                    PlayRateByCommander = card.PlayRateByCommander ?? new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase),
                    GenericColorStapleRate = card.GenericColorStapleRate,
                    FaceProfiles = card.HasMultipleFaces
                        ? new[] { new CardFaceProfile("0", card.Name, card.ManaCost, card.TypeLine, null, card.ImageUri, true), new CardFaceProfile("1", card.Name + " Back", null, card.TypeLine, null, card.ImageUri, false) }
                        : new[] { new CardFaceProfile("0", card.Name, card.ManaCost, card.TypeLine, null, card.ImageUri, true) },
                },
                StringComparer.OrdinalIgnoreCase);

        foreach (var missingId in requestedIds.Where(cardId => !fromSnapshot.ContainsKey(cardId)))
        {
            var scryfallDocument = await scryfallClient.GetNamedCardAsync(missingId, cancellationToken).ConfigureAwait(false);
            if (scryfallDocument is not null)
            {
                fromSnapshot[missingId] = scryfallCardMapper.MapCardProfile(scryfallDocument);
            }
        }

        return fromSnapshot;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CardSearchResultContract>> SearchAsync(CardSearchQueryContract request, CancellationToken cancellationToken = default)
    {
        var snapshot = await metadataStore.LoadSnapshotAsync(cancellationToken).ConfigureAwait(false);
        var searchIndex = searchIndexSnapshotBuilder.Build(snapshot);
        var colorFilters = request.Colors.Where(color => !string.IsNullOrWhiteSpace(color)).ToArray();

        var results = searchIndex.CardSummaries
            .Where(card => card.Name.Contains(request.Query, StringComparison.OrdinalIgnoreCase))
            .Where(card => colorFilters.Length == 0 || colorFilters.All(filter => card.ColorIdentity.Contains(filter, StringComparer.OrdinalIgnoreCase)))
            .OrderBy(card => card.Name, StringComparer.OrdinalIgnoreCase)
            .Take(20)
            .ToArray();

        if (results.Length > 0)
        {
            return results;
        }

        var scryfallResponse = await scryfallClient.SearchCardsAsync(request.Query, cancellationToken).ConfigureAwait(false);
        return scryfallResponse.Data.Select(scryfallCardMapper.MapSearchResult).Take(20).ToArray();
    }

    /// <inheritdoc />
    public async Task<string?> GetSnapshotVersionAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = await metadataStore.LoadSnapshotAsync(cancellationToken).ConfigureAwait(false);
        return searchIndexSnapshotBuilder.Build(snapshot).Version;
    }
}