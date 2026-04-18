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

    private static IReadOnlyList<PileDefinitionContract> CreatePiles() =>
    [
        new PileDefinitionContract { PileId = DeckWorkspaceViewModel.CommandZonePileId, Name = "Command Zone", SortOrder = 0 },
        new PileDefinitionContract { PileId = DeckWorkspaceViewModel.MainboardPileId, Name = "Mainboard", SortOrder = 1 },
        new PileDefinitionContract { PileId = "interaction", Name = "Interaction", SortOrder = 2 },
    ];

    private static WorkspaceCardView CreateCard(string cardId, string pileId) => new()
    {
        CardId = cardId,
        Name = "Sol Ring",
        ManaCost = "{1}",
        TypeLine = "Artifact",
        ColorIdentity = Array.Empty<string>(),
        Faces = [new WorkspaceCardFaceView("Sol Ring", "{1}", "Artifact", null, true)],
        AssignedPileId = pileId,
        Quantity = 1,
    };
}