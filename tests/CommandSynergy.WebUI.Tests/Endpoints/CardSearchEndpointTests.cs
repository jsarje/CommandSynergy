using System.Net;
using System.Net.Http.Json;
using CommandSynergy.Application.Abstractions;
using CommandSynergy.Application.Contracts;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CommandSynergy.WebUI.Tests.Endpoints;

public sealed class CardSearchEndpointTests : IClassFixture<TestApplicationFactory>
{
    private readonly TestApplicationFactory factory;

    public CardSearchEndpointTests(TestApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Get_search_endpoint_returns_expected_payload()
    {
        using var client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ICardSearchService>();
                services.AddSingleton<ICardSearchService>(new StubCardSearchService());
            });
        }).CreateClient();

        var response = await client.GetAsync("/api/cards/search?q=sol");
        var payload = await response.Content.ReadFromJsonAsync<CardSearchResponseContract>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        payload.Should().NotBeNull();
        payload!.Results.Should().ContainSingle(result => result.CardId == "sol-ring");
    }

    private sealed class StubCardSearchService : ICardSearchService
    {
        public Task<CardSearchResponseContract> SearchAsync(CardSearchQueryContract request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new CardSearchResponseContract
            {
                SnapshotVersion = "test",
                Results =
                [
                    new CardSearchResultContract
                    {
                        CardId = "sol-ring",
                        Name = "Sol Ring",
                        TypeLine = "Artifact",
                        ColorIdentity = Array.Empty<string>(),
                    },
                ],
            });
    }
}