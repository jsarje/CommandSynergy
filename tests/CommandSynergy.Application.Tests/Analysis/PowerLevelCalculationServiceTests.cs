using CommandSynergy.Application.Analysis;
using CommandSynergy.Domain.Analysis;
using CommandSynergy.Domain.Cards;
using CommandSynergy.Domain.Decks;
using FluentAssertions;

namespace CommandSynergy.Application.Tests.Analysis;

public sealed class PowerLevelCalculationServiceTests
{
    [Fact]
    public void Calculate_applies_curve_speed_tutor_interaction_and_combo_modifiers()
    {
        var deck = CreateDeck(
            ("commander", 1, true),
            ("sol-ring", 1, false),
            ("vampiric-tutor", 1, false),
            ("fierce-guardianship", 1, false),
            ("cheap-spell", 1, false));
        var profiles = CreateProfiles(
            CreateCard("commander", "Commander", manaValue: 2m),
            CreateCard("sol-ring", "Sol Ring", manaValue: 1m),
            CreateCard("vampiric-tutor", "Vampiric Tutor", manaValue: 1m),
            CreateCard("fierce-guardianship", "Fierce Guardianship", manaValue: 3m),
            CreateCard("cheap-spell", "Cheap Spell", manaValue: 2m));
        var comboAnalysis = new ComboAnalysis(
            [ new ComboResult(["Commander", "Sol Ring"], ["Infinite mana"], "Loop the artifacts.", "Commander on board.") ],
            [],
            0,
            DateTimeOffset.UtcNow);
        var sut = new PowerLevelCalculationService();

        var result = sut.Calculate(deck, profiles, comboAnalysis);

        result.Score.Should().Be(9.2m);
        result.Summary.Should().Contain("Average ManaCost 1.8");
        result.Summary.Should().Contain("1 fast mana card");
        result.Summary.Should().Contain("1 efficient tutor");
        result.Summary.Should().Contain("1 free interaction piece");
        result.Summary.Should().Contain("1 included combo");
    }

    [Fact]
    public void Calculate_ignores_lands_when_computing_average_mana_value()
    {
        var deck = CreateDeck(
            ("expensive-spell", 1, false),
            ("forest", 20, false));
        var profiles = CreateProfiles(
            CreateCard("expensive-spell", "Expensive Spell", manaValue: 5m),
            CreateCard("forest", "Forest", manaValue: 0m, typeLine: "Basic Land — Forest"));
        var sut = new PowerLevelCalculationService();

        var result = sut.Calculate(deck, profiles, ComboAnalysis.Empty());

        result.Score.Should().Be(3.5m);
        result.Summary.Should().Contain("Average ManaCost 5");
    }

    [Fact]
    public void Calculate_clamps_scores_to_the_supported_upper_bound()
    {
        var deck = CreateDeck(
            ("mana-crypt", 1, false),
            ("sol-ring", 1, false),
            ("jeweled-lotus", 1, false),
            ("chrome-mox", 1, false),
            ("mox-opal", 1, false),
            ("vampiric-tutor", 1, false),
            ("demonic-tutor", 1, false),
            ("fierce-guardianship", 1, false),
            ("force-of-will", 1, false),
            ("cheap-spell", 8, false));
        var profiles = CreateProfiles(
            CreateCard("mana-crypt", "Mana Crypt", manaValue: 0m),
            CreateCard("sol-ring", "Sol Ring", manaValue: 1m),
            CreateCard("jeweled-lotus", "Jeweled Lotus", manaValue: 0m),
            CreateCard("chrome-mox", "Chrome Mox", manaValue: 0m),
            CreateCard("mox-opal", "Mox Opal", manaValue: 0m),
            CreateCard("vampiric-tutor", "Vampiric Tutor", manaValue: 1m),
            CreateCard("demonic-tutor", "Demonic Tutor", manaValue: 2m),
            CreateCard("fierce-guardianship", "Fierce Guardianship", manaValue: 3m),
            CreateCard("force-of-will", "Force of Will", manaValue: 5m),
            CreateCard("cheap-spell", "Cheap Spell", manaValue: 1m));
        var comboAnalysis = new ComboAnalysis(
            [
                new ComboResult(["A", "B"], ["Infinite mana"], "A", "B"),
                new ComboResult(["C", "D"], ["Infinite damage"], "C", "D"),
            ],
            [],
            0,
            DateTimeOffset.UtcNow);
        var sut = new PowerLevelCalculationService();

        var result = sut.Calculate(deck, profiles, comboAnalysis);

        result.Score.Should().Be(10.0m);
    }

    private static Deck CreateDeck(params (string CardId, int Quantity, bool IsCommander)[] entries)
    {
        var deck = new Deck("deck-1", "Test Deck");

        foreach (var entry in entries)
        {
            deck.UpsertEntry(entry.CardId, entry.Quantity, assignedPileId: null, entry.IsCommander, isCompanion: false);
        }

        return deck;
    }

    private static IReadOnlyDictionary<string, CardProfile> CreateProfiles(params CardProfile[] profiles) =>
        profiles.ToDictionary(profile => profile.CardId, StringComparer.OrdinalIgnoreCase);

    private static CardProfile CreateCard(string cardId, string name, decimal manaValue, string typeLine = "Instant") => new()
    {
        CardId = cardId,
        OracleId = $"{cardId}-oracle",
        Name = name,
        ManaValue = manaValue,
        TypeLine = typeLine,
        FaceProfiles = [ new CardFaceProfile("0", name, null, typeLine, null, null, true) ],
    };
}
