using Bunit;
using CommandSynergy.Components.Decks;
using FluentAssertions;

namespace CommandSynergy.WebUI.Tests.Components;

public sealed class DeckPlaytestOverlayTests : BunitContext
{
    [Fact]
    public void Playtest_overlay_draws_an_opening_hand_from_the_live_mainboard()
    {
        var cut = Render<DeckPlaytestOverlay>(parameters => parameters
            .Add(component => component.Cards,
            [
                CreateCard("commander", DeckWorkspaceViewModel.CommandZonePileId, name: "Isshin, Two Heavens as One", typeLine: "Legendary Creature", quantity: 1),
                CreateCard("sol-ring", DeckWorkspaceViewModel.MainboardPileId, name: "Sol Ring", quantity: 2),
                CreateCard("arcane-signet", DeckWorkspaceViewModel.MainboardPileId, name: "Arcane Signet", quantity: 2),
                CreateCard("swords-to-plowshares", DeckWorkspaceViewModel.MainboardPileId, name: "Swords to Plowshares"),
                CreateCard("smothering-tithe", DeckWorkspaceViewModel.MainboardPileId, name: "Smothering Tithe"),
                CreateCard("dockside-extortionist", DeckWorkspaceViewModel.MainboardPileId, name: "Dockside Extortionist"),
                CreateCard("esper-sentinel", DeckWorkspaceViewModel.MainboardPileId, name: "Esper Sentinel"),
            ]));

        cut.Find("[data-testid='playtest-hand-count']").TextContent.Should().Be("7");
        cut.Find("[data-testid='playtest-library-count']").TextContent.Should().Be("1");
        cut.FindAll("[data-testid^='playtest-hand-card-']").Should().HaveCount(7);
        cut.Markup.Should().NotContain("Isshin, Two Heavens as One");
    }

    [Fact]
    public void Playtest_overlay_requires_bottoming_cards_after_a_mulligan_before_drawing_again()
    {
        var cut = Render<DeckPlaytestOverlay>(parameters => parameters
            .Add(component => component.Cards,
            [
                CreateCard("sol-ring", DeckWorkspaceViewModel.MainboardPileId, name: "Sol Ring"),
                CreateCard("arcane-signet", DeckWorkspaceViewModel.MainboardPileId, name: "Arcane Signet"),
                CreateCard("swords-to-plowshares", DeckWorkspaceViewModel.MainboardPileId, name: "Swords to Plowshares"),
                CreateCard("smothering-tithe", DeckWorkspaceViewModel.MainboardPileId, name: "Smothering Tithe"),
                CreateCard("dockside-extortionist", DeckWorkspaceViewModel.MainboardPileId, name: "Dockside Extortionist"),
                CreateCard("esper-sentinel", DeckWorkspaceViewModel.MainboardPileId, name: "Esper Sentinel"),
                CreateCard("teferis-protection", DeckWorkspaceViewModel.MainboardPileId, name: "Teferi's Protection"),
                CreateCard("mother-of-runes", DeckWorkspaceViewModel.MainboardPileId, name: "Mother of Runes"),
            ]));

        cut.Find("[data-testid='playtest-take-mulligan']").Click();

        cut.Find("[data-testid='playtest-status']").TextContent.Should().Contain("Choose 1 more card");
        cut.Find("[data-testid='playtest-draw-card']").HasAttribute("disabled").Should().BeTrue();

        cut.FindAll("[data-testid^='put-on-bottom-']").Should().HaveCount(7);
        cut.FindAll("[data-testid^='put-on-bottom-']")[0].Click();

        cut.Find("[data-testid='playtest-hand-count']").TextContent.Should().Be("6");
        cut.Find("[data-testid='playtest-bottom-count']").TextContent.Should().Be("1");
        cut.Find("[data-testid='playtest-bottom-card-1']").Should().NotBeNull();
        cut.Find("[data-testid='playtest-draw-card']").HasAttribute("disabled").Should().BeFalse();

        cut.Find("[data-testid='playtest-draw-card']").Click();

        cut.Find("[data-testid='playtest-hand-count']").TextContent.Should().Be("7");
        cut.Find("[data-testid='playtest-library-count']").TextContent.Should().Be("1");
    }

    [Fact]
    public void Playtest_overlay_enforces_the_mulligan_limit_for_small_live_mainboards()
    {
        var cut = Render<DeckPlaytestOverlay>(parameters => parameters
            .Add(component => component.Cards,
            [
                CreateCard("sol-ring", DeckWorkspaceViewModel.MainboardPileId, name: "Sol Ring"),
            ]));

        cut.Find("[data-testid='playtest-take-mulligan']").Click();
        cut.FindAll("[data-testid^='put-on-bottom-']")[0].Click();

        cut.Find("[data-testid='playtest-mulligan-count']").TextContent.Should().Be("1");
        cut.Find("[data-testid='playtest-take-mulligan']").HasAttribute("disabled").Should().BeTrue();
        cut.Find("[data-testid='playtest-status']").TextContent.Should().Contain("Mulligan limit reached");
    }

    private static WorkspaceCardView CreateCard(
        string cardId,
        string pileId,
        string? name = null,
        string? typeLine = "Artifact",
        int quantity = 1) => new()
    {
        CardId = cardId,
        Name = name ?? "Card",
        ManaCost = "{1}",
        ManaValue = 1m,
        TypeLine = typeLine ?? "Artifact",
        ColorIdentity = Array.Empty<string>(),
        Faces =
        [
            new WorkspaceCardFaceView(name ?? "Card", "{1}", typeLine ?? "Artifact", null, true),
        ],
        AssignedPileId = pileId,
        Quantity = quantity,
    };
}
