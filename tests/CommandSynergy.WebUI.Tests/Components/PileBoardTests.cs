using Bunit;
using CommandSynergy.Application.Contracts;
using CommandSynergy.Components.Decks;
using FluentAssertions;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor.Services;

namespace CommandSynergy.WebUI.Tests.Components;

public sealed class PileBoardTests : BunitContext
{
    public PileBoardTests()
    {
        Services.AddMudServices();
    }

    [Fact]
    public void Pile_board_emits_move_request_when_card_is_dropped_on_new_pile()
    {
        MoveCardRequest? capturedRequest = null;

        var cut = Render<PileBoard>(parameters => parameters
            .Add(component => component.Piles, CreatePiles())
            .Add(component => component.Cards, [CreateCard("sol-ring", DeckWorkspaceViewModel.MainboardPileId)])
            .Add(component => component.MoveRequested, (MoveCardRequest request) => capturedRequest = request));

        cut.Find("[data-testid='pile-card-sol-ring']").DragStart(new DragEventArgs());
        cut.Find("[data-testid='pile-drop-interaction']").Drop(new DragEventArgs());

        capturedRequest.Should().BeEquivalentTo(new MoveCardRequest("sol-ring", "interaction"));
    }

    [Fact]
    public void Pile_board_emits_set_commander_request_for_eligible_non_commander_card()
    {
        string? capturedCardId = null;

        var cut = Render<PileBoard>(parameters => parameters
            .Add(component => component.Piles, CreatePiles())
            .Add(component => component.Cards, [CreateCard("isshin-two-heavens-as-one", DeckWorkspaceViewModel.MainboardPileId, isCommanderEligible: true)])
            .Add(component => component.CommanderSelectionEnabled, true)
            .Add(component => component.SetCommanderRequested, (string cardId) => capturedCardId = cardId));

        cut.Find("[data-testid='set-commander-from-pile-isshin-two-heavens-as-one']").Click();

        capturedCardId.Should().Be("isshin-two-heavens-as-one");
    }

    private static IReadOnlyList<PileDefinitionContract> CreatePiles() =>
    [
        new PileDefinitionContract { PileId = DeckWorkspaceViewModel.CommandZonePileId, Name = "Command Zone", SortOrder = 0 },
        new PileDefinitionContract { PileId = DeckWorkspaceViewModel.MainboardPileId, Name = "Mainboard", SortOrder = 1 },
        new PileDefinitionContract { PileId = "interaction", Name = "Interaction", SortOrder = 2 },
    ];

    private static WorkspaceCardView CreateCard(string cardId, string pileId, bool isCommanderEligible = false) => new()
    {
        CardId = cardId,
        Name = cardId == "isshin-two-heavens-as-one" ? "Isshin, Two Heavens as One" : "Sol Ring",
        ManaCost = cardId == "isshin-two-heavens-as-one" ? "{R}{W}{B}" : "{1}",
        TypeLine = isCommanderEligible ? "Legendary Creature" : "Artifact",
        ColorIdentity = isCommanderEligible ? ["R", "W", "B"] : Array.Empty<string>(),
        Faces = [new WorkspaceCardFaceView(cardId == "isshin-two-heavens-as-one" ? "Isshin, Two Heavens as One" : "Sol Ring", cardId == "isshin-two-heavens-as-one" ? "{R}{W}{B}" : "{1}", isCommanderEligible ? "Legendary Creature" : "Artifact", null, true)],
        AssignedPileId = pileId,
        Quantity = 1,
        CommanderEligibilityBasis = isCommanderEligible ? Domain.Cards.CommanderEligibilityBasis.LegendaryCreature : Domain.Cards.CommanderEligibilityBasis.Unknown,
    };
}