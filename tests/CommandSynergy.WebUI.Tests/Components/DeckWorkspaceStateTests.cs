using Bunit;
using CommandSynergy.Application.Contracts;
using CommandSynergy.Application.Decks;
using CommandSynergy.Components.Decks;
using CommandSynergy.Domain.Cards;
using FluentAssertions;
using MudBlazor.Services;

namespace CommandSynergy.WebUI.Tests.Components;

public sealed class DeckWorkspaceStateTests : BunitContext
{
    public DeckWorkspaceStateTests()
    {
        Services.AddMudServices();
    }

    [Fact]
    public void Deck_workspace_renders_loading_state()
    {
        var cut = Render<DeckWorkspace>(parameters => parameters
            .Add(component => component.State, new DeckWorkspaceState(DeckWorkspaceStatus.Loading, null, null, Array.Empty<ValidationFindingContract>()))
            .Add(component => component.SearchQuery, string.Empty)
            .Add(component => component.Piles, CreatePiles())
            .Add(component => component.Cards, Array.Empty<WorkspaceCardView>())
            .Add(component => component.SearchResults, Array.Empty<WorkspaceCardView>()));

        cut.Find("[data-testid='workspace-loading']").TextContent.Should().Contain("Loading workspace shell");
    }

    [Fact]
    public void Deck_workspace_renders_empty_state_message()
    {
        var cut = Render<DeckWorkspace>(parameters => parameters
            .Add(component => component.State, new DeckWorkspaceState(DeckWorkspaceStatus.Empty, "Pick a commander.", null, Array.Empty<ValidationFindingContract>()))
            .Add(component => component.SearchQuery, string.Empty)
            .Add(component => component.Piles, CreatePiles())
            .Add(component => component.Cards, Array.Empty<WorkspaceCardView>())
            .Add(component => component.SearchResults, Array.Empty<WorkspaceCardView>()));

        cut.Find("[data-testid='workspace-empty']").TextContent.Should().Contain("Pick a commander.");
    }

    [Fact]
    public void Deck_workspace_renders_recovery_state_with_retry()
    {
        var cut = Render<DeckWorkspace>(parameters => parameters
            .Add(component => component.State, new DeckWorkspaceState(DeckWorkspaceStatus.Recovery, "Retry the sync.", null, Array.Empty<ValidationFindingContract>()))
            .Add(component => component.SearchQuery, string.Empty)
            .Add(component => component.Piles, CreatePiles())
            .Add(component => component.Cards, Array.Empty<WorkspaceCardView>())
            .Add(component => component.SearchResults, Array.Empty<WorkspaceCardView>()));

        cut.Find("[data-testid='workspace-recovery']").TextContent.Should().Contain("Retry the sync.");
        cut.Markup.Should().Contain("Retry sync");
    }

    [Fact]
    public void Deck_workspace_disables_commander_selection_for_ineligible_search_results()
    {
        var cut = Render<DeckWorkspace>(parameters => parameters
            .Add(component => component.State, new DeckWorkspaceState(DeckWorkspaceStatus.Empty, "Pick a commander.", null, Array.Empty<ValidationFindingContract>()))
            .Add(component => component.SearchQuery, "lightning")
            .Add(component => component.Piles, CreatePiles())
            .Add(component => component.Cards, Array.Empty<WorkspaceCardView>())
            .Add(component => component.SearchResults, [CreateSearchResult("lightning-bolt", CommanderEligibilityBasis.Unknown)]));

        cut.Find("[data-testid='set-commander-lightning-bolt']").HasAttribute("disabled").Should().BeTrue();
    }

    private static IReadOnlyList<PileDefinitionContract> CreatePiles() =>
    [
        new PileDefinitionContract { PileId = DeckWorkspaceViewModel.CommandZonePileId, Name = "Command Zone", SortOrder = 0 },
        new PileDefinitionContract { PileId = DeckWorkspaceViewModel.MainboardPileId, Name = "Mainboard", SortOrder = 1 },
    ];

    private static WorkspaceCardView CreateSearchResult(string cardId, CommanderEligibilityBasis commanderEligibilityBasis) => new()
    {
        CardId = cardId,
        Name = "Lightning Bolt",
        ManaCost = "{R}",
        TypeLine = "Instant",
        ColorIdentity = ["R"],
        Faces = [new WorkspaceCardFaceView("Lightning Bolt", "{R}", "Instant", null, true)],
        Quantity = 1,
        CommanderEligibilityBasis = commanderEligibilityBasis,
    };
}