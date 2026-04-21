using System.Text.Json;
using CommandSynergy.Application.Abstractions;
using CommandSynergy.Application.Contracts;
using CommandSynergy.Domain.Cards;
using CommandSynergy.Domain.Decks;

namespace CommandSynergy.Application.Tests.Analysis;

public static class ThemeAnalysisTestData
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static ThemeAnalysisFixture LoadFocusedFixture() => LoadFixture("focused-deck.json");

    public static ThemeAnalysisFixture LoadUnfocusedFixture() => LoadFixture("unfocused-deck.json");

    public static ThemeAnalysisFixture LoadCommanderMisalignedFixture() => LoadFixture("commander-misaligned-deck.json");

    public static ThemeAnalysisFixture CreateLargeFocusedDeck(int supportCardCount = 99)
    {
        var cards = new List<FixtureCardDocument>
        {
            new()
            {
                CardId = "large-focused-commander",
                Name = "Large Focused Commander",
                TypeLine = "Legendary Creature",
                IsCommander = true,
                ThemeSignals = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Tokens"] = 0.95m,
                },
            },
        };

        var groups = new[]
        {
            new FixtureGroupDocument
            {
                Prefix = "large-token",
                NameTemplate = "Large Token {n}",
                TypeLine = "Creature",
                Count = supportCardCount,
                ThemeSignals = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Tokens"] = 0.84m,
                },
            },
        };

        return BuildFixture(new ThemeFixtureDocument
        {
            DeckName = "Large Focused Tokens",
            Cards = cards,
            Groups = groups,
            EdhrecSynergyByCardId = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                ["large-token-01"] = 0.64m,
                ["large-token-02"] = 0.60m,
                ["large-token-03"] = 0.57m,
            },
        });
    }

    private static ThemeAnalysisFixture LoadFixture(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Analysis", "Fixtures", fileName);
        using var stream = File.OpenRead(path);
        var document = JsonSerializer.Deserialize<ThemeFixtureDocument>(stream, SerializerOptions)
            ?? throw new InvalidOperationException($"Fixture '{fileName}' could not be deserialized.");

        return BuildFixture(document);
    }

    private static ThemeAnalysisFixture BuildFixture(ThemeFixtureDocument document)
    {
        var deck = new Deck(name: document.DeckName);
        var entries = new List<DeckEntryContract>();
        var profiles = new Dictionary<string, CardProfile>(StringComparer.OrdinalIgnoreCase);

        foreach (var card in document.Cards)
        {
            AddCard(card.CardId, card.Name, card.TypeLine, card.Quantity, card.IsCommander, card.ThemeSignals);
        }

        foreach (var group in document.Groups)
        {
            for (var index = 1; index <= group.Count; index++)
            {
                var cardId = $"{group.Prefix}-{index:00}";
                var name = group.NameTemplate.Replace("{n}", index.ToString("00", System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal);
                AddCard(cardId, name, group.TypeLine, group.Quantity, false, group.ThemeSignals);
            }
        }

        var snapshot = new DeckSnapshotContract
        {
            DeckId = $"fixture-{document.DeckName.Replace(' ', '-').ToLowerInvariant()}",
            Name = document.DeckName,
            CommanderCardId = entries.SingleOrDefault(static entry => entry.IsCommander)?.CardId,
            Entries = entries,
            Piles = Array.Empty<PileDefinitionContract>(),
        };

        var edhrecInsights = document.EdhrecSynergyByCardId.Count == 0
            ? CommanderThemeInsights.Empty()
            : new CommanderThemeInsights("fixture-commander", true, new Dictionary<string, decimal>(document.EdhrecSynergyByCardId, StringComparer.OrdinalIgnoreCase));

        return new ThemeAnalysisFixture(deck, snapshot, profiles, edhrecInsights);

        void AddCard(string cardId, string name, string typeLine, int quantity, bool isCommander, IReadOnlyDictionary<string, decimal> themeSignals)
        {
            deck.UpsertEntry(cardId, quantity, isCommander: isCommander);
            entries.Add(new DeckEntryContract
            {
                CardId = cardId,
                Quantity = quantity,
                IsCommander = isCommander,
            });

            profiles[cardId] = new CardProfile
            {
                CardId = cardId,
                OracleId = cardId + "-oracle",
                Name = name,
                ManaValue = 3,
                TypeLine = typeLine,
                ThemeSignals = new Dictionary<string, decimal>(themeSignals, StringComparer.OrdinalIgnoreCase),
                FaceProfiles = [ new CardFaceProfile("0", name, null, typeLine, null, null, true) ],
            };
        }
    }

    private sealed record ThemeFixtureDocument
    {
        public string DeckName { get; init; } = string.Empty;

        public IReadOnlyList<FixtureCardDocument> Cards { get; init; } = Array.Empty<FixtureCardDocument>();

        public IReadOnlyList<FixtureGroupDocument> Groups { get; init; } = Array.Empty<FixtureGroupDocument>();

        public IReadOnlyDictionary<string, decimal> EdhrecSynergyByCardId { get; init; } = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
    }

    private sealed record FixtureCardDocument
    {
        public string CardId { get; init; } = string.Empty;

        public string Name { get; init; } = string.Empty;

        public string TypeLine { get; init; } = "Creature";

        public int Quantity { get; init; } = 1;

        public bool IsCommander { get; init; }

        public IReadOnlyDictionary<string, decimal> ThemeSignals { get; init; } = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
    }

    private sealed record FixtureGroupDocument
    {
        public string Prefix { get; init; } = string.Empty;

        public string NameTemplate { get; init; } = string.Empty;

        public string TypeLine { get; init; } = "Creature";

        public int Count { get; init; }

        public int Quantity { get; init; } = 1;

        public IReadOnlyDictionary<string, decimal> ThemeSignals { get; init; } = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
    }
}

public sealed record ThemeAnalysisFixture(
    Deck Deck,
    DeckSnapshotContract Snapshot,
    IReadOnlyDictionary<string, CardProfile> Profiles,
    CommanderThemeInsights EdhrecInsights);