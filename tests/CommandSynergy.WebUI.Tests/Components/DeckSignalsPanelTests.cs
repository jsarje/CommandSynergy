using Bunit;
using CommandSynergy.Application.Contracts;
using CommandSynergy.Application.Decks;
using CommandSynergy.Components.Decks;
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

        cut.Find("[data-testid='workspace-loading']").TextContent.Should().Contain("Loading workspace shell");
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
    public void Deck_signals_panel_renders_ready_metrics_and_findings()
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

        cut.Find("[data-testid='workspace-ready']").Should().NotBeNull();
        cut.Find("[data-testid='validation-findings']").TextContent.Should().Contain("Average mana value is climbing above the target.");
        cut.Find("[data-testid='analysis-panel']").TextContent.Should().Contain("Bracket");
        cut.Find("[data-testid='analysis-panel']").TextContent.Should().Contain("Synergy");
        cut.Find("[data-testid='analysis-panel']").TextContent.Should().Contain("Power level");
        cut.Markup.Should().Contain("Needs work");
        cut.Markup.Should().Contain("72.4");
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
    };
}