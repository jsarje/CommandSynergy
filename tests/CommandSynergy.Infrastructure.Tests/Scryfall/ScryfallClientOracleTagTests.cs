using System.Net;
using System.Text;
using CommandSynergy.Infrastructure.Scryfall;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace CommandSynergy.Infrastructure.Tests.Scryfall;

public sealed class ScryfallClientOracleTagTests
{
    [Fact]
    public async Task FetchAllByOracleTagAsync_returns_ids_from_a_single_page()
    {
        var sut = CreateClient(new OracleTagHttpMessageHandler());

        var result = await sut.FetchAllByOracleTagAsync("mass-land-denial");

        result.Should().BeEquivalentTo(["card-a", "card-b"]);
    }

    [Fact]
    public async Task FetchAllByOracleTagAsync_follows_next_page_links()
    {
        var sut = CreateClient(new OracleTagHttpMessageHandler(usePagination: true));

        var result = await sut.FetchAllByOracleTagAsync("mass-land-denial");

        result.Should().BeEquivalentTo(["card-a", "card-b", "card-c"]);
    }

    [Fact]
    public async Task FetchAllByOracleTagAsync_returns_empty_set_for_empty_results()
    {
        var sut = CreateClient(new OracleTagHttpMessageHandler(returnEmpty: true));

        var result = await sut.FetchAllByOracleTagAsync("mass-land-denial");

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task FetchAllByOracleTagAsync_returns_empty_set_when_http_errors_occur()
    {
        var sut = CreateClient(new OracleTagHttpMessageHandler(returnError: true));

        var result = await sut.FetchAllByOracleTagAsync("mass-land-denial");

        result.Should().BeEmpty();
    }

    private static ScryfallClient CreateClient(HttpMessageHandler handler) =>
        new(
            new HttpClient(handler)
            {
                BaseAddress = new Uri("https://api.scryfall.com/", UriKind.Absolute),
            },
            NullLogger<ScryfallClient>.Instance);

    private sealed class OracleTagHttpMessageHandler : HttpMessageHandler
    {
        private readonly bool usePagination;
        private readonly bool returnEmpty;
        private readonly bool returnError;

        public OracleTagHttpMessageHandler(bool usePagination = false, bool returnEmpty = false, bool returnError = false)
        {
            this.usePagination = usePagination;
            this.returnEmpty = returnEmpty;
            this.returnError = returnError;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri?.ToString() ?? string.Empty;

            if (returnError)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
            }

            if (returnEmpty && uri.Contains("cards/search?q=oracletag%3Amass-land-denial", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(CreateJsonResponse("""
                    {
                      "data": [],
                      "has_more": false,
                      "next_page": null
                    }
                    """));
            }

            if (usePagination && uri.Equals("https://api.scryfall.com/cards/search?page=2", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(CreateJsonResponse("""
                    {
                      "data": [
                        { "id": "card-c", "name": "Card C", "color_identity": [], "card_faces": [] }
                      ],
                      "has_more": false,
                      "next_page": null
                    }
                    """));
            }

            if (uri.Contains("cards/search?q=oracletag%3Amass-land-denial", StringComparison.OrdinalIgnoreCase))
            {
                var firstPage = usePagination
                    ? """
                    {
                      "data": [
                        { "id": "card-a", "name": "Card A", "color_identity": [], "card_faces": [] },
                        { "id": "card-b", "name": "Card B", "color_identity": [], "card_faces": [] }
                      ],
                      "has_more": true,
                      "next_page": "https://api.scryfall.com/cards/search?page=2"
                    }
                    """
                    : """
                    {
                      "data": [
                        { "id": "card-a", "name": "Card A", "color_identity": [], "card_faces": [] },
                        { "id": "card-b", "name": "Card B", "color_identity": [], "card_faces": [] }
                      ],
                      "has_more": false,
                      "next_page": null
                    }
                    """;

                return Task.FromResult(CreateJsonResponse(firstPage));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }

        private static HttpResponseMessage CreateJsonResponse(string content) => new(HttpStatusCode.OK)
        {
            Content = new StringContent(content, Encoding.UTF8, "application/json"),
        };
    }
}
