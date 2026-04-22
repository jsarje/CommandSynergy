using System.Diagnostics;
using CommandSynergy.Application.Analysis;
using CommandSynergy.Application.Tests.Analysis;
using FluentAssertions;

namespace CommandSynergy.Application.Tests.Performance;

public sealed class ThemeAnalysisPerformanceTests
{
    private readonly ThemeAnalysisService sut = new(new ThemeMatchingService(), new AnalysisExplanationBuilder());

    [Fact]
    public async Task AnalyseAsync_completes_within_budget_for_full_hundred_card_deck()
    {
        var fixture = ThemeAnalysisTestData.CreateLargeFocusedDeck();

        var started = Stopwatch.GetTimestamp();
        var (analysis, synergy) = await sut.AnalyseAsync(fixture.Deck, fixture.Profiles, fixture.EdhrecInsights);
        var elapsed = Stopwatch.GetElapsedTime(started);

        analysis.RankedThemes.Should().Contain(theme => theme.Name == "Tokens");
        synergy.FinalScore.Should().BeGreaterThan(60m);
        elapsed.Should().BeLessThan(TimeSpan.FromSeconds(3));
    }

    [Fact]
    public async Task AnalyseAsync_completes_within_incremental_budget_after_single_card_change()
    {
        var fixture = ThemeAnalysisTestData.CreateLargeFocusedDeck();
        fixture.Deck.UpsertEntry("late-cut", 1);

        var profiles = new Dictionary<string, Domain.Cards.CardProfile>(fixture.Profiles, StringComparer.OrdinalIgnoreCase)
        {
            ["late-cut"] = new()
            {
                CardId = "late-cut",
                Name = "Late Cut",
                OracleId = "late-cut-oracle",
                ManaValue = 2,
                TypeLine = string.Empty,
                ThemeSignals = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase),
                FaceProfiles = [ new Domain.Cards.CardFaceProfile("0", "Late Cut", null, string.Empty, null, null, true) ],
            },
        };

        var started = Stopwatch.GetTimestamp();
        var (analysis, _) = await sut.AnalyseAsync(fixture.Deck, profiles, fixture.EdhrecInsights);
        var elapsed = Stopwatch.GetElapsedTime(started);

        analysis.OffThemeCards.Should().Contain(card => card.CardId == "late-cut");
        elapsed.Should().BeLessThan(TimeSpan.FromSeconds(1));
    }
}