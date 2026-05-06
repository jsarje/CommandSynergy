using CommandSynergy.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace CommandSynergy.IntegrationTests.Analysis;

public sealed class DeckAnalysisServiceIntegrationTests : IClassFixture<DeckAnalysisIntegrationFixture>
{
    private readonly DeckAnalysisIntegrationFixture fixture;

    public DeckAnalysisServiceIntegrationTests(DeckAnalysisIntegrationFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task AnalyzeAsync_returns_live_analysis_for_fixture_deck()
    {
        if (!fixture.LiveDependenciesEnabled)
        {
            return;
        }

        var response = await fixture.AnalyzeAsync("anim-pakal-live-deck.json");

        response.Bracket.Level.Should().BeGreaterThanOrEqualTo(1);
        response.Bracket.Summary.Should().NotBeNullOrWhiteSpace();
        response.PowerLevel.Score.Should().BeGreaterThan(0m);
        response.Synergy.FinalScore.Should().BeGreaterThanOrEqualTo(0m);
        response.ThemeAnalysis.Should().NotBeNull();
        response.ComboAnalysis.Should().NotBeNull();
        response.DeckStats.Should().NotBeNull();
    }
}
