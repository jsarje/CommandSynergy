using System.Net;
using System.Net.Http;
using System.Text;
using CommandSynergy.Application.Configuration;
using CommandSynergy.Application.Contracts;
using CommandSynergy.Infrastructure.CardMetadata;
using CommandSynergy.Infrastructure.Scryfall;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CommandSynergy.Infrastructure.Tests.CardMetadata;

public sealed class CardMetadataQueryServiceTests
{
    [Fact]
    public async Task GetCardProfilesAsync_resolves_uuid_card_ids_via_scryfall_when_snapshot_is_missing()
    {
        var metadataDirectory = Path.Combine(Path.GetTempPath(), $"command-synergy-{Guid.NewGuid():N}");
        Directory.CreateDirectory(metadataDirectory);

        try
        {
            var metadataStore = new ParquetCardMetadataStore(
                Options.Create(new CardMetadataOptions
                {
                    SnapshotDirectory = metadataDirectory,
                    SnapshotFileName = "cards.parquet",
                    SearchIndexVersion = "test-v1",
                }),
                NullLogger<ParquetCardMetadataStore>.Instance);

            var searchIndexBuilder = new SearchIndexSnapshotBuilder(Options.Create(new CardMetadataOptions
            {
                SnapshotDirectory = metadataDirectory,
                SnapshotFileName = "cards.parquet",
                SearchIndexVersion = "test-v1",
            }));

            var scryfallClient = new ScryfallClient(
                new HttpClient(new StubHttpMessageHandler())
                {
                    BaseAddress = new Uri("https://api.scryfall.com/", UriKind.Absolute),
                },
                NullLogger<ScryfallClient>.Instance);

            var sut = new CardMetadataQueryService(
                metadataStore,
                searchIndexBuilder,
                scryfallClient,
                new ScryfallCardMapper(),
                NullLogger<CardMetadataQueryService>.Instance);

            var response = await sut.GetCardProfilesAsync([ "824b2d73-2151-4e5e-9f05-8f63e2bdcaa9" ]);

            response.Should().ContainKey("824b2d73-2151-4e5e-9f05-8f63e2bdcaa9");
            response["824b2d73-2151-4e5e-9f05-8f63e2bdcaa9"].Name.Should().Be("Atraxa, Praetors' Voice");
            response["824b2d73-2151-4e5e-9f05-8f63e2bdcaa9"].IsLegalInCommander.Should().BeTrue();
        }
        finally
        {
            Directory.Delete(metadataDirectory, true);
        }
    }

    [Fact]
    public async Task SearchAsync_returns_scryfall_fallback_results_when_snapshot_is_missing()
    {
        var metadataDirectory = Path.Combine(Path.GetTempPath(), $"command-synergy-{Guid.NewGuid():N}");
        Directory.CreateDirectory(metadataDirectory);

        try
        {
            var metadataStore = new ParquetCardMetadataStore(
                Options.Create(new CardMetadataOptions
                {
                    SnapshotDirectory = metadataDirectory,
                    SnapshotFileName = "cards.parquet",
                    SearchIndexVersion = "test-v1",
                }),
                NullLogger<ParquetCardMetadataStore>.Instance);

            var searchIndexBuilder = new SearchIndexSnapshotBuilder(Options.Create(new CardMetadataOptions
            {
                SnapshotDirectory = metadataDirectory,
                SnapshotFileName = "cards.parquet",
                SearchIndexVersion = "test-v1",
            }));

            var scryfallClient = new ScryfallClient(
                new HttpClient(new StubHttpMessageHandler())
                {
                    BaseAddress = new Uri("https://api.scryfall.com/", UriKind.Absolute),
                },
                NullLogger<ScryfallClient>.Instance);

            var sut = new CardMetadataQueryService(
                metadataStore,
                searchIndexBuilder,
                scryfallClient,
                new ScryfallCardMapper(),
                NullLogger<CardMetadataQueryService>.Instance);

            var response = await sut.SearchAsync(new CardSearchQueryContract
            {
                Query = "sol",
            });

            response.Should().ContainSingle();
            response[0].CardId.Should().Be("sol-ring-id");
            response[0].Name.Should().Be("Sol Ring");
        }
        finally
        {
            Directory.Delete(metadataDirectory, true);
        }
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var pathAndQuery = request.RequestUri?.PathAndQuery ?? string.Empty;

            if (pathAndQuery.StartsWith("/cards/autocomplete?q=sol", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(CreateJsonResponse("""
                    {
                      "data": ["Sol Ring"]
                    }
                    """));
            }

                    if (pathAndQuery.StartsWith("/cards/824b2d73-2151-4e5e-9f05-8f63e2bdcaa9", StringComparison.OrdinalIgnoreCase))
                    {
                    return Task.FromResult(CreateJsonResponse("""
                        {
                          "id": "824b2d73-2151-4e5e-9f05-8f63e2bdcaa9",
                          "oracle_id": "c6fdaae1-2deb-45ba-b9b7-3797f85f0c6e",
                          "name": "Atraxa, Praetors' Voice",
                          "oracle_text": "Flying, vigilance, deathtouch, lifelink\nAt the beginning of your end step, proliferate.",
                          "type_line": "Legendary Creature — Phyrexian Angel Horror",
                          "mana_cost": "{G}{W}{U}{B}",
                          "cmc": 4,
                          "color_identity": ["B", "G", "U", "W"],
                          "image_uris": { "normal": "https://example.test/atraxa.jpg" },
                          "card_faces": [],
                          "legalities": { "commander": "legal" }
                        }
                        """));
                    }

            if (pathAndQuery.StartsWith("/cards/named?exact=Sol%20Ring", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(CreateJsonResponse("""
                    {
                      "id": "sol-ring-id",
                      "oracle_id": "sol-ring-oracle",
                      "name": "Sol Ring",
                      "type_line": "Artifact",
                      "mana_cost": "{1}",
                      "cmc": 1,
                      "color_identity": [],
                      "image_uris": { "normal": "https://example.test/sol-ring.jpg" },
                      "card_faces": [],
                      "legalities": { "commander": "legal" }
                    }
                    """));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }

        private static HttpResponseMessage CreateJsonResponse(string content) => new(HttpStatusCode.OK)
        {
            Content = new StringContent(content, Encoding.UTF8, "application/json"),
        };
    }
}