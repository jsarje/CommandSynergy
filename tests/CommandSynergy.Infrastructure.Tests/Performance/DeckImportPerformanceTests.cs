using System.Diagnostics;
using System.Net;
using System.Text;
using CommandSynergy.Application.Analysis;
using CommandSynergy.Application.Cards;
using CommandSynergy.Application.Configuration;
using CommandSynergy.Application.Contracts;
using CommandSynergy.Application.Decks.Portability;
using CommandSynergy.Application.Decks.Portability.Formats;
using CommandSynergy.Domain.Cards;
using CommandSynergy.Infrastructure.CardMetadata;
using CommandSynergy.Infrastructure.Scryfall;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit.Abstractions;

namespace CommandSynergy.Infrastructure.Tests.Performance;

public sealed class DeckImportPerformanceTests
{
    private static readonly TimeSpan RemoteMissLatency = TimeSpan.FromMilliseconds(75);

    private readonly ITestOutputHelper testOutputHelper;

    public DeckImportPerformanceTests(ITestOutputHelper testOutputHelper)
    {
        this.testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task ImportAsync_completes_within_budget_for_standard_commander_deck_with_mostly_local_metadata()
    {
        var metadataDirectory = Path.Combine(Path.GetTempPath(), $"cs-import-perf-{Guid.NewGuid():N}");
        Directory.CreateDirectory(metadataDirectory);

        try
        {
            var metadataStore = new ParquetCardMetadataStore(
                Options.Create(new CardMetadataOptions
                {
                    SnapshotDirectory = metadataDirectory,
                    SnapshotFileName = "cards.parquet",
                    SearchIndexVersion = "perf-v1",
                }),
                NullLogger<ParquetCardMetadataStore>.Instance);

            await metadataStore.ReplaceSnapshotAsync(BuildLocalProfiles());

            var scryfallClient = new ScryfallClient(
                new HttpClient(new DelayedImportPerformanceHttpMessageHandler(RemoteMissLatency))
                {
                    BaseAddress = new Uri("https://api.scryfall.com/", UriKind.Absolute),
                },
                NullLogger<ScryfallClient>.Instance);

            var queryService = new CardMetadataQueryService(
                metadataStore,
                new SearchIndexSnapshotBuilder(Options.Create(new CardMetadataOptions
                {
                    SnapshotDirectory = metadataDirectory,
                    SnapshotFileName = "cards.parquet",
                    SearchIndexVersion = "perf-v1",
                })),
                scryfallClient,
                new ScryfallCardMapper(new ThemeMatchingService()),
                NullLogger<CardMetadataQueryService>.Instance);

            var cardSearchService = new CardSearchService(queryService);
            var formatRegistry = new DeckFormatRegistry([new GenericPlaintextFormatProfile()]);
            var sut = new DeckImportService(cardSearchService, new DeckFormatDetectionService(formatRegistry), TimeProvider.System);

            var started = Stopwatch.GetTimestamp();
            var result = await sut.ImportAsync(new DeckImportRequestContract
            {
                RawDocumentText = BuildDecklistDocument(),
            });
            var elapsed = Stopwatch.GetElapsedTime(started);

            testOutputHelper.WriteLine("Imported {0} cards in {1:0.00} ms with {2} remote misses at {3} ms each.",
                result.ImportedDeck.NormalizedDeck.ImportedCardCount,
                elapsed.TotalMilliseconds,
                5,
                RemoteMissLatency.TotalMilliseconds);

            result.ImportedDeck.NormalizedDeck.ImportedCardCount.Should().Be(100);
            result.ImportedDeck.NormalizedDeck.Entries.Should().HaveCount(100);
            result.Diagnostics.Should().BeEmpty();
            elapsed.Should().BeLessThan(TimeSpan.FromSeconds(2));
        }
        finally
        {
            Directory.Delete(metadataDirectory, true);
        }
    }

    private static IReadOnlyList<CardProfile> BuildLocalProfiles() =>
        Enumerable.Range(1, 95)
            .Select(index => new CardProfile
            {
                CardId = $"local-card-{index}",
                OracleId = $"local-oracle-{index}",
                Name = $"Local Card {index}",
                ManaCost = "{2}",
                ManaValue = 2,
                TypeLine = index == 1 ? "Legendary Creature" : "Artifact",
                ColorIdentity = index == 1 ? ["G", "W", "U", "B"] : Array.Empty<string>(),
                IsLegalInCommander = true,
                CommanderEligibilityBasis = index == 1 ? CommanderEligibilityBasis.LegendaryCreature : CommanderEligibilityBasis.Unknown,
                MetadataSource = CardMetadataSource.BulkSnapshotImport,
                FaceProfiles = [new CardFaceProfile("0", $"Local Card {index}", "{2}", index == 1 ? "Legendary Creature" : "Artifact", null, null, true)],
            })
            .ToArray();

    private static string BuildDecklistDocument()
    {
        var lines = Enumerable.Range(1, 95)
            .Select(index => $"1 Local Card {index}")
            .Concat(Enumerable.Range(96, 5).Select(index => $"1 Remote Card {index}"));

        return string.Join(Environment.NewLine, lines);
    }

    private sealed class DelayedImportPerformanceHttpMessageHandler : HttpMessageHandler
    {
        private readonly TimeSpan remoteMissLatency;

        public DelayedImportPerformanceHttpMessageHandler(TimeSpan remoteMissLatency)
        {
            this.remoteMissLatency = remoteMissLatency;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var pathAndQuery = request.RequestUri?.PathAndQuery ?? string.Empty;

            if (pathAndQuery.StartsWith("/cards/autocomplete?q=Remote%20Card%20", StringComparison.OrdinalIgnoreCase))
            {
                await Task.Delay(remoteMissLatency, cancellationToken).ConfigureAwait(false);

                var cardName = Uri.UnescapeDataString(pathAndQuery.Split('=')[1]);
                return CreateJsonResponse($$"""
                    {
                      "data": ["{{cardName}}"]
                    }
                    """);
            }

            if (pathAndQuery.StartsWith("/cards/named?exact=Remote%20Card%20", StringComparison.OrdinalIgnoreCase))
            {
                await Task.Delay(remoteMissLatency, cancellationToken).ConfigureAwait(false);

                var cardName = Uri.UnescapeDataString(pathAndQuery.Split('=')[1]);
                var suffix = cardName[(cardName.LastIndexOf(' ') + 1)..];
                return CreateJsonResponse($$"""
                    {
                      "id": "remote-card-{{suffix}}",
                      "oracle_id": "remote-oracle-{{suffix}}",
                      "name": "{{cardName}}",
                      "type_line": "Artifact",
                      "mana_cost": "{3}",
                      "cmc": 3,
                      "color_identity": [],
                      "image_uris": { "normal": "https://example.test/remote-card-{{suffix}}.jpg" },
                      "card_faces": [],
                      "legalities": { "commander": "legal" }
                    }
                    """);
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        private static HttpResponseMessage CreateJsonResponse(string content) => new(HttpStatusCode.OK)
        {
            Content = new StringContent(content, Encoding.UTF8, "application/json"),
        };
    }
}
