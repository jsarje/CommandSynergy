using CommandSynergy.Application.Abstractions;
using CommandSynergy.Application.Analysis;
using CommandSynergy.Application.Contracts;
using CommandSynergy.Domain.Cards;
using FluentAssertions;

namespace CommandSynergy.Application.Tests.Analysis;

public sealed class DeckSuggestionServiceTests
{
    [Fact]
    public async Task GetSuggestionsAsync_returns_top_three_candidates_excluding_current_deck_and_seen_cards()
    {
        var commander = CreateCard(
            "commander",
            "Commander",
            oracleId: "commander-oracle",
            typeLine: "Legendary Creature",
            colorIdentity: ["G"],
            themeSignals: new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                ["Tokens"] = 0.9m,
            },
            commanderEligibilityBasis: CommanderEligibilityBasis.LegendaryCreature);
        var deckCard = CreateCard(
            "deck-card",
            "Deck Card",
            themeSignals: new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                ["Tokens"] = 0.7m,
            });
        var candidateA = CreateCard(
            "candidate-a",
            "Candidate A",
            eurPrice: 3m,
            themeSignals: new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                ["Tokens"] = 0.9m,
            });
        var candidateB = CreateCard(
            "candidate-b",
            "Candidate B",
            eurPrice: 4m,
            themeSignals: new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                ["Tokens"] = 0.82m,
            });
        var candidateC = CreateCard(
            "candidate-c",
            "Candidate C",
            eurPrice: 2m,
            themeSignals: new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                ["Tokens"] = 0.8m,
            });
        var candidateD = CreateCard(
            "candidate-d",
            "Candidate D",
            eurPrice: 1m,
            themeSignals: new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                ["Tokens"] = 0.6m,
            });
        var sut = new DeckSuggestionService(
            new StubCardCatalogGateway([commander, deckCard, candidateA, candidateB, candidateC, candidateD]),
            new StubEdhrecClient(new CommanderThemeInsights("commander", true, new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                ["candidate-a"] = 0.72m,
                ["candidate-b"] = 0.65m,
                ["candidate-c"] = 0.61m,
                ["candidate-d"] = 0.15m,
            })),
            new ThemeMatchingService());

        var response = await sut.GetSuggestionsAsync(new DeckSuggestionsRequestContract
        {
            Deck = new DeckSnapshotContract
            {
                CommanderCardId = "commander",
                Entries =
                [
                    new DeckEntryContract { CardId = "commander", Quantity = 1, IsCommander = true },
                    new DeckEntryContract { CardId = "deck-card", Quantity = 1 },
                ],
            },
            ExcludedCardIds = ["candidate-c"],
            Limit = 3,
        });

        response.Suggestions.Select(static suggestion => suggestion.Card.CardId).Should().Equal("candidate-a", "candidate-b", "candidate-d");
    }

    [Fact]
    public async Task GetSuggestionsAsync_applies_targeted_filters()
    {
        var commander = CreateCard(
            "commander",
            "Commander",
            oracleId: "commander-oracle",
            typeLine: "Legendary Creature",
            colorIdentity: ["G", "W"],
            themeSignals: new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                ["Counters"] = 0.9m,
            },
            commanderEligibilityBasis: CommanderEligibilityBasis.LegendaryCreature);
        var matchingCard = CreateCard(
            "matching-card",
            "Matching Card",
            manaValue: 3m,
            typeLine: "Creature",
            colorIdentity: ["G"],
            eurPrice: 4.5m,
            themeSignals: new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                ["Counters"] = 0.8m,
            });
        var expensiveCard = CreateCard(
            "expensive-card",
            "Expensive Card",
            manaValue: 3m,
            typeLine: "Creature",
            colorIdentity: ["G"],
            eurPrice: 14m,
            themeSignals: new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                ["Counters"] = 0.9m,
            });
        var wrongTypeCard = CreateCard(
            "wrong-type",
            "Wrong Type",
            manaValue: 3m,
            typeLine: "Artifact",
            colorIdentity: ["G"],
            eurPrice: 2m,
            themeSignals: new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                ["Counters"] = 0.9m,
            });
        var sut = new DeckSuggestionService(
            new StubCardCatalogGateway([commander, matchingCard, expensiveCard, wrongTypeCard]),
            new StubEdhrecClient(CommanderThemeInsights.Empty("commander")),
            new ThemeMatchingService());

        var response = await sut.GetSuggestionsAsync(new DeckSuggestionsRequestContract
        {
            Deck = new DeckSnapshotContract
            {
                CommanderCardId = "commander",
                Entries =
                [
                    new DeckEntryContract { CardId = "commander", Quantity = 1, IsCommander = true },
                ],
            },
            Filters = new DeckSuggestionFiltersContract
            {
                CardType = "Creature",
                ManaValue = 3,
                ColorIdentity = ["G"],
                MaxEurPrice = 5m,
            },
            Limit = 3,
        });

        response.Suggestions.Should().ContainSingle();
        response.Suggestions[0].Card.CardId.Should().Be("matching-card");
    }

    [Fact]
    public async Task GetSuggestionsAsync_matches_colorless_filter_to_cards_with_colorless_identity()
    {
        var commander = CreateCard(
            "commander",
            "Commander",
            oracleId: "commander-oracle",
            typeLine: "Legendary Creature",
            colorIdentity: ["W"],
            commanderEligibilityBasis: CommanderEligibilityBasis.LegendaryCreature);
        var colorlessCard = CreateCard(
            "colorless-card",
            "Colorless Card",
            typeLine: "Artifact",
            colorIdentity: Array.Empty<string>(),
            themeSignals: new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                ["Artifacts"] = 0.8m,
            });
        var whiteCard = CreateCard(
            "white-card",
            "White Card",
            typeLine: "Artifact",
            colorIdentity: ["W"],
            themeSignals: new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                ["Artifacts"] = 0.8m,
            });
        var sut = new DeckSuggestionService(
            new StubCardCatalogGateway([commander, colorlessCard, whiteCard]),
            new StubEdhrecClient(CommanderThemeInsights.Empty("commander")),
            new ThemeMatchingService());

        var response = await sut.GetSuggestionsAsync(new DeckSuggestionsRequestContract
        {
            Deck = new DeckSnapshotContract
            {
                CommanderCardId = "commander",
                Entries =
                [
                    new DeckEntryContract { CardId = "commander", Quantity = 1, IsCommander = true },
                ],
            },
            Filters = new DeckSuggestionFiltersContract
            {
                ColorIdentity = ["C"],
            },
            Limit = 3,
        });

        response.Suggestions.Should().ContainSingle();
        response.Suggestions[0].Card.CardId.Should().Be("colorless-card");
    }

    private static CardProfile CreateCard(
        string cardId,
        string name,
        string? oracleId = null,
        string? manaCost = "{2}",
        decimal manaValue = 2m,
        string typeLine = "Artifact",
        IReadOnlyList<string>? colorIdentity = null,
        decimal? eurPrice = null,
        IReadOnlyDictionary<string, decimal>? themeSignals = null,
        CommanderEligibilityBasis commanderEligibilityBasis = CommanderEligibilityBasis.Unknown) => new()
    {
        CardId = cardId,
        OracleId = oracleId ?? $"{cardId}-oracle",
        Name = name,
        ManaCost = manaCost,
        ManaValue = manaValue,
        TypeLine = typeLine,
        ColorIdentity = colorIdentity ?? Array.Empty<string>(),
        EurPrice = eurPrice,
        ThemeSignals = themeSignals ?? new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase),
        IsLegalInCommander = true,
        CommanderEligibilityBasis = commanderEligibilityBasis,
        FaceProfiles = [new CardFaceProfile("0", name, manaCost, typeLine, null, null, true)],
    };

    private sealed class StubCardCatalogGateway : ICardCatalogGateway
    {
        private readonly IReadOnlyDictionary<string, CardProfile> cards;

        public StubCardCatalogGateway(IEnumerable<CardProfile> cards)
        {
            this.cards = cards.ToDictionary(static card => card.CardId, StringComparer.OrdinalIgnoreCase);
        }

        public Task<IReadOnlyDictionary<string, CardProfile>> GetCardProfilesAsync(IEnumerable<string> cardIds, CancellationToken cancellationToken = default) =>
            Task.FromResult((IReadOnlyDictionary<string, CardProfile>)cards);

        public Task<IReadOnlyList<CardProfile>> GetCommanderLegalCardProfilesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult((IReadOnlyList<CardProfile>)cards.Values.ToArray());

        public Task<IReadOnlyList<CardSearchResultContract>> SearchAsync(CardSearchQueryContract request, CancellationToken cancellationToken = default) =>
            Task.FromResult((IReadOnlyList<CardSearchResultContract>)Array.Empty<CardSearchResultContract>());

        public Task<string?> GetSnapshotVersionAsync(CancellationToken cancellationToken = default) => Task.FromResult<string?>("suggestions-v1");
    }

    private sealed class StubEdhrecClient : IEdhrecClient
    {
        private readonly CommanderThemeInsights insights;

        public StubEdhrecClient(CommanderThemeInsights insights)
        {
            this.insights = insights;
        }

        public Task<CommanderThemeInsights> GetCommanderThemeInsightsAsync(CardProfile commanderProfile, CancellationToken cancellationToken = default) =>
            Task.FromResult(insights);
    }
}
