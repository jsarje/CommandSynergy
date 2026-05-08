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

    [Theory]
    [InlineData("monks.json")]
    [InlineData("Avacyn.json")]
    [InlineData("Elves.json")]
    [InlineData("Yshtola.json")]
    public async Task AnalyzeAsync_ShouldReturnExpectedAnalysis_WhenFixtureDeckPassed(string deckJson)
    {
        if (!fixture.LiveDependenciesEnabled)
        {
            return;
        }

        var expected = fixture.LoadFixtureExpectations(deckJson);
        var response = await fixture.AnalyzeAsync(deckJson);

        response.Bracket.Level.Should().Be(expected.ExpectedBracketLevel ?? 1);
        response.Bracket.Summary.Should().NotBeNullOrWhiteSpace();
        response.PowerLevel.Score.Should().BeInRange(expected.MinPower ?? 0m, expected.MaxPower ?? decimal.MaxValue);
        response.Synergy.FinalScore.Should().BeInRange(expected.MinSynergy ?? 0m, expected.MaxSynergy ?? decimal.MaxValue);
        response.ThemeAnalysis.Should().NotBeNull();
        if (!string.IsNullOrWhiteSpace(expected.ExpectedTheme))
        {
            response.ThemeAnalysis!.PrimaryThemes.Select(static theme => theme.Name)
                .Should()
                .Contain(theme => string.Equals(theme, expected.ExpectedTheme, StringComparison.OrdinalIgnoreCase));
        }

        response.ComboAnalysis.Should().NotBeNull();
        response.DeckStats.Should().NotBeNull();
    }
}
