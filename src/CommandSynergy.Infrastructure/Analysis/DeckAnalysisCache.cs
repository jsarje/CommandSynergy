using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CommandSynergy.Application.Abstractions;
using CommandSynergy.Application.Analysis;
using CommandSynergy.Application.Contracts;
using Microsoft.Extensions.Caching.Distributed;

namespace CommandSynergy.Infrastructure.Analysis;

/// <summary>
/// Provides shared cache helpers for deck analysis responses.
/// </summary>
public sealed class DeckAnalysisCache(IDistributedCache distributedCache) : IDeckAnalysisCache
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
    private const string CacheKeyPrefix = "DeckAnalysis";

    /// <summary>
    /// Tries to get a cached response for the supplied key.
    /// </summary>
    public bool TryGet(string cacheKey, out DeckAnalysisResponseContract response)
    {
        var payload = distributedCache.GetString(cacheKey);
        if (payload is null)
        {
            response = null!;
            return false;
        }

        try
        {
            response = JsonSerializer.Deserialize<DeckAnalysisResponseContract>(payload)!;
            return response is not null;
        }
        catch (JsonException)
        {
            response = null!;
            return false;
        }
    }

    /// <summary>
    /// Stores a response for the supplied cache key.
    /// </summary>
    public void Set(string cacheKey, DeckAnalysisResponseContract response)
    {
        distributedCache.SetString(
            cacheKey,
            JsonSerializer.Serialize(response),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheDuration,
            });
    }

    /// <summary>
    /// Creates a cache key from the submitted deck state and metadata snapshot version.
    /// </summary>
    public string CreateKey(DeckSnapshotContract deckSnapshot, string? snapshotVersion)
    {
        var builder = new StringBuilder();
        builder.Append(snapshotVersion ?? "missing").Append('|');
        builder.Append(deckSnapshot.CommanderCardId).Append('|');
        builder.Append(deckSnapshot.CompanionCardId).Append('|');

        foreach (var entry in deckSnapshot.Entries.OrderBy(entry => entry.CardId, StringComparer.OrdinalIgnoreCase))
        {
            builder
                .Append(entry.CardId)
                .Append(':')
                .Append(entry.Quantity)
                .Append(':')
                .Append(entry.IsCommander)
                .Append(':')
                .Append(entry.IsCompanion)
                .Append(':')
                .Append(entry.AssignedPileId)
                .Append('|');
        }

        foreach (var pile in deckSnapshot.Piles.OrderBy(pile => pile.PileId, StringComparer.OrdinalIgnoreCase))
        {
            builder.Append(pile.PileId).Append(':').Append(pile.Name).Append(':').Append(pile.SortOrder).Append('|');
        }

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()));
        return string.Concat(CacheKeyPrefix, "|", Convert.ToHexString(hash));
    }
}

/// <summary>
/// Decorates deck analysis with cache lookup and telemetry.
/// </summary>
public sealed class CachedDeckAnalysisService : IDeckAnalysisService
{
    private readonly IDeckAnalysisCoreService inner;
    private readonly IDeckAnalysisCache cache;
    private readonly ICardCatalogGateway cardCatalogGateway;
    private readonly IAnalysisTelemetry telemetry;

    /// <summary>
    /// Creates a cache-aware deck analysis decorator.
    /// </summary>
    public CachedDeckAnalysisService(
        IDeckAnalysisCoreService inner,
        IDeckAnalysisCache cache,
        ICardCatalogGateway cardCatalogGateway,
        IAnalysisTelemetry telemetry)
    {
        this.inner = inner;
        this.cache = cache;
        this.cardCatalogGateway = cardCatalogGateway;
        this.telemetry = telemetry;
    }

    /// <inheritdoc />
    public async Task<DeckAnalysisResponseContract> AnalyzeAsync(DeckSnapshotContract deckSnapshot, CancellationToken cancellationToken = default)
    {
        var snapshotVersion = await cardCatalogGateway.GetSnapshotVersionAsync(cancellationToken).ConfigureAwait(false);
        var cacheKey = cache.CreateKey(deckSnapshot, snapshotVersion);

        if (cache.TryGet(cacheKey, out var cachedResponse))
        {
            telemetry.RecordCacheHit(cacheKey);
            return cachedResponse;
        }

        telemetry.RecordCacheMiss(cacheKey);
        var response = await inner.AnalyzeAsync(deckSnapshot, cancellationToken).ConfigureAwait(false);
        cache.Set(cacheKey, response);
        telemetry.RecordAnalysisCompleted(response.Bracket.Level, response.Synergy.Score);
        return response;
    }
}
