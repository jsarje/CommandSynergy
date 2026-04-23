using CommandSynergy.Application.Analysis;
using CommandSynergy.Domain.Cards;
using CommandSynergy.Domain.Decks;
using FluentAssertions;

namespace CommandSynergy.Application.Tests.Analysis;

public sealed class DeckStatsCalculationServiceTests
{
    private readonly DeckStatsCalculationService sut = new();

    [Fact]
    public void Calculate_builds_mana_value_histogram_cost_distribution_and_curve()
    {
        var deck = CreateDeck(
            ("commander", 1, true),
            ("hybrid-spell", 2, false),
            ("big-spell", 1, false),
            ("utility-land", 1, false),
            ("missing-profile", 1, false));
        var profiles = CreateProfiles(
            CreateCard("commander", "Commander", "{2}{G}{W}", 4m, "Legendary Creature — Advisor"),
            CreateCard("hybrid-spell", "Hybrid Spell", "{W/U}{W/U}", 2m, "Instant"),
            CreateCard("big-spell", "Big Spell", "{8}", 8m, "Sorcery"),
            CreateCard("utility-land", "Utility Land", null, 0m, "Land"));

        var result = sut.Calculate(deck, profiles);

        result.ManaValueHistogram.Should().HaveCount(9);
        Slice(result.ManaValueHistogram, "2").Value.Should().Be(2m);
        Slice(result.ManaValueHistogram, "4").Value.Should().Be(1m);
        Slice(result.ManaValueHistogram, "8+").Value.Should().Be(1m);
        Slice(result.ManaValueHistogram, "2").Share.Should().Be(0.5m);
        Slice(result.ManaValueHistogram, "0").Value.Should().Be(0m);

        Slice(result.ManaCostDistribution, "White").Value.Should().Be(3m);
        Slice(result.ManaCostDistribution, "Blue").Value.Should().Be(2m);
        Slice(result.ManaCostDistribution, "Green").Value.Should().Be(1m);
        Slice(result.ManaCostDistribution, "Colorless").Value.Should().Be(10m);

        result.ManaCurve.SpellCount.Should().Be(3);
        result.ManaCurve.AverageManaValue.Should().Be(4m);
        Slice(result.ManaCurve.Buckets, "2").Value.Should().Be(2m);
        Slice(result.ManaCurve.Buckets, "8+").Value.Should().Be(1m);
    }

    [Fact]
    public void Calculate_builds_mana_generation_distribution_from_any_color_colorless_and_symbols()
    {
        var deck = CreateDeck(
            ("rainbow-land", 1, false),
            ("combination-land", 1, false),
            ("powerstone", 2, false),
            ("dual-land", 1, false),
            ("druid", 1, false));
        var profiles = CreateProfiles(
            CreateCard("rainbow-land", "Rainbow Land", null, 0m, "Land", "Add two mana of any one color."),
            CreateCard("combination-land", "Combination Land", null, 0m, "Land", "Add three mana in any combination of colors."),
            CreateCard("powerstone", "Powerstone", null, 2m, "Artifact", "Add one colorless mana."),
            CreateCard("dual-land", "Dual Land", null, 0m, "Land", "{T}: Add {G} or {W}."),
            CreateCard("druid", "Druid", "{G}", 1m, "Creature — Elf Druid", "{T}: Add {G}."));

        var result = sut.Calculate(deck, profiles);

        Slice(result.ManaGenerationDistribution, "Any").Value.Should().Be(5m);
        Slice(result.ManaGenerationDistribution, "Colorless").Value.Should().Be(2m);
        Slice(result.ManaGenerationDistribution, "Green").Value.Should().Be(2m);
        Slice(result.ManaGenerationDistribution, "White").Value.Should().Be(1m);
        result.ManaGenerationDistribution.Should().NotContain(slice => slice.Label == "Blue");
    }

    [Fact]
    public void Calculate_groups_cards_into_supported_primary_type_buckets()
    {
        var deck = CreateDeck(
            ("commander", 1, true),
            ("artifact", 1, false),
            ("enchantment", 1, false),
            ("instant", 1, false),
            ("sorcery", 1, false),
            ("planeswalker", 1, false),
            ("land", 1, false),
            ("battle", 1, false),
            ("kindred", 1, false),
            ("oddity", 1, false),
            ("missing-profile", 1, false));
        var profiles = CreateProfiles(
            CreateCard("commander", "Commander", "{3}{W}", 4m, "Legendary Creature — Human"),
            CreateCard("artifact", "Artifact", "{2}", 2m, "Artifact"),
            CreateCard("enchantment", "Enchantment", "{2}{W}", 3m, "Enchantment"),
            CreateCard("instant", "Instant", "{1}{U}", 2m, "Instant"),
            CreateCard("sorcery", "Sorcery", "{2}{R}", 3m, "Sorcery"),
            CreateCard("planeswalker", "Planeswalker", "{4}{G}", 5m, "Planeswalker"),
            CreateCard("land", "Land", null, 0m, "Land"),
            CreateCard("battle", "Battle", "{2}{R}", 3m, "Battle — Siege"),
            CreateCard("kindred", "Kindred", "{3}{B}", 4m, "Kindred Sorcery"),
            CreateCard("oddity", "Oddity", "{1}", 1m, "Conspiracy"));

        var result = sut.Calculate(deck, profiles);

        Slice(result.CardTypeDistribution, "Creature").Value.Should().Be(1m);
        Slice(result.CardTypeDistribution, "Artifact").Value.Should().Be(1m);
        Slice(result.CardTypeDistribution, "Enchantment").Value.Should().Be(1m);
        Slice(result.CardTypeDistribution, "Instant").Value.Should().Be(1m);
        Slice(result.CardTypeDistribution, "Sorcery").Value.Should().Be(1m);
        Slice(result.CardTypeDistribution, "Planeswalker").Value.Should().Be(1m);
        Slice(result.CardTypeDistribution, "Land").Value.Should().Be(1m);
        Slice(result.CardTypeDistribution, "Battle").Value.Should().Be(1m);
        Slice(result.CardTypeDistribution, "Kindred").Value.Should().Be(1m);
        Slice(result.CardTypeDistribution, "Other").Value.Should().Be(1m);
        result.CardTypeDistribution.Should().HaveCount(10);
    }

    private static Deck CreateDeck(params (string CardId, int Quantity, bool IsCommander)[] entries)
    {
        var deck = new Deck("deck-1", "Coverage Deck");

        foreach (var entry in entries)
        {
            deck.UpsertEntry(entry.CardId, entry.Quantity, assignedPileId: null, entry.IsCommander, isCompanion: false);
        }

        return deck;
    }

    private static IReadOnlyDictionary<string, CardProfile> CreateProfiles(params CardProfile[] profiles) =>
        profiles.ToDictionary(profile => profile.CardId, StringComparer.OrdinalIgnoreCase);

    private static CardProfile CreateCard(
        string cardId,
        string name,
        string? manaCost,
        decimal manaValue,
        string typeLine,
        string? oracleText = null) => new()
    {
        CardId = cardId,
        OracleId = $"{cardId}-oracle",
        Name = name,
        ManaCost = manaCost,
        ManaValue = manaValue,
        TypeLine = typeLine,
        OracleText = oracleText,
        FaceProfiles = [ new CardFaceProfile("0", name, manaCost, typeLine, oracleText, null, true) ],
    };

    private static Application.Contracts.DeckStatSliceContract Slice(
        IReadOnlyList<Application.Contracts.DeckStatSliceContract> slices,
        string label) => slices.Single(slice => slice.Label == label);
}
