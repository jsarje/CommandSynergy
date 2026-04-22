using CommandSynergy.Application.Abstractions;
using CommandSynergy.Application.Analysis;
using CommandSynergy.Application.Configuration;
using CommandSynergy.Application.Contracts;
using CommandSynergy.Domain.Cards;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace CommandSynergy.Application.Tests.Analysis;

public sealed class DeckAnalysisServiceTests
{
    [Fact]
    public async Task AnalyzeAsync_returns_bracket_and_synergy_results_for_the_supplied_snapshot()
    {
        var gateway = new StubCardCatalogGateway(new Dictionary<string, CardProfile>(StringComparer.OrdinalIgnoreCase)
        {
            ["commander"] = CreateCard("commander", "Commander", "commander-oracle"),
            ["mana-rock"] = CreateCard(
                "mana-rock",
                "Mana Rock",
                "mana-rock-oracle",
                oracleText: "Add one mana of any color.",
                playRateByCommander: new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
                {
                    ["commander-oracle"] = 0.35m,
                },
                genericColorStapleRate: 0.10m,
                saltScore: 2.0m),
        });

        var options = Options.Create(new BracketOptions
        {
            LevelThresholds = [0m, 1m, 2m, 3m, 4m],
            GameChangerWeight = 2.5m,
        });

        var sut = new DeckAnalysisService(
            gateway,
            new StubCommanderSpellbookClient(),
            new StubEdhrecClient(),
            new BracketCalculationService(new Domain.Analysis.BracketEngine(), new AnalysisExplanationBuilder(), options),
            new PowerLevelCalculationService(),
            new SynergyScoringService(new AnalysisExplanationBuilder()),
            new ThemeAnalysisService(new ThemeMatchingService(), new AnalysisExplanationBuilder()),
            Array.Empty<IDeckAdviceService>());

        var response = await sut.AnalyzeAsync(new DeckSnapshotContract
        {
            CommanderCardId = "commander",
            Entries =
            [
                new DeckEntryContract { CardId = "commander", Quantity = 1, IsCommander = true },
                new DeckEntryContract { CardId = "mana-rock", Quantity = 1 },
            ],
        });

        response.Bracket.Level.Should().BeGreaterThan(1);
        response.Bracket.Factors.Should().Contain(factor => factor.Category == "game-changer");
        response.PowerLevel.Score.Should().BeGreaterThan(4m);
        response.Synergy.Score.Should().BeGreaterThan(50m);
    }

    [Fact]
    public async Task AnalyzeAsync_does_not_forward_land_cards_to_Commander_Spellbook()
    {
        var gateway = new StubCardCatalogGateway(new Dictionary<string, CardProfile>(StringComparer.OrdinalIgnoreCase)
        {
            ["commander"] = CreateCard("commander", "Commander", "commander-oracle"),
            ["mountain"] = CreateCard("mountain", "Mountain", "mountain-oracle", typeLine: "Basic Land — Mountain"),
            ["sol-ring"] = CreateCard("sol-ring", "Sol Ring", "sol-ring-oracle", typeLine: "Artifact"),
        });
        var spellbookClient = new CapturingCommanderSpellbookClient();

        var sut = new DeckAnalysisService(
            gateway,
            spellbookClient,
            new StubEdhrecClient(),
            new BracketCalculationService(new Domain.Analysis.BracketEngine(), new AnalysisExplanationBuilder(), Options.Create(new BracketOptions())),
            new PowerLevelCalculationService(),
            new SynergyScoringService(new AnalysisExplanationBuilder()),
            new ThemeAnalysisService(new ThemeMatchingService(), new AnalysisExplanationBuilder()),
            Array.Empty<IDeckAdviceService>());

        await sut.AnalyzeAsync(new DeckSnapshotContract
        {
            CommanderCardId = "commander",
            Entries =
            [
                new DeckEntryContract { CardId = "commander", Quantity = 1, IsCommander = true },
                new DeckEntryContract { CardId = "mountain", Quantity = 24 },
                new DeckEntryContract { CardId = "sol-ring", Quantity = 1 },
            ],
        });

        spellbookClient.ReceivedMainDeckNames.Should().Equal("Sol Ring");
        spellbookClient.ReceivedCommanderNames.Should().Equal("Commander");
    }

    [Fact]
    public async Task AnalyzeAsync_includes_chart_ready_deck_stats()
    {
        var gateway = new StubCardCatalogGateway(new Dictionary<string, CardProfile>(StringComparer.OrdinalIgnoreCase)
        {
            ["commander"] = CreateCard(
                "commander",
                "Commander",
                "commander-oracle",
                manaCost: "{1}{G}{W}",
                manaValue: 3,
                typeLine: "Legendary Creature — Advisor"),
            ["signet"] = CreateCard(
                "signet",
                "Arcane Signet",
                "signet-oracle",
                manaCost: "{2}",
                manaValue: 2,
                typeLine: "Artifact",
                oracleText: "{T}: Add one mana of any color."),
            ["elf"] = CreateCard(
                "elf",
                "Mana Elf",
                "elf-oracle",
                manaCost: "{G}",
                manaValue: 1,
                typeLine: "Creature — Elf Druid",
                oracleText: "{T}: Add {G}."),
            ["removal"] = CreateCard(
                "removal",
                "Generous Gift",
                "removal-oracle",
                manaCost: "{2}{W}",
                manaValue: 3,
                typeLine: "Instant"),
            ["garden"] = CreateCard(
                "garden",
                "Temple Garden",
                "garden-oracle",
                manaValue: 0,
                typeLine: "Land",
                oracleText: "{T}: Add {G} or {W}."),
        });

        var sut = new DeckAnalysisService(
            gateway,
            new StubCommanderSpellbookClient(),
            new StubEdhrecClient(),
            new BracketCalculationService(new Domain.Analysis.BracketEngine(), new AnalysisExplanationBuilder(), Options.Create(new BracketOptions())),
            new PowerLevelCalculationService(),
            new SynergyScoringService(new AnalysisExplanationBuilder()),
            new ThemeAnalysisService(new ThemeMatchingService(), new AnalysisExplanationBuilder()),
            Array.Empty<IDeckAdviceService>());

        var response = await sut.AnalyzeAsync(new DeckSnapshotContract
        {
            CommanderCardId = "commander",
            Entries =
            [
                new DeckEntryContract { CardId = "commander", Quantity = 1, IsCommander = true },
                new DeckEntryContract { CardId = "signet", Quantity = 1 },
                new DeckEntryContract { CardId = "elf", Quantity = 1 },
                new DeckEntryContract { CardId = "removal", Quantity = 1 },
                new DeckEntryContract { CardId = "garden", Quantity = 1 },
            ],
        });

        response.DeckStats.Should().NotBeNull();
        response.DeckStats!.ManaValueHistogram.Should().Contain(slice => slice.Label == "1" && slice.Value == 1m);
        response.DeckStats.ManaValueHistogram.Should().Contain(slice => slice.Label == "2" && slice.Value == 2m);
        response.DeckStats.ManaValueHistogram.Should().Contain(slice => slice.Label == "3" && slice.Value == 1m);
        response.DeckStats.ManaCostDistribution.Should().Contain(slice => slice.Label == "Green" && slice.Value == 2m);
        response.DeckStats.ManaCostDistribution.Should().Contain(slice => slice.Label == "White" && slice.Value == 2m);
        response.DeckStats.ManaCostDistribution.Should().Contain(slice => slice.Label == "Colorless" && slice.Value == 3m);
        response.DeckStats.ManaGenerationDistribution.Should().Contain(slice => slice.Label == "Any" && slice.Value == 1m);
        response.DeckStats.ManaGenerationDistribution.Should().Contain(slice => slice.Label == "Green" && slice.Value == 2m);
        response.DeckStats.ManaGenerationDistribution.Should().Contain(slice => slice.Label == "White" && slice.Value == 1m);
        response.DeckStats.CardTypeDistribution.Should().Contain(slice => slice.Label == "Creature" && slice.Value == 2m);
        response.DeckStats.CardTypeDistribution.Should().Contain(slice => slice.Label == "Artifact" && slice.Value == 1m);
        response.DeckStats.CardTypeDistribution.Should().Contain(slice => slice.Label == "Instant" && slice.Value == 1m);
        response.DeckStats.CardTypeDistribution.Should().Contain(slice => slice.Label == "Land" && slice.Value == 1m);
        response.DeckStats.ManaCurve.SpellCount.Should().Be(3);
        response.DeckStats.ManaCurve.AverageManaValue.Should().BeApproximately(5m / 3m, 0.001m);
    }

    private static CardProfile CreateCard(
        string cardId,
        string name,
        string oracleId,
        string? manaCost = null,
        decimal manaValue = 2,
        string? oracleText = null,
        string? typeLine = null,
        IReadOnlyDictionary<string, decimal>? playRateByCommander = null,
        decimal? genericColorStapleRate = null,
        decimal? saltScore = null) => new()
    {
        CardId = cardId,
        OracleId = oracleId,
        Name = name,
        ManaCost = manaCost,
        ManaValue = manaValue,
        TypeLine = typeLine ?? "Artifact",
        OracleText = oracleText,
        SaltScore = saltScore,
        GenericColorStapleRate = genericColorStapleRate,
        IsGameChanger = true,
        PlayRateByCommander = playRateByCommander ?? new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase),
        FaceProfiles = [ new CardFaceProfile("0", name, null, "Artifact", oracleText, null, true) ],
    };

    private sealed class StubCardCatalogGateway : ICardCatalogGateway
    {
        private readonly IReadOnlyDictionary<string, CardProfile> profiles;

        public StubCardCatalogGateway(IReadOnlyDictionary<string, CardProfile> profiles)
        {
            this.profiles = profiles;
        }

        public Task<IReadOnlyDictionary<string, CardProfile>> GetCardProfilesAsync(IEnumerable<string> cardIds, CancellationToken cancellationToken = default) =>
            Task.FromResult(profiles);

        public Task<IReadOnlyList<CardSearchResultContract>> SearchAsync(CardSearchQueryContract request, CancellationToken cancellationToken = default) =>
            Task.FromResult((IReadOnlyList<CardSearchResultContract>)Array.Empty<CardSearchResultContract>());

        public Task<string?> GetSnapshotVersionAsync(CancellationToken cancellationToken = default) => Task.FromResult<string?>("snapshot-v1");
    }

    private sealed class StubEdhrecClient : IEdhrecClient
    {
        public Task<CommanderThemeInsights> GetCommanderThemeInsightsAsync(CardProfile commanderProfile, CancellationToken cancellationToken = default) =>
            Task.FromResult(CommanderThemeInsights.Empty());
    }

    private sealed class StubCommanderSpellbookClient : ICommanderSpellbookClient
    {
        public Task<Domain.Analysis.ComboAnalysis> FindCombosAsync(IEnumerable<string> commanderNames, IEnumerable<string> mainDeckNames, CancellationToken cancellationToken = default) =>
            Task.FromResult(Domain.Analysis.ComboAnalysis.Empty());
    }

    private sealed class CapturingCommanderSpellbookClient : ICommanderSpellbookClient
    {
        public IReadOnlyList<string> ReceivedCommanderNames { get; private set; } = Array.Empty<string>();

        public IReadOnlyList<string> ReceivedMainDeckNames { get; private set; } = Array.Empty<string>();

        public Task<Domain.Analysis.ComboAnalysis> FindCombosAsync(IEnumerable<string> commanderNames, IEnumerable<string> mainDeckNames, CancellationToken cancellationToken = default)
        {
            ReceivedCommanderNames = commanderNames.ToArray();
            ReceivedMainDeckNames = mainDeckNames.ToArray();
            return Task.FromResult(Domain.Analysis.ComboAnalysis.Empty());
        }
    }
}
