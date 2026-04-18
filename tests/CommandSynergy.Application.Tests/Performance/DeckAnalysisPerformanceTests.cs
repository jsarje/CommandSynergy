using System.Diagnostics;
using CommandSynergy.Application.Abstractions;
using CommandSynergy.Application.Analysis;
using CommandSynergy.Application.Configuration;
using CommandSynergy.Application.Contracts;
using CommandSynergy.Domain.Cards;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace CommandSynergy.Application.Tests.Performance;

public sealed class DeckAnalysisPerformanceTests
{
    [Fact]
    public async Task AnalyzeAsync_completes_within_budget_for_full_commander_deck()
    {
        var cardProfiles = Enumerable.Range(1, 100)
            .ToDictionary(
                index => $"card-{index}",
                index => new CardProfile
                {
                    CardId = $"card-{index}",
                    OracleId = $"oracle-{index}",
                    Name = $"Card {index}",
                    ManaCost = "{2}",
                    ManaValue = 2,
                    TypeLine = index == 1 ? "Legendary Creature" : "Artifact",
                    OracleText = index % 10 == 0 ? "Add one mana of any color." : "Draw a card.",
                    SaltScore = index % 15 == 0 ? 2.5m : 0.5m,
                    PlayRateByCommander = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["oracle-1"] = 0.35m,
                    },
                    GenericColorStapleRate = 0.12m,
                    FaceProfiles = [ new CardFaceProfile("0", $"Card {index}", "{2}", "Artifact", null, null, true) ],
                },
                StringComparer.OrdinalIgnoreCase);

        var gateway = new StubCardCatalogGateway(cardProfiles);
        var options = Options.Create(new BracketOptions
        {
            LevelThresholds = [0m, 5m, 10m, 15m, 20m],
            GameChangers =
            [
                new BracketGameChangerOption
                {
                    CardId = "oracle-10",
                    Category = "game-changer",
                    Weight = 6m,
                    Explanation = "Card 10 is a configured game changer.",
                },
            ],
        });

        var sut = new DeckAnalysisService(
            gateway,
            new BracketCalculationService(GameChangerCatalog.FromOptions(options.Value.GameChangers), new Domain.Analysis.BracketEngine(), new AnalysisExplanationBuilder(), options),
            new SynergyScoringService(new AnalysisExplanationBuilder()),
            Array.Empty<IDeckAdviceService>());

        var deckSnapshot = new DeckSnapshotContract
        {
            CommanderCardId = "card-1",
            Entries = Enumerable.Range(1, 100)
                .Select(index => new DeckEntryContract
                {
                    CardId = $"card-{index}",
                    Quantity = 1,
                    IsCommander = index == 1,
                })
                .ToArray(),
        };

        var started = Stopwatch.GetTimestamp();
        var response = await sut.AnalyzeAsync(deckSnapshot);
        var elapsed = Stopwatch.GetElapsedTime(started);

        response.Bracket.Level.Should().BeGreaterThan(0);
        elapsed.Should().BeLessThan(TimeSpan.FromSeconds(2));
    }

    private sealed class StubCardCatalogGateway : ICardCatalogGateway
    {
        private readonly IReadOnlyDictionary<string, CardProfile> cardProfiles;

        public StubCardCatalogGateway(IReadOnlyDictionary<string, CardProfile> cardProfiles)
        {
            this.cardProfiles = cardProfiles;
        }

        public Task<IReadOnlyDictionary<string, CardProfile>> GetCardProfilesAsync(IEnumerable<string> cardIds, CancellationToken cancellationToken = default) =>
            Task.FromResult(cardProfiles);

        public Task<IReadOnlyList<CardSearchResultContract>> SearchAsync(CardSearchQueryContract request, CancellationToken cancellationToken = default) =>
            Task.FromResult((IReadOnlyList<CardSearchResultContract>)Array.Empty<CardSearchResultContract>());

        public Task<string?> GetSnapshotVersionAsync(CancellationToken cancellationToken = default) => Task.FromResult<string?>("perf-v1");
    }
}