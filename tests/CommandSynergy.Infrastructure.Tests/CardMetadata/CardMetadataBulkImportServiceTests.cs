using System.Net;
using System.Text;
using CommandSynergy.Application.Configuration;
using CommandSynergy.Domain.Cards;
using CommandSynergy.Infrastructure.CardMetadata;
using CommandSynergy.Infrastructure.Scryfall;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CommandSynergy.Infrastructure.Tests.CardMetadata;

/// <summary>
/// Validates end-to-end bulk metadata import behavior.
/// </summary>
public sealed class CardMetadataBulkImportServiceTests
{
    [Fact]
    public async Task ImportOracleCardsAsync_replaces_snapshot_with_bulk_imported_cards()
    {
        var metadataDirectory = Path.Combine(Path.GetTempPath(), $"cs-bulk-import-tests-{Guid.NewGuid():N}");
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

            await metadataStore.UpsertCardAsync(new CardProfile
            {
                CardId = "stale-card-id",
                OracleId = "stale-card-oracle",
                Name = "Stale Card",
                TypeLine = "Artifact",
                MetadataSource = CardMetadataSource.UserDrivenScryfallEnrichment,
                FaceProfiles = [new CardFaceProfile("0", "Stale Card", null, "Artifact", null, null, true)],
            });

            var scryfallClient = new ScryfallClient(
                new HttpClient(new StubHttpMessageHandler())
                {
                    BaseAddress = new Uri("https://api.scryfall.com/", UriKind.Absolute),
                },
                NullLogger<ScryfallClient>.Instance);

            var sut = new CardMetadataBulkImportService(
                metadataStore,
                scryfallClient,
                new ScryfallCardMapper(),
                NullLogger<CardMetadataBulkImportService>.Instance);

            var result = await sut.ImportOracleCardsAsync();
            var snapshot = await metadataStore.LoadSnapshotAsync();

            result.CardCount.Should().Be(2);
            snapshot.Cards.Should().HaveCount(2);
            snapshot.Cards.Should().NotContain(card => card.CardId == "stale-card-id");
            snapshot.Cards.Should().OnlyContain(card => card.MetadataSource == CardMetadataSource.BulkSnapshotImport);
            snapshot.Cards.Should().Contain(card => card.CardId == "alpha-card-id" && card.CommanderEligibilityBasis == CommanderEligibilityBasis.LegendaryCreature);
            snapshot.Cards.Should().Contain(card => card.CardId == "alpha-card-id" && card.IsMassLandDenial);
            snapshot.Cards.Should().Contain(card => card.CardId == "beta-card-id" && !card.IsMassLandDenial);
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
            var uri = request.RequestUri?.ToString() ?? string.Empty;

            if (uri.EndsWith("/bulk-data", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(CreateJsonResponse("""
                    {
                      "data": [
                        {
                          "type": "oracle_cards",
                          "download_uri": "https://data.scryfall.io/oracle-cards.json",
                          "updated_at": "2026-04-18T12:00:00Z"
                        }
                      ]
                    }
                    """));
            }

            if (uri.Equals("https://data.scryfall.io/oracle-cards.json", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(CreateJsonResponse("""
                    [
                      {
                        "id": "alpha-card-id",
                        "oracle_id": "alpha-oracle-id",
                        "name": "Alpha Commander",
                        "oracle_text": "Flying",
                        "type_line": "Legendary Creature — Angel",
                        "mana_cost": "{3}{W}{U}",
                        "cmc": 5,
                        "color_identity": ["U", "W"],
                        "image_uris": { "normal": "https://example.test/alpha.jpg" },
                        "card_faces": [],
                        "legalities": { "commander": "legal" }
                      },
                      {
                        "id": "beta-card-id",
                        "oracle_id": "beta-oracle-id",
                        "name": "Beta Signet",
                        "oracle_text": "{T}: Add {U} or {B}.",
                        "type_line": "Artifact",
                        "mana_cost": "{2}",
                        "cmc": 2,
                        "color_identity": ["U", "B"],
                        "image_uris": { "normal": "https://example.test/beta.jpg" },
                        "card_faces": [],
                        "legalities": { "commander": "legal" }
                      }
                    ]
                    """));
            }

            if (uri.Contains("cards/search?q=oracletag%3Amass-land-denial", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(CreateJsonResponse("""
                    {
                      "data": [
                        {
                          "id": "alpha-card-id",
                          "name": "Alpha Commander",
                          "color_identity": ["U", "W"],
                          "card_faces": []
                        }
                      ],
                      "has_more": false,
                      "next_page": null
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