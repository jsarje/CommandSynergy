using System.Net;
using System.Net.Http.Json;
using CommandSynergy.Application.Abstractions;
using CommandSynergy.Application.Contracts;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CommandSynergy.WebUI.Tests.Endpoints;

public sealed class DeckAnalysisEndpointTests : IClassFixture<TestApplicationFactory>
{
    private readonly TestApplicationFactory factory;

    public DeckAnalysisEndpointTests(TestApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Post_analyze_endpoint_returns_the_expected_payload()
    {
        using var client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IDeckAnalysisService>();
                services.AddSingleton<IDeckAnalysisService>(new StubDeckAnalysisService());
            });
        }).CreateClient();

        var response = await client.PostAsJsonAsync("/api/decks/analyze", new DeckSnapshotContract
        {
            CommanderCardId = "commander",
            Entries = [ new DeckEntryContract { CardId = "commander", Quantity = 1, IsCommander = true } ],
        });

        var payload = await response.Content.ReadFromJsonAsync<DeckAnalysisResponseContract>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        payload.Should().NotBeNull();
        payload!.Bracket.Level.Should().Be(4);
        payload.Synergy.Score.Should().Be(67.5m);
        payload.ThemeAnalysis.Should().NotBeNull();
    }

    private sealed class StubDeckAnalysisService : IDeckAnalysisService
    {
        public Task<DeckAnalysisResponseContract> AnalyzeAsync(DeckSnapshotContract deckSnapshot, CancellationToken cancellationToken = default) =>
            Task.FromResult(new DeckAnalysisResponseContract
            {
                Bracket = new BracketAssessmentContract
                {
                    Level = 4,
                    TotalWeight = 12.5m,
                    Summary = "Bracket summary",
                    Factors =
                    [
                        new BracketFactorContract
                        {
                            Category = "game-changer",
                            Weight = 4m,
                            Explanation = "Configured game changer.",
                            SourceCardId = "impact-card",
                        },
                    ],
                },
                Synergy = new SynergyAssessmentContract
                {
                    Score = 67.5m,
                    ThemeScore = 65m,
                    FinalScore = 67.5m,
                    QualitativeLabel = "Focused",
                    Summary = "Synergy summary",
                    CommanderSpecificHits = ["Engine Card"],
                    StapleOverloadIndicators = ["Staple Card"],
                },
                ThemeAnalysis = new ThemeAnalysisContract
                {
                    RankedThemes = Array.Empty<DeckThemeContract>(),
                    PrimaryThemes = Array.Empty<DeckThemeContract>(),
                    OffThemeCards = Array.Empty<OffThemeCardContract>(),
                    CommanderAlignment = new CommanderAlignmentContract
                    {
                        Level = "Moderate",
                        DeckStrengthForCommanderTheme = 0.42m,
                        EvidenceCardIds = Array.Empty<string>(),
                        Summary = "Commander alignment summary.",
                    },
                    AnalysedCardCount = 99,
                    AnalysedAtUtc = DateTimeOffset.UtcNow,
                },
            });
    }
}