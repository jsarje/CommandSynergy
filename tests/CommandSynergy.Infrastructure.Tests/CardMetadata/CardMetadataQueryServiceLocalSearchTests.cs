using CommandSynergy.Application.Configuration;
using CommandSynergy.Application.Contracts;
using CommandSynergy.Domain.Cards;
using CommandSynergy.Infrastructure.CardMetadata;
using CommandSynergy.Infrastructure.Scryfall;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CommandSynergy.Infrastructure.Tests.CardMetadata;

public sealed class CardMetadataQueryServiceLocalSearchTests
{
    [Fact]
    public async Task SearchAsync_prefers_exact_name_matches_and_applies_color_filters()
    {
        var store = new StubMetadataStore(CreateSnapshot(
            new CardMetadataRecord("sol-ring", "oracle-1", "Sol Ring", "Artifact", Array.Empty<string>(), "{1}", 1m, null, null, false),
            new CardMetadataRecord("sol-ring-white", "oracle-2", "Sol Ring", "Artifact", ["W"], "{1}{W}", 2m, null, null, false),
            new CardMetadataRecord("solar-blast", "oracle-3", "Solar Blast", "Instant", ["R"], "{3}{R}", 4m, null, null, false)));
        var builder = new CountingSearchIndexSnapshotBuilder();
        var sut = CreateSut(store, builder);

        var results = await sut.SearchAsync(new CardSearchQueryContract
        {
            Query = "Sol Ring",
            Colors = ["W"],
        });

        results.Should().ContainSingle();
        results[0].CardId.Should().Be("sol-ring-white");
        builder.BuildCount.Should().Be(1);
    }

    [Fact]
    public async Task SearchAsync_and_GetSnapshotVersion_reuse_cached_search_index_until_snapshot_changes()
    {
        var snapshotV1 = CreateSnapshot(
            new CardMetadataRecord("sol-ring", "oracle-1", "Sol Ring", "Artifact", Array.Empty<string>(), "{1}", 1m, null, null, false));
        var snapshotV2 = new CardMetadataSnapshot(
            "snapshot-v2",
            snapshotV1.SourcePath,
            snapshotV1.LastUpdatedUtc.AddMinutes(5),
            [
                .. snapshotV1.Cards,
                new CardMetadataRecord("arcane-signet", "oracle-2", "Arcane Signet", "Artifact", Array.Empty<string>(), "{2}", 2m, null, null, false),
            ]);
        var store = new StubMetadataStore(snapshotV1);
        var builder = new CountingSearchIndexSnapshotBuilder();
        var sut = CreateSut(store, builder);

        (await sut.GetSnapshotVersionAsync()).Should().Be("search-v1");
        (await sut.SearchAsync(new CardSearchQueryContract { Query = "Sol" })).Should().ContainSingle();
        builder.BuildCount.Should().Be(1);

        store.Snapshot = snapshotV2;

        (await sut.GetSnapshotVersionAsync()).Should().Be("search-v1");
        (await sut.SearchAsync(new CardSearchQueryContract { Query = "Arcane" })).Should().ContainSingle();
        builder.BuildCount.Should().Be(2);
    }

    private static CardMetadataQueryService CreateSut(StubMetadataStore store, CountingSearchIndexSnapshotBuilder builder) =>
        new(
            store,
            builder,
            new UnexpectedScryfallClient(),
            new UnexpectedScryfallCardMapper(),
            NullLogger<CardMetadataQueryService>.Instance);

    private static CardMetadataSnapshot CreateSnapshot(params CardMetadataRecord[] cards) =>
        new(
            "snapshot-v1",
            "/tmp/cards.parquet",
            DateTime.UnixEpoch,
            cards);

    private sealed class StubMetadataStore(CardMetadataSnapshot snapshot) : IParquetCardMetadataStore
    {
        public CardMetadataSnapshot Snapshot { get; set; } = snapshot;

        public Task<CardMetadataSnapshot> LoadSnapshotAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Snapshot);

        public Task UpsertCardAsync(CardProfile card, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task ReplaceSnapshotAsync(IEnumerable<CardProfile> cards, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class CountingSearchIndexSnapshotBuilder : ISearchIndexSnapshotBuilder
    {
        private readonly SearchIndexSnapshotBuilder inner = new(Options.Create(new CardMetadataOptions
        {
            SearchIndexVersion = "search-v1",
        }));

        public int BuildCount { get; private set; }

        public SearchIndexSnapshotContract Build(CardMetadataSnapshot snapshot)
        {
            BuildCount++;
            return inner.Build(snapshot);
        }
    }

    private sealed class UnexpectedScryfallClient : IScryfallClient
    {
        public Task<IReadOnlyList<string>> AutocompleteCardNamesAsync(string query, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Scryfall fallback should not be used in local-search tests.");

        public Task<IReadOnlySet<string>> FetchAllByOracleTagAsync(string tag, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Oracle tag fetch should not be used in local-search tests.");

        public Task<ScryfallCardDocument?> GetCardAsync(string cardIdOrName, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Card lookup should not be used in local-search tests.");

        public Task<ScryfallCardDocument?> GetCardByIdAsync(string cardId, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Card lookup should not be used in local-search tests.");

        public Task<ScryfallCardDocument?> GetNamedCardAsync(string exactName, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Card lookup should not be used in local-search tests.");

        public Task<ScryfallBulkDownloadResult?> DownloadOracleCardsAsync(CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Bulk download should not be used in local-search tests.");

        public Task<ScryfallSearchResponse> SearchCardsAsync(string query, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Search should not be used in local-search tests.");
    }

    private sealed class UnexpectedScryfallCardMapper : IScryfallCardMapper
    {
        public CardProfile MapCardProfile(ScryfallCardDocument document) =>
            throw new InvalidOperationException("Mapping should not be used in local-search tests.");

        public CardProfile MapCardProfile(ScryfallCardDocument document, CardMetadataSource metadataSource, DateTimeOffset? lastSyncedUtc) =>
            throw new InvalidOperationException("Mapping should not be used in local-search tests.");

        public CardProfile MapCardProfile(ScryfallCardDocument document, CardMetadataSource metadataSource, DateTimeOffset? lastSyncedUtc, IReadOnlySet<string> massLandDenialIds) =>
            throw new InvalidOperationException("Mapping should not be used in local-search tests.");

        public CardSearchResultContract MapSearchResult(ScryfallCardDocument document) =>
            throw new InvalidOperationException("Mapping should not be used in local-search tests.");
    }
}
