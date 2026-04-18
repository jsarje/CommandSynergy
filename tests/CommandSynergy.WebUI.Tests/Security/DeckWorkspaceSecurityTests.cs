using System.Net;
using System.Net.Http.Json;
using CommandSynergy.Application.Abstractions;
using CommandSynergy.Application.Contracts;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CommandSynergy.WebUI.Tests.Security;

public sealed class DeckWorkspaceSecurityTests : IClassFixture<TestApplicationFactory>
{
    private readonly TestApplicationFactory factory;

    public DeckWorkspaceSecurityTests(TestApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Get_search_endpoint_rejects_blank_queries()
    {
        using var client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ICardSearchService>();
                services.AddSingleton<ICardSearchService>(new StubCardSearchService());
            });
        }).CreateClient();

        var response = await client.GetAsync("/api/cards/search?q=%20%20%20");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_validate_endpoint_rejects_pathological_entry_counts()
    {
        using var client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IDeckValidationService>();
                services.AddSingleton<IDeckValidationService>(new StubDeckValidationService());
            });
        }).CreateClient();

        var payload = new DeckSnapshotContract
        {
            CommanderCardId = "commander",
            Entries = Enumerable.Range(0, 251)
                .Select(index => new DeckEntryContract
                {
                    CardId = $"card-{index}",
                    Quantity = 1,
                    IsCommander = index == 0,
                })
                .ToArray(),
        };

        var response = await client.PostAsJsonAsync("/api/decks/validate", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private sealed class StubCardSearchService : ICardSearchService
    {
        public Task<CardSearchResponseContract> SearchAsync(CardSearchQueryContract request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new CardSearchResponseContract
            {
                SnapshotVersion = "test",
                Results = Array.Empty<CardSearchResultContract>(),
            });
    }

    private sealed class StubDeckValidationService : IDeckValidationService
    {
        public Task<DeckValidationResponseContract> ValidateAsync(DeckSnapshotContract request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new DeckValidationResponseContract
            {
                IsValid = true,
                DeckCardCount = request.Entries.Sum(entry => entry.Quantity),
                Findings = Array.Empty<ValidationFindingContract>(),
            });
    }
}