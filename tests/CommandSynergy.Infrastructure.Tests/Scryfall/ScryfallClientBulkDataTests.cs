using System.Net;
using System.Text;
using CommandSynergy.Infrastructure.Scryfall;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace CommandSynergy.Infrastructure.Tests.Scryfall;

/// <summary>
/// Validates Scryfall bulk-data download behavior.
/// </summary>
public sealed class ScryfallClientBulkDataTests
{
    [Fact]
    public async Task DownloadOracleCardsAsync_returns_bulk_cards_from_manifest_entry()
    {
        var sut = new ScryfallClient(
            new HttpClient(new StubHttpMessageHandler())
            {
                BaseAddress = new Uri("https://api.scryfall.com/", UriKind.Absolute),
            },
            NullLogger<ScryfallClient>.Instance);

        var result = await sut.DownloadOracleCardsAsync();

        result.Should().NotBeNull();
        result!.Type.Should().Be("oracle_cards");
        result.DownloadUri.Should().Be("https://data.scryfall.io/oracle-cards.json");
        result.Cards.Should().ContainSingle();
        result.Cards[0].Id.Should().Be("bulk-card-id");
        result.Cards[0].Name.Should().Be("Bulk Card");
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
                        "id": "bulk-card-id",
                        "oracle_id": "bulk-oracle-id",
                        "name": "Bulk Card",
                        "type_line": "Artifact",
                        "mana_cost": "{2}",
                        "cmc": 2,
                        "color_identity": [],
                        "image_uris": { "normal": "https://example.test/bulk.jpg" },
                        "card_faces": [],
                        "legalities": { "commander": "legal" }
                      }
                    ]
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