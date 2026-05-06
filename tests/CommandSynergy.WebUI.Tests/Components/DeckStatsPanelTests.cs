using Bunit;
using CommandSynergy.Application.Contracts;
using CommandSynergy.Components.Decks;
using FluentAssertions;
using MudBlazor.Services;

namespace CommandSynergy.WebUI.Tests.Components;

/// <summary>
/// Validates the deferred background-loading deck-stats experience and ready-state rendering.
/// </summary>
public sealed class DeckStatsPanelTests : BunitContext
{
    public DeckStatsPanelTests()
    {
        Services.AddMudServices();
    }

    [Fact]
    public void Deck_stats_panel_renders_loading_state_when_stats_are_not_available_yet()
    {
        var cut = Render<DeckStatsPanel>(parameters => parameters
            .Add(component => component.IsLoading, true)
            .Add(component => component.HasError, false)
            .Add(component => component.Analysis, null));

        cut.Find("[data-testid='deck-stats-loading']").Should().NotBeNull();
        cut.Markup.Should().Contain("Loading deck stats");
    }

    [Fact]
    public void Deck_stats_panel_shows_background_loading_shell_before_rendering_charts()
    {
        var cut = Render<DeckStatsPanel>(parameters => parameters
            .Add(component => component.IsLoading, false)
            .Add(component => component.HasError, false)
            .Add(component => component.Analysis, CreateAnalysis()));

        cut.Find("[data-testid='deck-stats-lazy']").Should().NotBeNull();
        cut.Markup.Should().Contain("Loading charts in the background");
        cut.Markup.Should().NotContain("deck-stats-load-button");
    }

    [Fact]
    public void Deck_stats_panel_renders_all_requested_charts_after_background_loading()
    {
        var cut = Render<DeckStatsPanel>(parameters => parameters
            .Add(component => component.IsLoading, false)
            .Add(component => component.HasError, false)
            .Add(component => component.Analysis, CreateAnalysis()));

        cut.WaitForAssertion(() => cut.Find("[data-testid='deck-stats-mana-values']"));

        cut.Find("[data-testid='deck-stats-mana-values']").TextContent.Should().Contain("Mana values");
        cut.Find("[data-testid='deck-stats-mana-cost']").TextContent.Should().Contain("Mana cost mix");
        cut.Find("[data-testid='deck-stats-mana-generation']").TextContent.Should().Contain("Mana generation");
        cut.Find("[data-testid='deck-stats-card-types']").TextContent.Should().Contain("Card types");
        cut.Markup.Should().NotContain("Mana curve");
    }

    private static DeckAnalysisResponseContract CreateAnalysis() => new()
    {
        Bracket = new BracketAssessmentContract
        {
            Level = 2,
            TotalWeight = 3m,
            Summary = "Bracket summary.",
            Factors = Array.Empty<BracketFactorContract>(),
        },
        PowerLevel = new PowerLevelAssessmentContract
        {
            Score = 5.5m,
            Summary = "Power summary.",
        },
        Synergy = new SynergyAssessmentContract
        {
            Score = 70m,
            Summary = "Synergy summary.",
            CommanderSpecificHits = Array.Empty<string>(),
            StapleOverloadIndicators = Array.Empty<string>(),
        },
        DeckStats = new DeckStatsContract
        {
            ManaValueHistogram =
            [
                new DeckStatSliceContract { Label = "0", Value = 0m, Share = 0m },
                new DeckStatSliceContract { Label = "1", Value = 2m, Share = 0.2m },
                new DeckStatSliceContract { Label = "2", Value = 4m, Share = 0.4m },
                new DeckStatSliceContract { Label = "3", Value = 3m, Share = 0.3m },
                new DeckStatSliceContract { Label = "4", Value = 1m, Share = 0.1m },
                new DeckStatSliceContract { Label = "5", Value = 0m, Share = 0m },
                new DeckStatSliceContract { Label = "6", Value = 0m, Share = 0m },
                new DeckStatSliceContract { Label = "7", Value = 0m, Share = 0m },
                new DeckStatSliceContract { Label = "8+", Value = 0m, Share = 0m },
            ],
            ManaCostDistribution =
            [
                new DeckStatSliceContract { Label = "White", Value = 3m, Share = 0.3m },
                new DeckStatSliceContract { Label = "Blue", Value = 2m, Share = 0.2m },
                new DeckStatSliceContract { Label = "Colorless", Value = 5m, Share = 0.5m },
            ],
            ManaGenerationDistribution =
            [
                new DeckStatSliceContract { Label = "Green", Value = 3m, Share = 0.6m },
                new DeckStatSliceContract { Label = "Any", Value = 2m, Share = 0.4m },
            ],
            CardTypeDistribution =
            [
                new DeckStatSliceContract { Label = "Creature", Value = 5m, Share = 0.5m },
                new DeckStatSliceContract { Label = "Instant", Value = 2m, Share = 0.2m },
                new DeckStatSliceContract { Label = "Land", Value = 3m, Share = 0.3m },
            ],
            ManaCurve = new ManaCurveContract
            {
                Buckets =
                [
                    new DeckStatSliceContract { Label = "0", Value = 0m, Share = 0m },
                    new DeckStatSliceContract { Label = "1", Value = 2m, Share = 0.22m },
                    new DeckStatSliceContract { Label = "2", Value = 4m, Share = 0.44m },
                    new DeckStatSliceContract { Label = "3", Value = 2m, Share = 0.22m },
                    new DeckStatSliceContract { Label = "4", Value = 1m, Share = 0.11m },
                    new DeckStatSliceContract { Label = "5", Value = 0m, Share = 0m },
                    new DeckStatSliceContract { Label = "6", Value = 0m, Share = 0m },
                    new DeckStatSliceContract { Label = "7", Value = 0m, Share = 0m },
                    new DeckStatSliceContract { Label = "8+", Value = 0m, Share = 0m },
                ],
                AverageManaValue = 2.3m,
                SpellCount = 9,
            },
        },
    };
}
