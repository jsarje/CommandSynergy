using Bunit;
using CommandSynergy.Application.Contracts;
using CommandSynergy.Components.Decks;
using FluentAssertions;
using MudBlazor.Services;

namespace CommandSynergy.WebUI.Tests.Components;

/// <summary>
/// Validates the loading, empty, error, and ready states of <see cref="AnalysisPanel"/>.
/// </summary>
public sealed class AnalysisPanelTests : BunitContext
{
    public AnalysisPanelTests()
    {
        Services.AddMudServices();
    }

    [Fact]
    public void Analysis_panel_renders_loading_state_when_is_loading_is_true()
    {
        var cut = Render<AnalysisPanel>(parameters => parameters
            .Add(component => component.IsLoading, true)
            .Add(component => component.Analysis, null)
            .Add(component => component.HasError, false));

        cut.Find("[data-testid='analysis-panel-loading']").Should().NotBeNull();
        cut.Markup.Should().Contain("Fetching bracket, power, and synergy results");
    }

    [Fact]
    public void Analysis_panel_renders_empty_state_when_analysis_is_null_and_not_loading()
    {
        var cut = Render<AnalysisPanel>(parameters => parameters
            .Add(component => component.IsLoading, false)
            .Add(component => component.Analysis, null)
            .Add(component => component.HasError, false));

        cut.Find("[data-testid='analysis-panel-empty']").Should().NotBeNull();
        cut.Markup.Should().Contain("Bracket, power, and synergy feedback arrives after the first successful sync.");
    }

    [Fact]
    public void Analysis_panel_renders_error_state_when_has_error_is_true()
    {
        var cut = Render<AnalysisPanel>(parameters => parameters
            .Add(component => component.IsLoading, false)
            .Add(component => component.Analysis, null)
            .Add(component => component.HasError, true));

        cut.Find("[data-testid='analysis-panel-error']").Should().NotBeNull();
        cut.Markup.Should().Contain("temporarily unavailable");
    }

    [Fact]
    public void Analysis_panel_renders_bracket_level_and_synergy_score_in_ready_state()
    {
        var cut = Render<AnalysisPanel>(parameters => parameters
            .Add(component => component.IsLoading, false)
            .Add(component => component.HasError, false)
            .Add(component => component.Analysis, CreateAnalysis(bracketLevel: 3, synergyScore: 74.2m)));

        cut.Find("[data-testid='analysis-panel-ready']").Should().NotBeNull();
        cut.Find("[data-testid='analysis-bracket']").TextContent.Should().Contain("Bracket 3");
        cut.Find("[data-testid='analysis-power']").TextContent.Should().Contain("6.7");
        cut.Find("[data-testid='analysis-synergy']").TextContent.Should().Contain("74.2");
    }

    [Fact]
    public void Analysis_panel_renders_bracket_factors_when_present()
    {
        var analysis = CreateAnalysis(bracketLevel: 4, synergyScore: 55m) with
        {
            Bracket = CreateBracket(4, 10m, "High power table") with
            {
                Factors =
                [
                    new BracketFactorContract
                    {
                        Category = "game-changer",
                        Weight = 4m,
                        Explanation = "Mana Crypt is a configured game changer.",
                        SourceCardId = "mana-crypt",
                    },
                ],
            },
        };

        var cut = Render<AnalysisPanel>(parameters => parameters
            .Add(component => component.IsLoading, false)
            .Add(component => component.HasError, false)
            .Add(component => component.Analysis, analysis));

        var factors = cut.Find("[data-testid='bracket-factors']");
        factors.TextContent.Should().Contain("Mana Crypt is a configured game changer.");
        factors.TextContent.Should().Contain("+4");
    }

    [Fact]
    public void Analysis_panel_renders_commander_specific_hits_and_staple_overload_indicators()
    {
        var analysis = CreateAnalysis(bracketLevel: 3, synergyScore: 80m) with
        {
            Synergy = new SynergyAssessmentContract
            {
                Score = 80m,
                Summary = "Strong commander alignment.",
                CommanderSpecificHits = ["Graveyard Enabler", "Sacrifice Outlet"],
                StapleOverloadIndicators = ["Rhystic Study"],
            },
        };

        var cut = Render<AnalysisPanel>(parameters => parameters
            .Add(component => component.IsLoading, false)
            .Add(component => component.HasError, false)
            .Add(component => component.Analysis, analysis));

        cut.Find("[data-testid='synergy-commander-hits']").TextContent.Should().Contain("Graveyard Enabler");
        cut.Find("[data-testid='synergy-staple-overload']").TextContent.Should().Contain("Rhystic Study");
    }

    private static DeckAnalysisResponseContract CreateAnalysis(int bracketLevel, decimal synergyScore) => new()
    {
        Bracket = CreateBracket(bracketLevel, bracketLevel * 2m, $"Bracket {bracketLevel} deck."),
        PowerLevel = new PowerLevelAssessmentContract
        {
            Score = 6.7m,
            Summary = "avg MV 2.4; 1 fast mana card, 0 efficient tutors, 0 free interaction pieces, and 0 included combos.",
        },
        Synergy = new SynergyAssessmentContract
        {
            Score = synergyScore,
            Summary = "Synergy summary.",
            CommanderSpecificHits = Array.Empty<string>(),
            StapleOverloadIndicators = Array.Empty<string>(),
        },
    };

    private static BracketAssessmentContract CreateBracket(int level, decimal totalWeight, string summary) => new()
    {
        Level = level,
        TotalWeight = totalWeight,
        Summary = summary,
        Factors = Array.Empty<BracketFactorContract>(),
    };
}
