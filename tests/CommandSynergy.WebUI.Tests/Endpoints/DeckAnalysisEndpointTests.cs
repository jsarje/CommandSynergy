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
        using var client = CreateClient(CreateResponse());

        var response = await client.PostAsJsonAsync("/api/decks/analyze", new DeckSnapshotContract
        {
            CommanderCardId = "commander",
            Entries = [ new DeckEntryContract { CardId = "commander", Quantity = 1, IsCommander = true } ],
        });

        var payload = await response.Content.ReadFromJsonAsync<DeckAnalysisResponseContract>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        payload.Should().NotBeNull();
        payload!.Bracket.Level.Should().Be(4);
        payload.PowerLevel.Score.Should().Be(7.4m);
        payload.Synergy.Score.Should().Be(67.5m);
        payload.ThemeAnalysis.Should().NotBeNull();
        payload.Synergy.ThemeScore.Should().Be(65m);
        payload.Synergy.FinalScore.Should().Be(67.5m);
        payload.Synergy.QualitativeLabel.Should().Be("Focused");
    }

    [Fact]
    public async Task Post_analyze_endpoint_preserves_insufficient_theme_payload()
    {
        using var client = CreateClient(new DeckAnalysisResponseContract
        {
            Bracket = CreateResponse().Bracket,
            PowerLevel = CreateResponse().PowerLevel,
            Synergy = new SynergyAssessmentContract
            {
                Score = 0m,
                ThemeScore = 0m,
                FinalScore = 0m,
                QualitativeLabel = "Pile",
                Summary = "Add more cards to evaluate thematic coherence.",
                CommanderSpecificHits = Array.Empty<string>(),
                StapleOverloadIndicators = Array.Empty<string>(),
            },
            ThemeAnalysis = new ThemeAnalysisContract
            {
                RankedThemes = Array.Empty<DeckThemeContract>(),
                PrimaryThemes = Array.Empty<DeckThemeContract>(),
                OffThemeCards = Array.Empty<OffThemeCardContract>(),
                CommanderAlignment = new CommanderAlignmentContract
                {
                    Level = "None",
                    DeckStrengthForCommanderTheme = 0m,
                    EvidenceCardIds = Array.Empty<string>(),
                    Summary = "Add more cards to reveal a deck theme.",
                },
                AnalysedCardCount = 7,
                AnalysedAtUtc = DateTimeOffset.UtcNow,
                IsInsufficient = true,
            },
        });

        var response = await client.PostAsJsonAsync("/api/decks/analyze", new DeckSnapshotContract
        {
            CommanderCardId = "commander",
            Entries = [ new DeckEntryContract { CardId = "commander", Quantity = 1, IsCommander = true } ],
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<DeckAnalysisResponseContract>();

        payload.Should().NotBeNull();
        payload!.ThemeAnalysis!.IsInsufficient.Should().BeTrue();
        payload.Synergy.FinalScore.Should().Be(0m);
        payload.Synergy.QualitativeLabel.Should().Be("Pile");
    }

    [Fact]
    public async Task Post_analyze_endpoint_preserves_alignment_and_edhrec_enhanced_fields()
    {
        using var client = CreateClient(CreateResponse() with
        {
            PowerLevel = new PowerLevelAssessmentContract
            {
                Score = 7.4m,
                Summary = "avg MV 2.1; 2 fast mana cards, 1 efficient tutor, 1 free interaction piece, and 1 included combo.",
            },
            Synergy = new SynergyAssessmentContract
            {
                Score = 67.5m,
                ThemeScore = 65m,
                FinalScore = 74.2m,
                QualitativeLabel = "Focused",
                EdhrecEnhanced = true,
                Summary = "Synergy summary",
                CommanderSpecificHits = ["Engine Card"],
                StapleOverloadIndicators = ["Staple Card"],
            },
            ThemeAnalysis = CreateResponse().ThemeAnalysis! with
            {
                CommanderAlignment = new CommanderAlignmentContract
                {
                    Level = "Strong",
                    CommanderTopTheme = "Tokens",
                    DeckStrengthForCommanderTheme = 0.74m,
                    EvidenceCardIds = ["impact-card"],
                    Summary = "Commander alignment summary.",
                },
            },
        });

        var response = await client.PostAsJsonAsync("/api/decks/analyze", new DeckSnapshotContract
        {
            CommanderCardId = "commander",
            Entries = [ new DeckEntryContract { CardId = "commander", Quantity = 1, IsCommander = true } ],
        });

        var payload = await response.Content.ReadFromJsonAsync<DeckAnalysisResponseContract>();

        payload.Should().NotBeNull();
        payload!.Synergy.EdhrecEnhanced.Should().BeTrue();
        payload.Synergy.FinalScore.Should().Be(74.2m);
        payload.ThemeAnalysis!.CommanderAlignment.Level.Should().Be("Strong");
        payload.ThemeAnalysis.CommanderAlignment.CommanderTopTheme.Should().Be("Tokens");
    }

    private HttpClient CreateClient(DeckAnalysisResponseContract response) => factory.WithWebHostBuilder(builder =>
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IDeckAnalysisService>();
            services.AddSingleton<IDeckAnalysisService>(new StubDeckAnalysisService(response));
        });
    }).CreateClient();

    private static DeckAnalysisResponseContract CreateResponse() => new()
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
        PowerLevel = new PowerLevelAssessmentContract
        {
            Score = 7.4m,
            Summary = "avg MV 2.1; 2 fast mana cards, 1 efficient tutor, 1 free interaction piece, and 1 included combo.",
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
    };

    private sealed class StubDeckAnalysisService : IDeckAnalysisService
    {
        private readonly DeckAnalysisResponseContract response;

        public StubDeckAnalysisService(DeckAnalysisResponseContract response)
        {
            this.response = response;
        }

        public Task<DeckAnalysisResponseContract> AnalyzeAsync(DeckSnapshotContract deckSnapshot, CancellationToken cancellationToken = default) =>
            Task.FromResult(response);
    }
}
