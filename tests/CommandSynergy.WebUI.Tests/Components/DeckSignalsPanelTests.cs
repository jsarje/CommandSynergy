using Bunit;
using CommandSynergy.Application.Contracts;
using CommandSynergy.Application.Decks;
using CommandSynergy.Components.Decks;
using CommandSynergy.WebUI.Tests;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using MudBlazor.Services;

namespace CommandSynergy.WebUI.Tests.Components;

/// <summary>
/// Validates the extracted signals panel states without going through the full workspace shell.
/// </summary>
public sealed class DeckSignalsPanelTests : BunitContext
{
    public DeckSignalsPanelTests()
    {
        Services.AddMudServices();
    }

    [Fact]
    public void Deck_signals_panel_renders_loading_state()
    {
        var cut = Render<DeckSignalsPanel>(parameters => parameters
            .Add(component => component.State, new DeckWorkspaceState(DeckWorkspaceStatus.Loading, null, null, Array.Empty<ValidationFindingContract>()))
            .Add(component => component.Analysis, null)
            .Add(component => component.IsRefreshingInsights, false));

        cut.Find("[data-testid='workspace-loading']").TextContent.Should().Contain("Loading workspace");
    }

    [Fact]
    public void Deck_signals_panel_invokes_retry_callback_in_recovery_state()
    {
        var wasInvoked = false;

        var cut = Render<DeckSignalsPanel>(parameters => parameters
            .Add(component => component.State, new DeckWorkspaceState(DeckWorkspaceStatus.Recovery, "Retry the sync.", null, Array.Empty<ValidationFindingContract>()))
            .Add(component => component.Analysis, null)
            .Add(component => component.IsRefreshingInsights, false)
            .Add(component => component.RetryRequested, EventCallback.Factory.Create(this, () => wasInvoked = true)));

        cut.Find(".workspace-state__retry").Click();

        wasInvoked.Should().BeTrue();
    }

    [Fact]
    public void Deck_signals_panel_renders_ready_metrics_with_details_collapsed_by_default()
    {
        var findings = new[]
        {
            new ValidationFindingContract
            {
                Severity = "warning",
                Code = "curve.high",
                Message = "Average mana value is climbing above the target.",
            },
        };

        var cut = Render<DeckSignalsPanel>(parameters => parameters
            .Add(component => component.State, new DeckWorkspaceState(
                DeckWorkspaceStatus.Ready,
                null,
                new DeckValidationResponseContract
                {
                    IsValid = false,
                    Findings = findings,
                    DeckCardCount = 100,
                },
                findings))
            .Add(component => component.Analysis, CreateAnalysis())
            .Add(component => component.CommanderName, "Isshin, Two Heavens as One")
            .Add(component => component.TotalDeckCards, 100)
            .Add(component => component.IsRefreshingInsights, false));

        cut.Find("[data-testid='workspace-ready']").Should().NotBeNull();
        cut.Find("[data-testid='workspace-briefing']").TextContent.Should().Contain("Deck needs validation work");
        cut.Find("[data-testid='workspace-briefing']").TextContent.Should().Contain("Isshin, Two Heavens as One");
        cut.Find("[data-testid='workspace-briefing']").TextContent.Should().Contain("Fix findings");
        cut.Find("[data-testid='analysis-detail-hint']").TextContent.Should().Contain("Click a metric");
        cut.FindAll("[data-testid='validation-findings']").Should().BeEmpty();
        cut.Find("[data-testid='metric-validation-trigger']").GetAttribute("aria-expanded").Should().Be("false");
        cut.Find("[data-testid='metric-bracket-trigger']").GetAttribute("aria-expanded").Should().Be("false");
        cut.Markup.Should().Contain("Needs work");
        cut.Markup.Should().Contain("72.4");
    }

    [Fact]
    public void Deck_signals_panel_reveals_requested_detail_when_metric_is_clicked()
    {
        var findings = new[]
        {
            new ValidationFindingContract
            {
                Severity = "warning",
                Code = "curve.high",
                Message = "Average mana value is climbing above the target.",
            },
        };

        var cut = Render<DeckSignalsPanel>(parameters => parameters
            .Add(component => component.State, new DeckWorkspaceState(
                DeckWorkspaceStatus.Ready,
                null,
                new DeckValidationResponseContract
                {
                    IsValid = false,
                    Findings = findings,
                    DeckCardCount = 100,
                },
                findings))
            .Add(component => component.Analysis, CreateAnalysis())
            .Add(component => component.IsRefreshingInsights, false));

        cut.Find("[data-testid='metric-validation-trigger']").Click();

        cut.Find("[data-testid='validation-findings']").TextContent.Should().Contain("Average mana value is climbing above the target.");
        cut.Find("[data-testid='metric-validation-trigger']").GetAttribute("aria-expanded").Should().Be("true");
        cut.FindAll("[data-testid='analysis-detail-hint']").Should().BeEmpty();

        cut.Find("[data-testid='metric-bracket-trigger']").Click();

        cut.Find("[data-testid='analysis-bracket']").TextContent.Should().Contain("Bracket summary.");
        cut.Find("[data-testid='metric-validation-trigger']").GetAttribute("aria-expanded").Should().Be("false");
        cut.Find("[data-testid='metric-bracket-trigger']").GetAttribute("aria-expanded").Should().Be("true");
        cut.FindAll("[data-testid='validation-findings']").Should().BeEmpty();
    }

    [Fact]
    public void Deck_signals_panel_prioritizes_library_restore_state_over_analysis_refresh()
    {
        var cut = Render<DeckSignalsPanel>(parameters => parameters
            .Add(component => component.State, new DeckWorkspaceState(
                DeckWorkspaceStatus.Ready,
                null,
                new DeckValidationResponseContract
                {
                    IsValid = true,
                    DeckCardCount = 100,
                    Findings = Array.Empty<ValidationFindingContract>(),
                },
                Array.Empty<ValidationFindingContract>()))
            .Add(component => component.Analysis, CreateAnalysis())
            .Add(component => component.IsRefreshingInsights, false)
            .Add(component => component.IsHydratingLibrary, true)
            .Add(component => component.IsAutoOpeningDeck, true));

        cut.Find("[data-testid='library-hydrate-indicator']").TextContent.Should().Contain("Restoring library");
        cut.Find("[data-testid='library-hydrate-banner']").TextContent.Should().Contain("Restoring saved deck");
        cut.Find("[data-testid='workspace-briefing']").TextContent.Should().Contain("Restoring saved deck");
        cut.FindAll("[data-testid='analysis-refresh-banner']").Should().BeEmpty();
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
            Score = 6.8m,
            Summary = "Power summary.",
        },
        Synergy = new SynergyAssessmentContract
        {
            Score = 72.4m,
            Summary = "Synergy summary.",
            CommanderSpecificHits = Array.Empty<string>(),
            StapleOverloadIndicators = Array.Empty<string>(),
        },
        ThemeAnalysis = ThemeAnalysisTestData.CreateReadyAnalysis().ThemeAnalysis,
    };
}
