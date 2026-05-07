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
        cut.Markup.Should().Contain("Reading synergy");
    }

    [Fact]
    public void Synergy_analysis_panel_renders_empty_state_when_analysis_is_null_and_not_loading()
    {
        var cut = Render<SynergyAnalysisPanel>(parameters => parameters
            .Add(component => component.IsLoading, false)
            .Add(component => component.Analysis, null)
            .Add(component => component.HasError, false));

        cut.Find("[data-testid='synergy-analysis-empty']").Should().NotBeNull();
        cut.Markup.Should().Contain("Synergy appears after the first sync.");
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
                FinalScore = 82m,
                QualitativeLabel = "Tuned",
                Label = "Tuned",
                Summary = "Strong commander alignment.",
                CommanderSpecificHits = ["Graveyard Enabler", "Sacrifice Outlet"],
                StapleOverloadIndicators = ["Rhystic Study"],
                SupportingSections =
                [
                    new AnalysisSummarySectionContract
                    {
                        Title = "Breakdown",
                        Items =
                        [
                            new AnalysisSummaryItemContract
                            {
                                Label = "Final read",
                                Value = "82",
                                Description = "Final read after all synergy inputs.",
                                Tone = "positive",
                            },
                        ],
                    },
                    new AnalysisSummarySectionContract
                    {
                        Title = "Commander hits",
                        Items =
                        [
                            new AnalysisSummaryItemContract
                            {
                                Label = "Graveyard Enabler",
                                Value = "Supports the plan",
                                Description = "Commander-aligned piece.",
                                Tone = "positive",
                            },
                        ],
                    },
                    new AnalysisSummarySectionContract
                    {
                        Title = "Cautions",
                        Items =
                        [
                            new AnalysisSummaryItemContract
                            {
                                Label = "Rhystic Study",
                                Value = "Generic pressure",
                                Description = "Broad staple pressure.",
                                Tone = "warning",
                            },
                        ],
                    },
                ],
            },
        };

        var cut = Render<SynergyAnalysisPanel>(parameters => parameters
            .Add(component => component.IsLoading, false)
            .Add(component => component.HasError, false)
            .Add(component => component.Analysis, analysis));

        cut.Find("[data-testid='synergy-analysis-pill']").TextContent.Should().Contain("Tuned");
        cut.Find("[data-testid='analysis-synergy']").TextContent.Should().Contain("82");
        cut.Find("[data-testid='synergy-commander-hits']").TextContent.Should().Contain("Graveyard Enabler");
        cut.Find("[data-testid='synergy-staple-overload']").TextContent.Should().Contain("Rhystic Study");
    }

    [Fact]
    public void Synergy_analysis_panel_preserves_stale_results_while_refreshing()
    {
        var cut = Render<SynergyAnalysisPanel>(parameters => parameters
            .Add(component => component.IsLoading, true)
            .Add(component => component.HasError, false)
            .Add(component => component.Analysis, CreateAnalysis()));

        cut.Find("[data-testid='analysis-synergy']").HasAttribute("aria-busy").Should().BeTrue();
        cut.Find("[data-testid='synergy-analysis-refreshing']").TextContent.Should().Contain("Showing the previous result");
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
            Label = "Focused",
            Summary = "Power summary.",
        },
        Synergy = new SynergyAssessmentContract
        {
            Score = 74.2m,
            FinalScore = 74.2m,
            QualitativeLabel = "Focused",
            Label = "Focused",
            Summary = "Synergy summary.",
            CommanderSpecificHits = Array.Empty<string>(),
            StapleOverloadIndicators = Array.Empty<string>(),
            SupportingSections =
            [
                new AnalysisSummarySectionContract
                {
                    Title = "Breakdown",
                    Items =
                    [
                        new AnalysisSummaryItemContract
                        {
                            Label = "Final read",
                            Value = "74.2",
                            Description = "Composite read.",
                        },
                    ],
                },
                new AnalysisSummarySectionContract
                {
                    Title = "Commander hits",
                    Items =
                    [
                        new AnalysisSummaryItemContract
                        {
                            Label = "Commander hits",
                            Value = "None surfaced",
                            Description = "No standout glue pieces yet.",
                        },
                    ],
                },
                new AnalysisSummarySectionContract
                {
                    Title = "Cautions",
                    Items =
                    [
                        new AnalysisSummaryItemContract
                        {
                            Label = "Staple drag",
                            Value = "In check",
                            Description = "No obvious tension points.",
                        },
                    ],
                },
            ],
        },
    };
}
