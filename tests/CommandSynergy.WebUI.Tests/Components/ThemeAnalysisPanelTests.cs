using Bunit;
using CommandSynergy.Application.Contracts;
using CommandSynergy.Components.Decks;
using FluentAssertions;
using MudBlazor.Services;

namespace CommandSynergy.WebUI.Tests.Components;

public sealed class ThemeAnalysisPanelTests : BunitContext
{
    public ThemeAnalysisPanelTests()
    {
        Services.AddMudServices();
    }

    [Fact]
    public void Theme_analysis_panel_renders_loading_state()
    {
        var cut = Render<ThemeAnalysisPanel>(parameters => parameters
            .Add(component => component.IsLoading, true)
            .Add(component => component.HasError, false)
            .Add(component => component.Analysis, null));

        cut.Find("[data-testid='theme-analysis-loading']").Should().NotBeNull();
    }

    [Fact]
    public void Theme_analysis_panel_renders_insufficient_state()
    {
        var cut = Render<ThemeAnalysisPanel>(parameters => parameters
            .Add(component => component.IsLoading, false)
            .Add(component => component.HasError, false)
            .Add(component => component.Analysis, CreateAnalysis(isInsufficient: true)));

        cut.Find("[data-testid='theme-analysis-insufficient']").TextContent.Should().Contain("Add more cards");
    }

    [Fact]
    public void Theme_analysis_panel_renders_ready_state_with_off_theme_cards()
    {
        var cut = Render<ThemeAnalysisPanel>(parameters => parameters
            .Add(component => component.IsLoading, false)
            .Add(component => component.HasError, false)
            .Add(component => component.Analysis, CreateAnalysis(isInsufficient: false)));

        cut.Find("[data-testid='theme-analysis-ready']").Should().NotBeNull();
        cut.Find("[data-testid='off-theme-cards']").TextContent.Should().Contain("Staple Card");
    }

    private static DeckAnalysisResponseContract CreateAnalysis(bool isInsufficient) => new()
    {
        Bracket = new BracketAssessmentContract
        {
            Level = 3,
            TotalWeight = 10m,
            Summary = "Bracket summary.",
            Factors = Array.Empty<BracketFactorContract>(),
        },
        Synergy = new SynergyAssessmentContract
        {
            Score = 70m,
            ThemeScore = 68m,
            FinalScore = 70m,
            QualitativeLabel = "Focused",
            Summary = "Strong focus.",
            CommanderSpecificHits = Array.Empty<string>(),
            StapleOverloadIndicators = Array.Empty<string>(),
        },
        ThemeAnalysis = new ThemeAnalysisContract
        {
            RankedThemes =
            [
                new DeckThemeContract
                {
                    Name = "Tokens",
                    Description = "Creates a wide board.",
                    Strength = 0.72m,
                    StrengthLabel = "Strong",
                    ContributingCardIds = ["card-a"],
                    ContributingCardCount = 1,
                    Contributors =
                    [
                        new ThemeContributorContract
                        {
                            CardId = "card-a",
                            CardName = "Token Card",
                            Signal = 0.72m,
                            Reason = "Matched the card's oracle text.",
                        },
                    ],
                },
            ],
            PrimaryThemes =
            [
                new DeckThemeContract
                {
                    Name = "Tokens",
                    Description = "Creates a wide board.",
                    Strength = 0.72m,
                    StrengthLabel = "Strong",
                    ContributingCardIds = ["card-a"],
                    ContributingCardCount = 1,
                    Contributors = Array.Empty<ThemeContributorContract>(),
                },
            ],
            OffThemeCards =
            [
                new OffThemeCardContract
                {
                    CardId = "staple-card",
                    CardName = "Staple Card",
                    Reason = "No strong theme signal was detected for this card.",
                },
            ],
            CommanderAlignment = new CommanderAlignmentContract
            {
                Level = "Strong",
                CommanderTopTheme = "Tokens",
                DeckStrengthForCommanderTheme = 0.72m,
                EvidenceCardIds = ["card-a"],
                Summary = "The 99 strongly reinforce the commander's plan.",
            },
            AnalysedCardCount = 25,
            IsInsufficient = isInsufficient,
            AnalysedAtUtc = DateTimeOffset.UtcNow,
            RefreshSummary = "Primary themes: Tokens.",
        },
    };
}