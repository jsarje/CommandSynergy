using System.Net;
using System.Net.Http.Json;
using CommandSynergy.Application.Abstractions;
using CommandSynergy.Application.Contracts;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CommandSynergy.WebUI.Tests.Endpoints;

public sealed class DeckValidationEndpointTests : IClassFixture<TestApplicationFactory>
{
    private readonly TestApplicationFactory factory;

    public DeckValidationEndpointTests(TestApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Post_validate_endpoint_returns_expected_findings()
    {
        using var client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IDeckValidationService>();
                services.AddSingleton<IDeckValidationService>(new StubDeckValidationService());
            });
        }).CreateClient();

        var response = await client.PostAsJsonAsync("/api/decks/validate", new DeckSnapshotContract
        {
            CommanderCardId = "commander",
            Entries = [ new DeckEntryContract { CardId = "commander", Quantity = 1, IsCommander = true } ],
        });

        var payload = await response.Content.ReadFromJsonAsync<DeckValidationResponseContract>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        payload.Should().NotBeNull();
        payload!.Findings.Should().ContainSingle(finding => finding.Code == "deck-size");
    }

    private sealed class StubDeckValidationService : IDeckValidationService
    {
        public Task<DeckValidationResponseContract> ValidateAsync(DeckSnapshotContract deckSnapshot, CancellationToken cancellationToken = default) =>
            Task.FromResult(new DeckValidationResponseContract
            {
                IsValid = false,
                DeckCardCount = 1,
                Findings =
                [
                    new ValidationFindingContract
                    {
                        Severity = "error",
                        Code = "deck-size",
                        Message = "Commander decks must contain exactly 100 cards.",
                    },
                ],
            });
    }
}