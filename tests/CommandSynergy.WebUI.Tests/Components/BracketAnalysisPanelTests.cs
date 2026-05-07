using Bunit;
using CommandSynergy.Application.Contracts;
using CommandSynergy.Components.Decks;
using FluentAssertions;
using MudBlazor.Services;

namespace CommandSynergy.WebUI.Tests.Components;

/// <summary>
/// Validates the loading, empty, error, and ready states of <see cref="BracketAnalysisPanel"/>.
/// </summary>
public sealed class BracketAnalysisPanelTests : BunitContext
{
    public BracketAnalysisPanelTests()
    {
        Services.AddMudServices();
    }

    [Fact]
    public void Bracket_analysis_panel_renders_loading_state_when_is_loading_is_true()
    {
        var cut = Render<BracketAnalysisPanel>(parameters => parameters
            .Add(component => component.IsLoading, true)
            .Add(component => component.Analysis, null)
            .Add(component => component.HasError, false));

        cut.Find("[data-testid='bracket-analysis-loading']").Should().NotBeNull();
        cut.Markup.Should().Contain("Reading bracket");
    }

    [Fact]
    public void Bracket_analysis_panel_renders_empty_state_when_analysis_is_null_and_not_loading()
    {
        var cut = Render<BracketAnalysisPanel>(parameters => parameters
            .Add(component => component.IsLoading, false)
            .Add(component => component.Analysis, null)
            .Add(component => component.HasError, false));

        cut.Find("[data-testid='bracket-analysis-empty']").Should().NotBeNull();
        cut.Markup.Should().Contain("Bracket appears after the first sync.");
    }

    [Fact]
    public void Bracket_analysis_panel_renders_error_state_when_has_error_is_true()
    {
        var cut = Render<BracketAnalysisPanel>(parameters => parameters
            .Add(component => component.IsLoading, false)
            .Add(component => component.Analysis, null)
            .Add(component => component.HasError, true));

        cut.Find("[data-testid='bracket-analysis-error']").Should().NotBeNull();
        cut.Markup.Should().Contain("temporarily unavailable");
    }

    [Fact]
    public void Bracket_analysis_panel_renders_bracket_level_and_factors_in_ready_state()
    {
        var analysis = CreateAnalysis() with
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

        var cut = Render<BracketAnalysisPanel>(parameters => parameters
            .Add(component => component.IsLoading, false)
            .Add(component => component.HasError, false)
            .Add(component => component.Analysis, analysis));

        cut.Find("[data-testid='bracket-analysis-pill']").TextContent.Should().Contain("Bracket 4");
        cut.Find("[data-testid='bracket-analysis-overview']").TextContent.Should().Contain("Total weight");
        cut.Find("[data-testid='bracket-factors']").TextContent.Should().Contain("Mana Crypt is a configured game changer.");
    }

    [Fact]
    public void Bracket_analysis_panel_preserves_stale_results_while_refreshing()
    {
        var cut = Render<BracketAnalysisPanel>(parameters => parameters
            .Add(component => component.IsLoading, true)
            .Add(component => component.HasError, false)
            .Add(component => component.Analysis, CreateAnalysis()));

        cut.Find("[data-testid='analysis-bracket']").HasAttribute("aria-busy").Should().BeTrue();
        cut.Find("[data-testid='bracket-analysis-refreshing']").TextContent.Should().Contain("Showing the previous result");
    }

    private static DeckAnalysisResponseContract CreateAnalysis() => new()
    {
        Bracket = CreateBracket(3, 6m, "Bracket summary."),
        PowerLevel = new PowerLevelAssessmentContract
        {
            Score = 6.7m,
            Label = "Focused",
            Summary = "Power summary.",
        },
        Synergy = new SynergyAssessmentContract
        {
            Score = 74.2m,
            Label = "Focused",
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
