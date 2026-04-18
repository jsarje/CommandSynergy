using CommandSynergy.Application.Analysis;
using CommandSynergy.Domain.Cards;
using CommandSynergy.Domain.Decks;
using FluentAssertions;

namespace CommandSynergy.Application.Tests.Analysis;

public sealed class SynergyScoringServiceTests
{
    private readonly SynergyScoringService sut = new(new AnalysisExplanationBuilder());

    [Fact]
    public void Calculate_identifies_commander_specific_hits_and_staple_overload_cards()
    {
        var deck = new Deck();
        deck.UpsertEntry("commander", 1, isCommander: true);
        deck.UpsertEntry("engine-card", 1);
        deck.UpsertEntry("staple-card", 1);

        var profiles = new Dictionary<string, CardProfile>(StringComparer.OrdinalIgnoreCase)
        {
            ["commander"] = CreateCard("commander", "Commander", "commander-oracle"),
            ["engine-card"] = CreateCard("engine-card", "Engine Card", "engine-oracle", new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                ["commander-oracle"] = 0.42m,
            }, 0.08m),
            ["staple-card"] = CreateCard("staple-card", "Staple Card", "staple-oracle", new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                ["commander-oracle"] = 0.05m,
            }, 0.44m),
        };

        var assessment = sut.Calculate(deck, profiles);

        assessment.CommanderSpecificHits.Should().Contain("Engine Card");
        assessment.StapleOverloadIndicators.Should().Contain("Staple Card");
    }

    [Fact]
    public void Calculate_normalizes_scores_to_a_zero_to_one_hundred_scale()
    {
        var deck = new Deck();
        deck.UpsertEntry("commander", 1, isCommander: true);
        deck.UpsertEntry("card-a", 1);
        deck.UpsertEntry("card-b", 1);

        var profiles = new Dictionary<string, CardProfile>(StringComparer.OrdinalIgnoreCase)
        {
            ["commander"] = CreateCard("commander", "Commander", "commander-oracle"),
            ["card-a"] = CreateCard("card-a", "Card A", "a-oracle", new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                ["commander-oracle"] = 0.90m,
            }, 0.00m),
            ["card-b"] = CreateCard("card-b", "Card B", "b-oracle", new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                ["commander-oracle"] = 0.00m,
            }, 0.90m),
        };

        var assessment = sut.Calculate(deck, profiles);

        assessment.SynergyScore.Should().BeGreaterThanOrEqualTo(0m);
        assessment.SynergyScore.Should().BeLessThanOrEqualTo(100m);
    }

    private static CardProfile CreateCard(
        string cardId,
        string name,
        string oracleId,
        IReadOnlyDictionary<string, decimal>? playRateByCommander = null,
        decimal? genericColorStapleRate = null) => new()
    {
        CardId = cardId,
        OracleId = oracleId,
        Name = name,
        ManaValue = 2,
        TypeLine = "Creature",
        GenericColorStapleRate = genericColorStapleRate,
        PlayRateByCommander = playRateByCommander ?? new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase),
        FaceProfiles = [ new CardFaceProfile("0", name, null, "Creature", null, null, true) ],
    };
}