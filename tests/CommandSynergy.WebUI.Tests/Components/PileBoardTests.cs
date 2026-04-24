using Bunit;
using CommandSynergy.Application.Contracts;
using CommandSynergy.Components.Decks;
using FluentAssertions;
using MudBlazor.Services;

namespace CommandSynergy.WebUI.Tests.Components;

public sealed class PileBoardTests : BunitContext
{
    public PileBoardTests()
    {
        Services.AddMudServices();
    }

    [Fact]
    public void Pile_board_renders_only_commander_and_mainboard_lanes()
    {
        var cut = Render<PileBoard>(parameters => parameters
            .Add(component => component.Piles, CreatePiles())
            .Add(component => component.Cards, [CreateCard("sol-ring", DeckWorkspaceViewModel.MainboardPileId)]));

        cut.FindAll("[data-testid^='pile-lane-']").Should().HaveCount(2);
        cut.Markup.Should().NotContain("Interaction");
        cut.FindAll("button[data-testid^='move-']").Should().BeEmpty();
        cut.Find("[data-testid='pile-card-sol-ring']").HasAttribute("draggable").Should().BeFalse();
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

    [Fact]
    public void Pile_board_groups_and_sorts_mainboard_cards_using_selected_controls()
    {
        var cut = Render<PileBoard>(parameters => parameters
            .Add(component => component.Piles, CreatePiles())
            .Add(component => component.Cards,
            [
                CreateCard("arcane-signet", DeckWorkspaceViewModel.MainboardPileId, manaCost: "{2}", manaValue: 2m),
                CreateCard("lightning-bolt", DeckWorkspaceViewModel.MainboardPileId, name: "Lightning Bolt", manaCost: "{R}", manaValue: 1m, typeLine: "Instant", colors: ["R"]),
                CreateCard("island", DeckWorkspaceViewModel.MainboardPileId, name: "Island", manaCost: null, manaValue: 0m, typeLine: "Basic Land — Island"),
            ]));

        cut.Markup.Should().Contain("Artifacts");
        cut.Markup.Should().Contain("Instants");
        cut.Markup.Should().Contain("Lands");

        cut.Find("[data-testid='mainboard-grouping']").Change("None");
        cut.Find("[data-testid='mainboard-sorting']").Change("ManaCost");

        cut.FindAll(".pile-board__group-header h4")
            .Select(element => element.TextContent.Trim())
            .Should()
            .ContainInOrder("Mana value 0", "Mana value 1", "Mana value 2");

        cut.FindAll("[data-testid^='pile-card-']")
            .Select(element => element.GetAttribute("data-testid"))
            .Should()
            .ContainInOrder("pile-card-island", "pile-card-lightning-bolt", "pile-card-arcane-signet");
    }

    [Fact]
    public void Pile_board_uses_requested_type_group_order_for_mainboard_sections()
    {
        var cut = Render<PileBoard>(parameters => parameters
            .Add(component => component.Piles, CreatePiles())
            .Add(component => component.Cards,
            [
                CreateCard("angel", DeckWorkspaceViewModel.MainboardPileId, name: "Angel", typeLine: "Creature — Angel"),
                CreateCard("walker", DeckWorkspaceViewModel.MainboardPileId, name: "Walker", typeLine: "Planeswalker"),
                CreateCard("wrath", DeckWorkspaceViewModel.MainboardPileId, name: "Wrath", typeLine: "Sorcery"),
                CreateCard("bolt", DeckWorkspaceViewModel.MainboardPileId, name: "Bolt", typeLine: "Instant"),
                CreateCard("ring", DeckWorkspaceViewModel.MainboardPileId, name: "Ring", typeLine: "Artifact"),
                CreateCard("rhystic-study", DeckWorkspaceViewModel.MainboardPileId, name: "Rhystic Study", typeLine: "Enchantment"),
                CreateCard("plains", DeckWorkspaceViewModel.MainboardPileId, name: "Plains", manaCost: null, manaValue: 0m, typeLine: "Basic Land — Plains"),
            ]));

        cut.FindAll(".pile-board__group-header h4")
            .Select(element => element.TextContent.Trim())
            .Should()
            .ContainInOrder("Creatures", "Planeswalkers", "Sorceries", "Instants", "Artifacts", "Enchantments", "Lands");
    }

    [Fact]
    public void Pile_board_supports_compact_mode_and_multiple_copy_quantity_actions()
    {
        string? incrementedCardId = null;
        string? decrementedCardId = null;

        var cut = Render<PileBoard>(parameters => parameters
            .Add(component => component.Piles, CreatePiles())
            .Add(component => component.Cards,
            [
                CreateCard("shadowborn-apostle", DeckWorkspaceViewModel.MainboardPileId, name: "Shadowborn Apostle", manaCost: "{B}", manaValue: 1m, typeLine: "Creature", colors: ["B"], quantity: 3, allowsMultipleCopies: true),
                CreateCard("sol-ring", DeckWorkspaceViewModel.MainboardPileId),
            ])
            .Add(component => component.IncrementRequested, (string cardId) => incrementedCardId = cardId)
            .Add(component => component.DecrementRequested, (string cardId) => decrementedCardId = cardId));

        cut.Find("[data-testid='compact-mode-toggle']").Click();
        cut.Find("[data-testid='pile-card-shadowborn-apostle']").ClassList.Should().Contain("pile-board__card--compact");
        cut.Find("[data-testid='commander-card-shadowborn-apostle']").ClassList.Should().Contain("commander-card--compact");
        cut.Find(".pile-board__group-cards").ClassList.Should().Contain("pile-board__group-cards--compact");
        cut.Find("[data-testid='quantity-shadowborn-apostle']").ParentElement!.ClassList.Should().Contain("pile-board__actions");
        cut.Find("[data-testid='compact-mode-toggle']").TextContent.Should().Contain("Expand cards");
        cut.Find("[data-testid='quantity-shadowborn-apostle']").TextContent.Should().Contain("3 copies");
        cut.FindAll("[data-testid='increment-sol-ring']").Should().BeEmpty();

        cut.Find("[data-testid='increment-shadowborn-apostle']").Click();
        cut.Find("[data-testid='decrement-shadowborn-apostle']").Click();

        incrementedCardId.Should().Be("shadowborn-apostle");
        decrementedCardId.Should().Be("shadowborn-apostle");
    }

    [Fact]
    public void Pile_board_groups_mainboard_cards_by_color_rarity_and_set()
    {
        var cut = Render<PileBoard>(parameters => parameters
            .Add(component => component.Piles, CreatePiles())
            .Add(component => component.Cards,
            [
                CreateCard("sol-ring", DeckWorkspaceViewModel.MainboardPileId, name: "Sol Ring", colors: [], sourceTag: "rare", sourceSetCode: "cmm"),
                CreateCard("swords-to-plowshares", DeckWorkspaceViewModel.MainboardPileId, name: "Swords to Plowshares", colors: ["W"], sourceTag: "common", sourceSetCode: "2xm"),
                CreateCard("counterspell", DeckWorkspaceViewModel.MainboardPileId, name: "Counterspell", colors: ["U"], sourceTag: "mythic rare", sourceSetCode: null),
                CreateCard("anguished-unmaking", DeckWorkspaceViewModel.MainboardPileId, name: "Anguished Unmaking", colors: ["W", "B"], sourceTag: null, sourceSetCode: "soi"),
            ]));

        cut.Find("[data-testid='mainboard-grouping']").Change("Color");
        cut.FindAll(".pile-board__group-header h4")
            .Select(element => element.TextContent.Trim())
            .Should()
            .ContainInOrder("Colorless", "White", "Blue", "Multicolor — White / Black");

        cut.Find("[data-testid='mainboard-grouping']").Change("Rarity");
        cut.FindAll(".pile-board__group-header h4")
            .Select(element => element.TextContent.Trim())
            .Should()
            .ContainInOrder("Common", "Rare", "Mythic", "Unknown rarity");

        cut.Find("[data-testid='mainboard-grouping']").Change("Set");
        cut.FindAll(".pile-board__group-header h4")
            .Select(element => element.TextContent.Trim())
            .Should()
            .ContainInOrder("Set 2XM", "Set CMM", "Set SOI", "Unknown set");
    }

    [Fact]
    public void Pile_board_derives_jump_navigation_sections_when_mainboard_is_ungrouped()
    {
        var cut = Render<PileBoard>(parameters => parameters
            .Add(component => component.Piles, CreatePiles())
            .Add(component => component.Cards,
            [
                CreateCard("arcane-signet", DeckWorkspaceViewModel.MainboardPileId, name: "Arcane Signet"),
                CreateCard("brainstorm", DeckWorkspaceViewModel.MainboardPileId, name: "Brainstorm", typeLine: "Instant", colors: ["U"]),
                CreateCard("command-tower", DeckWorkspaceViewModel.MainboardPileId, name: "Command Tower", manaCost: null, manaValue: 0m, typeLine: "Land"),
            ]));

        cut.Find("[data-testid='mainboard-grouping']").Change("None");
        cut.Find("[data-testid='mainboard-sorting']").Change("Name");

        cut.FindAll(".pile-board__group-header h4")
            .Select(element => element.TextContent.Trim())
            .Should()
            .ContainInOrder("A", "B", "C");

        cut.Find("[data-testid='mainboard-group-link-name-a']").GetAttribute("href")
            .Should()
            .Be("#mainboard-section-name-a");

        var currentGroupJumpMenu = cut.Find("[data-testid='mainboard-group-jump-name-c']");
        currentGroupJumpMenu.Should().NotBeNull();
        var currentGroupLink = currentGroupJumpMenu.QuerySelector("[data-testid='mainboard-group-link-name-c']");
        currentGroupLink.Should().NotBeNull();
        currentGroupLink!.GetAttribute("aria-current")
            .Should()
            .Be("location");
    }

    private static IReadOnlyList<PileDefinitionContract> CreatePiles() =>
    [
        new PileDefinitionContract { PileId = DeckWorkspaceViewModel.CommandZonePileId, Name = "Command Zone", SortOrder = 0 },
        new PileDefinitionContract { PileId = DeckWorkspaceViewModel.MainboardPileId, Name = "Mainboard", SortOrder = 1 },
        new PileDefinitionContract { PileId = "interaction", Name = "Interaction", SortOrder = 2 },
    ];

    private static WorkspaceCardView CreateCard(
        string cardId,
        string pileId,
        string? name = null,
        string? manaCost = "{1}",
        decimal manaValue = 1m,
        string? typeLine = "Artifact",
        IReadOnlyList<string>? colors = null,
        bool isCommanderEligible = false,
        int quantity = 1,
        bool allowsMultipleCopies = false,
        string? sourceTag = null,
        string? sourceSetCode = null) => new()
    {
        CardId = cardId,
        Name = name ?? (cardId == "isshin-two-heavens-as-one" ? "Isshin, Two Heavens as One" : "Sol Ring"),
        ManaCost = manaCost,
        ManaValue = manaValue,
        TypeLine = typeLine ?? (isCommanderEligible ? "Legendary Creature" : "Artifact"),
        ColorIdentity = colors ?? (isCommanderEligible ? ["R", "W", "B"] : Array.Empty<string>()),
        Faces =
        [
            new WorkspaceCardFaceView(
                name ?? (cardId == "isshin-two-heavens-as-one" ? "Isshin, Two Heavens as One" : "Sol Ring"),
                manaCost,
                typeLine ?? (isCommanderEligible ? "Legendary Creature" : "Artifact"),
                null,
                true),
        ],
        AssignedPileId = pileId,
        Quantity = quantity,
        AllowsMultipleCopies = allowsMultipleCopies,
        SourceTag = sourceTag,
        SourceSetCode = sourceSetCode,
        CommanderEligibilityBasis = isCommanderEligible ? Domain.Cards.CommanderEligibilityBasis.LegendaryCreature : Domain.Cards.CommanderEligibilityBasis.Unknown,
    };
}
