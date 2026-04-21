using CommandSynergy.Application.Abstractions;
using CommandSynergy.Application.Contracts;
using CommandSynergy.Domain.Cards;
using CommandSynergy.Infrastructure.Scryfall;
using Microsoft.Extensions.Logging;

namespace CommandSynergy.Infrastructure.CardMetadata;

/// <summary>
/// Searches the derived card index and loads authoritative card profiles with read-only Scryfall fallback.
/// </summary>
public sealed class CardMetadataQueryService : ICardCatalogGateway
{
    private readonly ParquetCardMetadataStore metadataStore;
    private readonly SearchIndexSnapshotBuilder searchIndexSnapshotBuilder;
    private readonly ScryfallClient scryfallClient;
    private readonly ScryfallCardMapper scryfallCardMapper;
    private readonly ILogger<CardMetadataQueryService> logger;
    private readonly object searchIndexLock = new();

    private CachedSearchIndex? cachedSearchIndex;

    /// <summary>
    /// Creates a card catalog gateway backed by local metadata and read-only Scryfall fallback.
    /// </summary>
    public CardMetadataQueryService(
        ParquetCardMetadataStore metadataStore,
        SearchIndexSnapshotBuilder searchIndexSnapshotBuilder,
        ScryfallClient scryfallClient,
        ScryfallCardMapper scryfallCardMapper,
        ILogger<CardMetadataQueryService> logger)
    {
        this.metadataStore = metadataStore;
        this.searchIndexSnapshotBuilder = searchIndexSnapshotBuilder;
        this.scryfallClient = scryfallClient;
        this.scryfallCardMapper = scryfallCardMapper;
        this.logger = logger;
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
                    Keywords = Array.Empty<string>(),
                    ColorIdentity = card.ColorIdentity,
                    ImageUri = card.ImageUri,
                    SaltScore = card.SaltScore,
                    PlayRateByCommander = card.PlayRateByCommander ?? new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase),
                    ThemeSignals = card.ThemeSignals ?? new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase),
                    GenericColorStapleRate = card.GenericColorStapleRate,
                    IsGameChanger = card.IsGameChanger,
                    IsMassLandDenial = card.IsMassLandDenial,
                    IsLegalInCommander = card.IsLegalInCommander,
                    AllowsMultipleCopies = card.AllowsMultipleCopies,
                    CompanionRequirementCode = card.CompanionRequirementCode,
                    CommanderEligibilityBasis = card.CommanderEligibilityBasis,
                    MetadataSource = card.MetadataSource,
                    LastSyncedUtc = card.LastSyncedUtc,
                    FaceProfiles = card.HasMultipleFaces
                        ? new[] { new CardFaceProfile("0", card.Name, card.ManaCost, card.TypeLine, null, card.ImageUri, true), new CardFaceProfile("1", card.Name + " Back", null, card.TypeLine, null, card.ImageUri, false) }
                        : new[] { new CardFaceProfile("0", card.Name, card.ManaCost, card.TypeLine, null, card.ImageUri, true) },
                },
                StringComparer.OrdinalIgnoreCase);

        logger.LogDebug(
            "Loaded {ResolvedCardCount} of {RequestedCardCount} requested card profiles from snapshot {SnapshotId}",
            fromSnapshot.Count,
            requestedIds.Length,
            snapshot.SnapshotId);

        foreach (var missingId in requestedIds.Where(cardId => !fromSnapshot.ContainsKey(cardId)))
        {
            var scryfallDocument = await scryfallClient.GetCardAsync(missingId, cancellationToken).ConfigureAwait(false);
            if (scryfallDocument is not null)
            {
                logger.LogInformation("Resolved missing card profile {CardId} via Scryfall fallback", missingId);
                var resolvedProfile = scryfallCardMapper.MapCardProfile(scryfallDocument);
                fromSnapshot[missingId] = resolvedProfile;
            }
            else
            {
                logger.LogWarning("Unable to resolve card profile {CardId} from snapshot or Scryfall fallback", missingId);
            }
        }

        return fromSnapshot;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CardSearchResultContract>> SearchAsync(CardSearchQueryContract request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var snapshot = await metadataStore.LoadSnapshotAsync(cancellationToken).ConfigureAwait(false);
        var searchIndex = GetOrCreateCachedSearchIndex(snapshot);
        var normalizedQuery = request.Query.Trim();
        var colorFilters = request.Colors.Where(color => !string.IsNullOrWhiteSpace(color)).Select(static color => color.Trim()).ToArray();

        if (searchIndex.ExactNameLookup.TryGetValue(normalizedQuery, out var exactMatches))
        {
            var exactResults = exactMatches
                .Where(card => colorFilters.Length == 0 || colorFilters.All(filter => card.ColorIdentity.Contains(filter, StringComparer.OrdinalIgnoreCase)))
                .OrderBy(card => card.Name, StringComparer.OrdinalIgnoreCase)
                .Take(20)
                .ToArray();

            if (exactResults.Length > 0)
            {
                logger.LogDebug("Resolved {ResultCount} exact-name card search results from snapshot {SnapshotId}", exactResults.Length, snapshot.SnapshotId);
                return exactResults;
            }
        }

        var results = searchIndex.SearchIndex.CardSummaries
            .Where(card => card.Name.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase))
            .Where(card => colorFilters.Length == 0 || colorFilters.All(filter => card.ColorIdentity.Contains(filter, StringComparer.OrdinalIgnoreCase)))
            .OrderBy(card => card.Name, StringComparer.OrdinalIgnoreCase)
            .Take(20)
            .ToArray();

        if (results.Length > 0)
        {
            logger.LogDebug("Resolved {ResultCount} card search results from snapshot {SnapshotId}", results.Length, snapshot.SnapshotId);
            return results;
        }

        logger.LogInformation("No local card-search results for query {Query}; falling back to Scryfall autocomplete", normalizedQuery);

        var candidateNames = await scryfallClient.AutocompleteCardNamesAsync(normalizedQuery, cancellationToken).ConfigureAwait(false);
        if (candidateNames.Count == 0)
        {
            logger.LogWarning("Scryfall autocomplete returned no candidates for query {Query}", normalizedQuery);
            return Array.Empty<CardSearchResultContract>();
        }

        var documents = await Task.WhenAll(candidateNames.Select(name => scryfallClient.GetNamedCardAsync(name, cancellationToken))).ConfigureAwait(false);
        var fallbackResults = documents
            .Where(static document => document is not null)
            .Select(document => scryfallCardMapper.MapSearchResult(document!))
            .Take(20)
            .ToArray();

        logger.LogInformation("Resolved {ResultCount} fallback card-search results from Scryfall for query {Query}", fallbackResults.Length, normalizedQuery);
        return fallbackResults;
    }

    /// <inheritdoc />
    public async Task<string?> GetSnapshotVersionAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = await metadataStore.LoadSnapshotAsync(cancellationToken).ConfigureAwait(false);
        return GetOrCreateCachedSearchIndex(snapshot).SearchIndex.Version;
    }

    private CachedSearchIndex GetOrCreateCachedSearchIndex(CardMetadataSnapshot snapshot)
    {
        var currentCache = cachedSearchIndex;
        if (currentCache is not null && currentCache.Matches(snapshot))
        {
            return currentCache;
        }

        lock (searchIndexLock)
        {
            currentCache = cachedSearchIndex;
            if (currentCache is not null && currentCache.Matches(snapshot))
            {
                return currentCache;
            }

            var searchIndex = searchIndexSnapshotBuilder.Build(snapshot);
            var exactNameLookup = searchIndex.CardSummaries
                .GroupBy(card => card.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => group.OrderBy(card => card.Name, StringComparer.OrdinalIgnoreCase).ToArray(),
                    StringComparer.OrdinalIgnoreCase);

            currentCache = new CachedSearchIndex(
                snapshot.SourcePath,
                snapshot.LastUpdatedUtc,
                snapshot.Cards.Count,
                searchIndex,
                exactNameLookup);

            cachedSearchIndex = currentCache;
            return currentCache;
        }
    }

    private sealed record CachedSearchIndex(
        string SnapshotPath,
        DateTime SnapshotLastUpdatedUtc,
        int CardCount,
        SearchIndexSnapshotContract SearchIndex,
        IReadOnlyDictionary<string, CardSearchResultContract[]> ExactNameLookup)
    {
        public bool Matches(CardMetadataSnapshot snapshot) =>
            string.Equals(SnapshotPath, snapshot.SourcePath, StringComparison.OrdinalIgnoreCase)
            && SnapshotLastUpdatedUtc == snapshot.LastUpdatedUtc
            && CardCount == snapshot.Cards.Count;
    }
}