using CommandSynergy.Domain.Cards;
using CommandSynergy.Domain.Decks;
using CommandSynergy.Domain.Rules;
using FluentAssertions;

namespace CommandSynergy.Domain.Tests.Rules;

public sealed class CommanderRulesTests
{
    private readonly CommanderRules sut = new();

    [Fact]
    public void Validate_accepts_a_100_card_singleton_deck_in_color_identity()
    {
        var deck = new Deck(name: "Mono Green");
        var profiles = new Dictionary<string, CardProfile>(StringComparer.OrdinalIgnoreCase);

        profiles["commander"] = CreateCard("commander", "Verdant Leader", new[] { "G" }, manaValue: 4, typeLine: "Legendary Creature");
        deck.UpsertEntry("commander", 1, isCommander: true);

        for (var index = 1; index <= 99; index++)
        {
            var cardId = $"card-{index}";
            profiles[cardId] = CreateCard(cardId, $"Card {index}", new[] { "G" }, manaValue: 2, typeLine: index <= 35 ? "Basic Land - Forest" : "Creature");
            deck.UpsertEntry(cardId, 1);
        }

        var result = sut.Validate(deck, profiles);

        result.IsValid.Should().BeTrue();
        result.Findings.Should().BeEmpty();
        result.DeckCardCount.Should().Be(100);
    }

    [Fact]
    public void Validate_flags_deck_size_violations()
    {
        var deck = new Deck();
        var profiles = new Dictionary<string, CardProfile>(StringComparer.OrdinalIgnoreCase)
        {
            ["commander"] = CreateCard("commander", "Verdant Leader", new[] { "G" }, manaValue: 4, typeLine: "Legendary Creature"),
        };

        deck.UpsertEntry("commander", 1, isCommander: true);

        var result = sut.Validate(deck, profiles);

        result.IsValid.Should().BeFalse();
        result.Findings.Should().ContainSingle(finding => finding.Code == "deck-size");
    }

    [Fact]
    public void Validate_flags_duplicate_non_basic_cards()
    {
        var deck = new Deck();
        var profiles = new Dictionary<string, CardProfile>(StringComparer.OrdinalIgnoreCase)
        {
            ["commander"] = CreateCard("commander", "Verdant Leader", new[] { "G" }, manaValue: 4, typeLine: "Legendary Creature"),
            ["sol-ring-a"] = CreateCard("sol-ring-a", "Sol Ring", Array.Empty<string>(), manaValue: 1, typeLine: "Artifact", oracleId: "sol-ring"),
            ["sol-ring-b"] = CreateCard("sol-ring-b", "Sol Ring", Array.Empty<string>(), manaValue: 1, typeLine: "Artifact", oracleId: "sol-ring"),
        };

        deck.UpsertEntry("commander", 1, isCommander: true);
        deck.UpsertEntry("sol-ring-a", 1);
        deck.UpsertEntry("sol-ring-b", 1);

        var result = sut.Validate(deck, profiles);

        result.Findings.Should().ContainSingle(finding => finding.Code == "singleton");
    }

    [Fact]
    public void Validate_flags_cards_outside_commander_color_identity()
    {
        var deck = new Deck();
        var profiles = new Dictionary<string, CardProfile>(StringComparer.OrdinalIgnoreCase)
        {
            ["commander"] = CreateCard("commander", "Verdant Leader", new[] { "G" }, manaValue: 4, typeLine: "Legendary Creature"),
            ["off-color"] = CreateCard("off-color", "Lightning Bolt", new[] { "R" }, manaValue: 1, typeLine: "Instant"),
        };

        deck.UpsertEntry("commander", 1, isCommander: true);
        deck.UpsertEntry("off-color", 99);

        var result = sut.Validate(deck, profiles);

        result.Findings.Should().ContainSingle(finding => finding.Code == "color-identity");
    }

    [Fact]
    public void Validate_enforces_even_mana_value_companion_restrictions()
    {
        var deck = new Deck();
        var profiles = new Dictionary<string, CardProfile>(StringComparer.OrdinalIgnoreCase)
        {
            ["commander"] = CreateCard("commander", "Verdant Leader", new[] { "G" }, manaValue: 4, typeLine: "Legendary Creature"),
            ["companion"] = CreateCard("companion", "Gyruda", new[] { "U", "B" }, manaValue: 6, typeLine: "Creature", companionRequirementCode: "even-mana-value"),
            ["odd-card"] = CreateCard("odd-card", "Odd Spell", new[] { "G" }, manaValue: 3, typeLine: "Sorcery"),
        };

        deck.UpsertEntry("commander", 1, isCommander: true);
        deck.UpsertEntry("companion", 1, isCompanion: true);
        deck.UpsertEntry("odd-card", 98);

        var result = sut.Validate(deck, profiles);

        result.Findings.Should().ContainSingle(finding => finding.Code == "companion-restriction");
    }

    [Fact]
    public void Validate_flags_multi_face_cards_without_alternate_face_metadata()
    {
        var deck = new Deck();
        var profiles = new Dictionary<string, CardProfile>(StringComparer.OrdinalIgnoreCase)
        {
            ["commander"] = CreateCard("commander", "Verdant Leader", new[] { "G" }, manaValue: 4, typeLine: "Legendary Creature"),
            ["mdfc"] = new CardProfile
            {
                CardId = "mdfc",
                Name = "Shatterskull Smashing // Shatterskull, the Hammer Pass",
                ManaValue = 3,
                TypeLine = "Sorcery",
                OracleText = "Front text // Back text",
                ColorIdentity = new[] { "R" },
                FaceProfiles = new[] { new CardFaceProfile("0", "Shatterskull Smashing", "{X}{R}{R}", "Sorcery", "Front text", null, false) },
            },
        };

        deck.UpsertEntry("commander", 1, isCommander: true);
        deck.UpsertEntry("mdfc", 99);

        var result = sut.Validate(deck, profiles);

        result.Findings.Should().Contain(finding => finding.Code == "multiface-primary-face" || finding.Code == "multiface-metadata");
    }

    private static CardProfile CreateCard(string cardId, string name, IReadOnlyList<string> colors, decimal manaValue, string typeLine, string? oracleId = null, string? companionRequirementCode = null) => new()
    {
        CardId = cardId,
        OracleId = oracleId ?? cardId,
        Name = name,
        ManaValue = manaValue,
        TypeLine = typeLine,
        ColorIdentity = colors,
        CompanionRequirementCode = companionRequirementCode,
        FaceProfiles = new[] { new CardFaceProfile("0", name, null, typeLine, null, null, true) },
    };
}