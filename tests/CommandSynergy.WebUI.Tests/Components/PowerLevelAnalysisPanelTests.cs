using Bunit;
using CommandSynergy.Application.Contracts;
using CommandSynergy.Components.Decks;
using FluentAssertions;
using MudBlazor.Services;

namespace CommandSynergy.WebUI.Tests.Components;

/// <summary>
/// Validates the loading, empty, error, and ready states of <see cref="PowerLevelAnalysisPanel"/>.
/// </summary>
public sealed class PowerLevelAnalysisPanelTests : BunitContext
{
    public PowerLevelAnalysisPanelTests()
    {
        Services.AddMudServices();
    }

    [Fact]
    public void Power_level_analysis_panel_renders_loading_state_when_is_loading_is_true()
    {
        var cut = Render<PowerLevelAnalysisPanel>(parameters => parameters
            .Add(component => component.IsLoading, true)
            .Add(component => component.Analysis, null)
            .Add(component => component.HasError, false));

        cut.Find("[data-testid='power-analysis-loading']").Should().NotBeNull();
        cut.Markup.Should().Contain("Reading power");
    }

    [Fact]
    public void Power_level_analysis_panel_renders_empty_state_when_analysis_is_null_and_not_loading()
    {
        var cut = Render<PowerLevelAnalysisPanel>(parameters => parameters
            .Add(component => component.IsLoading, false)
            .Add(component => component.Analysis, null)
            .Add(component => component.HasError, false));

        cut.Find("[data-testid='power-analysis-empty']").Should().NotBeNull();
        cut.Markup.Should().Contain("Power appears after the first sync.");
    }

    [Fact]
    public void Power_level_analysis_panel_renders_error_state_when_has_error_is_true()
    {
        var cut = Render<PowerLevelAnalysisPanel>(parameters => parameters
            .Add(component => component.IsLoading, false)
            .Add(component => component.Analysis, null)
            .Add(component => component.HasError, true));

        cut.Find("[data-testid='power-analysis-error']").Should().NotBeNull();
        cut.Markup.Should().Contain("temporarily unavailable");
    }

    [Fact]
    public void Power_level_analysis_panel_renders_score_and_summary_in_ready_state()
    {
        var cut = Render<PowerLevelAnalysisPanel>(parameters => parameters
            .Add(component => component.IsLoading, false)
            .Add(component => component.HasError, false)
            .Add(component => component.Analysis, CreateAnalysis()));

        cut.Find("[data-testid='analysis-power']").TextContent.Should().Contain("6.7");
        cut.Find("[data-testid='analysis-power']").TextContent.Should().Contain("Power summary.");
    }

    private static DeckAnalysisResponseContract CreateAnalysis() => new()
    {
        Bracket = new BracketAssessmentContract
        {
            Level = 3,
            TotalWeight = 6m,
            Summary = "Bracket summary.",
            Factors = Array.Empty<BracketFactorContract>(),
        },
        PowerLevel = new PowerLevelAssessmentContract
        {
            Score = 6.7m,
            Summary = "Power summary.",
        },
        Synergy = new SynergyAssessmentContract
        {
            Score = 74.2m,
            Summary = "Synergy summary.",
            CommanderSpecificHits = Array.Empty<string>(),
            StapleOverloadIndicators = Array.Empty<string>(),
        },
    };
}
