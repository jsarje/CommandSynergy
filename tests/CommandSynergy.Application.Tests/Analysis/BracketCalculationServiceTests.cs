using CommandSynergy.Application.Analysis;
using CommandSynergy.Application.Configuration;
using CommandSynergy.Domain.Analysis;
using CommandSynergy.Domain.Cards;
using CommandSynergy.Domain.Decks;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace CommandSynergy.Application.Tests.Analysis;

public sealed class BracketCalculationServiceTests
{
    private readonly BracketCalculationService sut = new(
        new Domain.Analysis.BracketEngine(),
        new AnalysisExplanationBuilder(),
        Options.Create(new BracketOptions { LevelThresholds = [0, 10, 15] }));

    [Fact]
    public void Calculate_returns_bracket_one_when_no_synergy_or_escalating_signals_exist()
    {
        var deck = CreateDeck("filler-card");
        var profiles = CreateProfiles(CreateCard("filler-card", "Filler Card", manaValue: 3m));

        var result = sut.Calculate(
            deck,
            profiles,
            ComboAnalysis.Empty(),
            CreateSynergyAssessment(score: 0m, qualitativeLabel: "Unfocused", commanderSpecificHits: []));

        result.BracketLevel.Should().Be(1);
        result.ContributingFactors.Should().BeEmpty();
    }

    [Fact]
    public void Calculate_returns_bracket_two_when_the_deck_has_meaningful_synergy_without_higher_bracket_signals()
    {
        var deck = CreateDeck("engine-card");
        var profiles = CreateProfiles(CreateCard("engine-card", "Engine Card", manaValue: 3m));

        var result = sut.Calculate(
            deck,
            profiles,
            ComboAnalysis.Empty(),
            CreateSynergyAssessment(score: 67m, commanderSpecificHits: ["Engine Card"]));

        result.BracketLevel.Should().Be(2);
        result.ContributingFactors.Should().ContainSingle(factor => factor.Category == "synergy");
    }

    [Fact]
    public void Calculate_returns_bracket_two_when_the_deck_shows_any_non_random_coherence()
    {
        var deck = CreateDeck("theme-card");
        var profiles = CreateProfiles(CreateCard("theme-card", "Theme Card", manaValue: 3m));

        var result = sut.Calculate(
            deck,
            profiles,
            ComboAnalysis.Empty(),
            CreateSynergyAssessment(score: 28m));

        result.BracketLevel.Should().Be(2);
    }

    [Fact]
    public void Calculate_returns_bracket_three_for_late_two_card_combos()
    {
        var deck = CreateDeck("card-a", "card-b");
        var profiles = CreateProfiles(
            CreateCard("card-a", "Card A", manaValue: 4m),
            CreateCard("card-b", "Card B", manaValue: 3m));
        var comboAnalysis = new ComboAnalysis(
            [ new ComboResult(["Card A", "Card B"], ["Win the game"], "Resolve both pieces.", string.Empty) ],
            [],
            0,
            DateTimeOffset.UtcNow);

        var result = sut.Calculate(deck, profiles, comboAnalysis, CreateSynergyAssessment(score: 35m));

        result.BracketLevel.Should().Be(3);
        result.ContributingFactors.Should().ContainSingle(factor => factor.Category == "late-two-card-combo");
    }

    [Fact]
    public void Calculate_returns_bracket_four_for_mass_land_denial_or_early_two_card_combos()
    {
        var deck = CreateDeck("armageddon");
        var profiles = CreateProfiles(CreateCard("armageddon", "Armageddon", manaValue: 4m, isMassLandDenial: true));

        var result = sut.Calculate(deck, profiles, ComboAnalysis.Empty(), CreateSynergyAssessment(score: 45m));

        result.BracketLevel.Should().Be(4);
        result.ContributingFactors.Should().ContainSingle(factor =>
            factor.Category == "mass-land-denial"
            && factor.SourceCardId == "armageddon");
    }

    [Fact]
    public void Calculate_returns_bracket_five_for_highly_optimized_decks_with_many_game_changers_and_infinite_combos()
    {
        var deck = CreateDeck("combo-a", "combo-b", "changer-1", "changer-2", "changer-3", "changer-4", "changer-5");
        var profiles = CreateProfiles(
            CreateCard("combo-a", "Combo A", manaValue: 2m),
            CreateCard("combo-b", "Combo B", manaValue: 2m),
            CreateCard("changer-1", "Changer 1", manaValue: 2m, isGameChanger: true),
            CreateCard("changer-2", "Changer 2", manaValue: 2m, isGameChanger: true),
            CreateCard("changer-3", "Changer 3", manaValue: 2m, isGameChanger: true),
            CreateCard("changer-4", "Changer 4", manaValue: 2m, isGameChanger: true),
            CreateCard("changer-5", "Changer 5", manaValue: 2m, isGameChanger: true));
        var comboAnalysis = new ComboAnalysis(
            [
                new ComboResult(["Combo A", "Combo B"], ["Infinite mana"], "Loop the pieces infinitely.", string.Empty),
                new ComboResult(["Combo A", "Changer 1"], ["Infinite tokens"], "Create infinite tokens.", string.Empty),
                new ComboResult(["Combo B", "Changer 2"], ["Infinite damage"], "Deal infinite damage.", string.Empty),
            ],
            [],
            0,
            DateTimeOffset.UtcNow);

        var result = sut.Calculate(
            deck,
            profiles,
            comboAnalysis,
            CreateSynergyAssessment(score: 86m, qualitativeLabel: "Tuned", commanderSpecificHits: ["Combo A", "Combo B"]));

        result.BracketLevel.Should().Be(5);
        result.ContributingFactors.Should().Contain(factor => factor.Category == "optimization");
    }

    private static Deck CreateDeck(params string[] cardIds)
    {
        var deck = new Deck();

        foreach (var cardId in cardIds)
        {
            deck.UpsertEntry(cardId, 1);
        }

        return deck;
    }

    private static IReadOnlyDictionary<string, CardProfile> CreateProfiles(params CardProfile[] profiles) =>
        profiles.ToDictionary(static profile => profile.CardId, StringComparer.OrdinalIgnoreCase);

    private static SynergyAssessment CreateSynergyAssessment(
        decimal score,
        string qualitativeLabel = "Developing",
        IReadOnlyList<string>? commanderSpecificHits = null) => new(
            score,
            commanderSpecificHits ?? Array.Empty<string>(),
            Array.Empty<string>(),
            string.Empty,
            DateTimeOffset.UtcNow,
            score,
            score,
            qualitativeLabel,
            false);

    private static CardProfile CreateCard(
        string cardId,
        string name,
        decimal manaValue,
        bool isGameChanger = false,
        bool isMassLandDenial = false) => new()
    {
        CardId = cardId,
        Name = name,
        ManaValue = manaValue,
        TypeLine = "Artifact",
        IsGameChanger = isGameChanger,
        IsMassLandDenial = isMassLandDenial,
        FaceProfiles = [ new CardFaceProfile("0", name, null, "Artifact", null, null, true) ],
    };
}
