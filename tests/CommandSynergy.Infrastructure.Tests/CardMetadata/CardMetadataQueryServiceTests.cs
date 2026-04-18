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
using Parquet.Serialization;

namespace CommandSynergy.Infrastructure.Tests.CardMetadata;

public sealed class CardMetadataQueryServiceTests
{
    [Fact]
    public async Task GetCardProfilesAsync_uses_parquet_snapshot_when_available()
    {
        var metadataDirectory = Path.Combine(Path.GetTempPath(), $"command-synergy-{Guid.NewGuid():N}");
        Directory.CreateDirectory(metadataDirectory);

        try
        {
            await WriteSnapshotAsync(
                metadataDirectory,
                new TestParquetCardMetadataRow
                {
                    CardId = "persistent-petitioners-id",
                    OracleId = "persistent-petitioners-oracle",
                    Name = "Persistent Petitioners",
                    TypeLine = "Creature — Human Advisor",
                    ColorIdentity = [ "U" ],
                    ManaCost = "{1}{U}",
                    ManaValue = 2,
                    SaltScore = 0.4m,
                    ImageUri = "https://example.test/persistent-petitioners.jpg",
                    HasMultipleFaces = false,
                    OracleText = "A deck can have any number of cards named Persistent Petitioners.",
                    PlayRateByCommander = new Dictionary<string, decimal>
                    {
                        ["persistent-petitioners-oracle"] = 0.72m,
                    },
                    GenericColorStapleRate = 0.18m,
                    IsLegalInCommander = true,
                    AllowsMultipleCopies = true,
                    CompanionRequirementCode = "even-mana-value",
                });

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

            var response = await sut.GetCardProfilesAsync([ "persistent-petitioners-id" ]);

            response.Should().ContainKey("persistent-petitioners-id");
            response["persistent-petitioners-id"].Name.Should().Be("Persistent Petitioners");
            response["persistent-petitioners-id"].AllowsMultipleCopies.Should().BeTrue();
            response["persistent-petitioners-id"].CompanionRequirementCode.Should().Be("even-mana-value");
            response["persistent-petitioners-id"].PlayRateByCommander.Should().ContainKey("persistent-petitioners-oracle");
            response["persistent-petitioners-id"].GenericColorStapleRate.Should().Be(0.18m);
        }
        finally
        {
            Directory.Delete(metadataDirectory, true);
        }
    }

    [Fact]
    public async Task GetCardProfilesAsync_resolves_uuid_card_ids_via_scryfall_when_snapshot_is_missing_without_writing_through()
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

            var snapshot = await metadataStore.LoadSnapshotAsync();
            snapshot.Cards.Should().BeEmpty();
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

    private static async Task WriteSnapshotAsync(string metadataDirectory, params TestParquetCardMetadataRow[] rows)
    {
        var snapshotPath = Path.Combine(metadataDirectory, "cards.parquet");
        await using var stream = File.Create(snapshotPath);
        await ParquetSerializer.SerializeAsync(rows, stream);
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

    private sealed class TestParquetCardMetadataRow
    {
        public string CardId { get; init; } = string.Empty;

        public string? OracleId { get; init; }

        public string Name { get; init; } = string.Empty;

        public string TypeLine { get; init; } = string.Empty;

        public string[] ColorIdentity { get; init; } = Array.Empty<string>();

        public string? ManaCost { get; init; }

        public decimal ManaValue { get; init; }

        public decimal? SaltScore { get; init; }

        public string? ImageUri { get; init; }

        public bool HasMultipleFaces { get; init; }

        public string? OracleText { get; init; }

        public Dictionary<string, decimal>? PlayRateByCommander { get; init; }

        public decimal? GenericColorStapleRate { get; init; }

        public bool IsLegalInCommander { get; init; } = true;

        public bool AllowsMultipleCopies { get; init; }

        public string? CompanionRequirementCode { get; init; }
    }
}