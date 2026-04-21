using CommandSynergy.Application.Abstractions;
using CommandSynergy.Application.Analysis;
using CommandSynergy.Domain.Analysis;
using CommandSynergy.Domain.Cards;
using CommandSynergy.Domain.Decks;
using FluentAssertions;

namespace CommandSynergy.Application.Tests.Analysis;

public sealed class ThemeAnalysisServiceTests
{
    private readonly ThemeAnalysisService sut = new(new ThemeMatchingService(), new AnalysisExplanationBuilder());

    [Fact]
    public async Task AnalyseAsync_returns_insufficient_result_for_small_decks()
    {
        var deck = new Deck();
        deck.UpsertEntry("commander", 1, isCommander: true);
        deck.UpsertEntry("card-a", 1);
        deck.UpsertEntry("card-b", 1);

        var profiles = new Dictionary<string, CardProfile>(StringComparer.OrdinalIgnoreCase)
        {
            ["commander"] = CreateCard("commander", "Commander", new Dictionary<string, decimal> { ["Tokens"] = 0.8m }),
            ["card-a"] = CreateCard("card-a", "Card A", new Dictionary<string, decimal> { ["Tokens"] = 0.6m }),
            ["card-b"] = CreateCard("card-b", "Card B", new Dictionary<string, decimal> { ["Ramp"] = 0.4m }),
        };

        var (analysis, synergy) = await sut.AnalyseAsync(deck, profiles, CommanderThemeInsights.Empty());

        analysis.IsInsufficient.Should().BeTrue();
        synergy.FinalScore.Should().Be(0m);
    }

    [Fact]
    public async Task AnalyseAsync_ranks_primary_theme_and_commander_alignment_for_focused_deck()
    {
        var deck = new Deck();
        deck.UpsertEntry("commander", 1, isCommander: true);

        var profiles = new Dictionary<string, CardProfile>(StringComparer.OrdinalIgnoreCase)
        {
            ["commander"] = CreateCard("commander", "Commander", new Dictionary<string, decimal> { ["Tokens"] = 0.9m }),
        };

        for (var index = 1; index <= 24; index++)
        {
            var cardId = $"token-{index}";
            deck.UpsertEntry(cardId, 1);
            profiles[cardId] = CreateCard(cardId, $"Token {index}", new Dictionary<string, decimal> { ["Tokens"] = 0.8m });
        }

        var (analysis, synergy) = await sut.AnalyseAsync(deck, profiles, CommanderThemeInsights.Empty());

        analysis.IsInsufficient.Should().BeFalse();
        analysis.RankedThemes[0].Name.Should().Be("Tokens");
        analysis.CommanderAlignment.Level.Should().Be(AlignmentLevel.Strong);
        synergy.ThemeScore.Should().BeGreaterThan(50m);
        synergy.QualitativeLabel.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task AnalyseAsync_flags_missing_profiles_as_metadata_unavailable_off_theme_cards()
    {
        var deck = new Deck();
        deck.UpsertEntry("commander", 1, isCommander: true);

        var profiles = new Dictionary<string, CardProfile>(StringComparer.OrdinalIgnoreCase)
        {
            ["commander"] = CreateCard("commander", "Commander", new Dictionary<string, decimal> { ["Tokens"] = 0.9m }),
        };

        for (var index = 1; index <= 20; index++)
        {
            var cardId = $"card-{index}";
            deck.UpsertEntry(cardId, 1);
            if (index < 20)
            {
                profiles[cardId] = CreateCard(cardId, $"Card {index}", new Dictionary<string, decimal> { ["Tokens"] = 0.4m });
            }
        }

        var (analysis, _) = await sut.AnalyseAsync(deck, profiles, CommanderThemeInsights.Empty());

        analysis.OffThemeCards.Should().Contain(card => card.CardId == "card-20" && card.MetadataUnavailable);
    }

    private static CardProfile CreateCard(string cardId, string name, IReadOnlyDictionary<string, decimal> themeSignals) => new()
    {
        CardId = cardId,
        Name = name,
        OracleId = cardId + "-oracle",
        ManaValue = 3,
        TypeLine = "Creature",
        ThemeSignals = new Dictionary<string, decimal>(themeSignals, StringComparer.OrdinalIgnoreCase),
        FaceProfiles = [ new CardFaceProfile("0", name, null, "Creature", null, null, true) ],
    };
}