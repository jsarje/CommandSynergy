using Bunit;
using CommandSynergy.Application.Contracts;
using CommandSynergy.Components.Decks;
using FluentAssertions;
using MudBlazor.Services;

namespace CommandSynergy.WebUI.Tests.Components;

/// <summary>
/// Validates the loading, empty, error, and ready states of <see cref="SynergyAnalysisPanel"/>.
/// </summary>
public sealed class SynergyAnalysisPanelTests : BunitContext
{
    public SynergyAnalysisPanelTests()
    {
        Services.AddMudServices();
    }

    [Fact]
    public void Synergy_analysis_panel_renders_loading_state_when_is_loading_is_true()
    {
        var cut = Render<SynergyAnalysisPanel>(parameters => parameters
            .Add(component => component.IsLoading, true)
            .Add(component => component.Analysis, null)
            .Add(component => component.HasError, false));

        cut.Find("[data-testid='synergy-analysis-loading']").Should().NotBeNull();
        cut.Markup.Should().Contain("Measuring deck synergy");
    }

    [Fact]
    public void Synergy_analysis_panel_renders_empty_state_when_analysis_is_null_and_not_loading()
    {
        var cut = Render<SynergyAnalysisPanel>(parameters => parameters
            .Add(component => component.IsLoading, false)
            .Add(component => component.Analysis, null)
            .Add(component => component.HasError, false));

        cut.Find("[data-testid='synergy-analysis-empty']").Should().NotBeNull();
        cut.Markup.Should().Contain("Synergy feedback arrives after the first successful sync.");
    }

    [Fact]
    public void Synergy_analysis_panel_renders_error_state_when_has_error_is_true()
    {
        var cut = Render<SynergyAnalysisPanel>(parameters => parameters
            .Add(component => component.IsLoading, false)
            .Add(component => component.Analysis, null)
            .Add(component => component.HasError, true));

        cut.Find("[data-testid='synergy-analysis-error']").Should().NotBeNull();
        cut.Markup.Should().Contain("temporarily unavailable");
    }

    [Fact]
    public void Synergy_analysis_panel_renders_score_hits_and_staples_in_ready_state()
    {
        var analysis = CreateAnalysis() with
        {
            Synergy = new SynergyAssessmentContract
            {
                Score = 80m,
                Summary = "Strong commander alignment.",
                CommanderSpecificHits = ["Graveyard Enabler", "Sacrifice Outlet"],
                StapleOverloadIndicators = ["Rhystic Study"],
            },
        };

        var cut = Render<SynergyAnalysisPanel>(parameters => parameters
            .Add(component => component.IsLoading, false)
            .Add(component => component.HasError, false)
            .Add(component => component.Analysis, analysis));

        cut.Find("[data-testid='analysis-synergy']").TextContent.Should().Contain("80");
        cut.Find("[data-testid='synergy-commander-hits']").TextContent.Should().Contain("Graveyard Enabler");
        cut.Find("[data-testid='synergy-staple-overload']").TextContent.Should().Contain("Rhystic Study");
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