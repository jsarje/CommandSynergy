using System.Text.Json;
using CommandSynergy.Application.Abstractions;
using CommandSynergy.Application.Contracts;
using CommandSynergy.Domain.Cards;

namespace CommandSynergy.WebUI.Tests;

public static class ThemeAnalysisTestData
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static DeckAnalysisResponseContract CreateReadyAnalysis(bool isInsufficient = false) => new()
    {
        Bracket = new BracketAssessmentContract
        {
            Level = 3,
            TotalWeight = 10m,
            Summary = "Bracket summary.",
            Factors = Array.Empty<BracketFactorContract>(),
        },
        Synergy = new SynergyAssessmentContract
        {
            Score = 70m,
            ThemeScore = 68m,
            FinalScore = 70m,
            QualitativeLabel = "Focused",
            Summary = "Strong focus.",
            CommanderSpecificHits = Array.Empty<string>(),
            StapleOverloadIndicators = Array.Empty<string>(),
        },
        ThemeAnalysis = new ThemeAnalysisContract
        {
            RankedThemes =
            [
                new DeckThemeContract
                {
                    Name = "Tokens",
                    Description = "Creates a wide board.",
                    Strength = 0.72m,
                    StrengthLabel = "Strong",
                    ContributingCardIds = ["card-a", "card-b"],
                    ContributingCardCount = 2,
                    Contributors =
                    [
                        new ThemeContributorContract
                        {
                            CardId = "card-a",
                            CardName = "Token Card",
                            Signal = 0.72m,
                            Reason = "Matched the card's oracle text.",
                        },
                        new ThemeContributorContract
                        {
                            CardId = "card-b",
                            CardName = "Dual Theme Card",
                            Signal = 0.58m,
                            Reason = "Matched keyword and token patterns.",
                        },
                    ],
                },
                new DeckThemeContract
                {
                    Name = "Aristocrats",
                    Description = "Turns sacrifice loops into pressure.",
                    Strength = 0.41m,
                    StrengthLabel = "Moderate",
                    ContributingCardIds = ["card-b"],
                    ContributingCardCount = 1,
                    Contributors =
                    [
                        new ThemeContributorContract
                        {
                            CardId = "card-b",
                            CardName = "Dual Theme Card",
                            Signal = 0.41m,
                            Reason = "Matched sacrifice triggers.",
                        },
                    ],
                },
            ],
            PrimaryThemes =
            [
                new DeckThemeContract
                {
                    Name = "Tokens",
                    Description = "Creates a wide board.",
                    Strength = 0.72m,
                    StrengthLabel = "Strong",
                    ContributingCardIds = ["card-a", "card-b"],
                    ContributingCardCount = 2,
                    Contributors = Array.Empty<ThemeContributorContract>(),
                },
                new DeckThemeContract
                {
                    Name = "Aristocrats",
                    Description = "Turns sacrifice loops into pressure.",
                    Strength = 0.41m,
                    StrengthLabel = "Moderate",
                    ContributingCardIds = ["card-b"],
                    ContributingCardCount = 1,
                    Contributors = Array.Empty<ThemeContributorContract>(),
                },
            ],
            OffThemeCards =
            [
                new OffThemeCardContract
                {
                    CardId = "staple-card",
                    CardName = "Staple Card",
                    Reason = "No strong theme signal was detected for this card.",
                },
                new OffThemeCardContract
                {
                    CardId = "unknown-card",
                    CardName = "Unknown Card",
                    Reason = "Metadata was unavailable during analysis.",
                    MetadataUnavailable = true,
                },
            ],
            CommanderAlignment = new CommanderAlignmentContract
            {
                Level = "Strong",
                CommanderTopTheme = "Tokens",
                DeckStrengthForCommanderTheme = 0.72m,
                EvidenceCardIds = ["card-a", "card-b"],
                Summary = "The 99 strongly reinforce the commander's plan.",
            },
            AnalysedCardCount = 25,
            IsInsufficient = isInsufficient,
            AnalysedAtUtc = DateTimeOffset.UtcNow,
            RefreshSummary = "Primary themes: Tokens, Aristocrats. 2 card(s) are currently off-theme.",
        },
    };

    public static ThemeEndpointFixture LoadFixture(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Analysis", "Fixtures", fileName);
        using var stream = File.OpenRead(path);
        var document = JsonSerializer.Deserialize<ThemeFixtureDocument>(stream, SerializerOptions)
            ?? throw new InvalidOperationException($"Fixture '{fileName}' could not be deserialized.");

        var snapshotEntries = new List<DeckEntryContract>();
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

        return new ThemeEndpointFixture(
            new DeckSnapshotContract
            {
                DeckId = $"fixture-{document.DeckName.Replace(' ', '-').ToLowerInvariant()}",
                Name = document.DeckName,
                CommanderCardId = snapshotEntries.SingleOrDefault(static entry => entry.IsCommander)?.CardId,
                Entries = snapshotEntries,
                Piles = Array.Empty<PileDefinitionContract>(),
            },
            profiles,
            document.EdhrecSynergyByCardId.Count == 0
                ? CommanderThemeInsights.Empty()
                : new CommanderThemeInsights("fixture-commander", true, new Dictionary<string, decimal>(document.EdhrecSynergyByCardId, StringComparer.OrdinalIgnoreCase)));

        void AddCard(string cardId, string name, string typeLine, int quantity, bool isCommander, IReadOnlyDictionary<string, decimal> themeSignals)
        {
            snapshotEntries.Add(new DeckEntryContract
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

public sealed record ThemeEndpointFixture(
    DeckSnapshotContract Snapshot,
    IReadOnlyDictionary<string, CardProfile> Profiles,
    CommanderThemeInsights EdhrecInsights);