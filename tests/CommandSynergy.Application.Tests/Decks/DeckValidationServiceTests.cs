using CommandSynergy.Application.Abstractions;
using CommandSynergy.Application.Contracts;
using CommandSynergy.Application.Decks;
using CommandSynergy.Domain.Cards;
using CommandSynergy.Domain.Rules;
using FluentAssertions;

namespace CommandSynergy.Application.Tests.Decks;

public sealed class DeckValidationServiceTests
{
    [Fact]
    public async Task ValidateAsync_returns_commander_rule_findings_for_the_supplied_snapshot()
    {
        var gateway = new StubCardCatalogGateway(new Dictionary<string, CardProfile>(StringComparer.OrdinalIgnoreCase)
        {
            ["commander"] = CreateCard("commander", new[] { "G" }, 4, "Legendary Creature"),
            ["off-color"] = CreateCard("off-color", new[] { "R" }, 2, "Instant"),
        });

        var sut = new DeckValidationService(gateway, new CommanderRules());

        var response = await sut.ValidateAsync(new DeckSnapshotContract
        {
            CommanderCardId = "commander",
            Entries =
            [
                new DeckEntryContract { CardId = "commander", Quantity = 1, IsCommander = true },
                new DeckEntryContract { CardId = "off-color", Quantity = 99 },
            ],
        });

        response.IsValid.Should().BeFalse();
        response.Findings.Should().ContainSingle(finding => finding.Code == "color-identity");
        response.DeckCardCount.Should().Be(100);
    }

    [Fact]
    public async Task ValidateAsync_returns_commander_eligibility_finding_for_ineligible_commanders()
    {
        var gateway = new StubCardCatalogGateway(new Dictionary<string, CardProfile>(StringComparer.OrdinalIgnoreCase)
        {
            ["commander"] = CreateCard("commander", new[] { "G" }, 4, "Sorcery"),
        });

        var sut = new DeckValidationService(gateway, new CommanderRules());

        var response = await sut.ValidateAsync(new DeckSnapshotContract
        {
            CommanderCardId = "commander",
            Entries =
            [
                new DeckEntryContract { CardId = "commander", Quantity = 1, IsCommander = true },
            ],
        });

        response.Findings.Should().Contain(finding => finding.Code == "commander-eligibility");
    }

    private static CardProfile CreateCard(string cardId, IReadOnlyList<string> colors, decimal manaValue, string typeLine) => new()
    {
        CardId = cardId,
        OracleId = cardId,
        Name = cardId,
        ManaValue = manaValue,
        TypeLine = typeLine,
        ColorIdentity = colors,
        CommanderEligibilityBasis = typeLine.Contains("Legendary", StringComparison.OrdinalIgnoreCase)
            && typeLine.Contains("Creature", StringComparison.OrdinalIgnoreCase)
                ? CommanderEligibilityBasis.LegendaryCreature
                : CommanderEligibilityBasis.Unknown,
        FaceProfiles = new[] { new CardFaceProfile("0", cardId, null, typeLine, null, null, true) },
    };

    private sealed class StubCardCatalogGateway : ICardCatalogGateway
    {
        private readonly IReadOnlyDictionary<string, CardProfile> profiles;

        public StubCardCatalogGateway(IReadOnlyDictionary<string, CardProfile> profiles)
        {
            this.profiles = profiles;
        }

        public Task<IReadOnlyDictionary<string, CardProfile>> GetCardProfilesAsync(IEnumerable<string> cardIds, CancellationToken cancellationToken = default) =>
            Task.FromResult((IReadOnlyDictionary<string, CardProfile>)profiles);

        public Task<IReadOnlyList<CardSearchResultContract>> SearchAsync(CardSearchQueryContract request, CancellationToken cancellationToken = default) =>
            Task.FromResult((IReadOnlyList<CardSearchResultContract>)Array.Empty<CardSearchResultContract>());

        public Task<string?> GetSnapshotVersionAsync(CancellationToken cancellationToken = default) => Task.FromResult<string?>("test");
    }
}