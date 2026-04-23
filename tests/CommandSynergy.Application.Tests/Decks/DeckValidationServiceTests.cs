using CommandSynergy.Application.Abstractions;
using CommandSynergy.Application.Contracts;
using CommandSynergy.Application.Decks;
using CommandSynergy.Domain.Cards;
using CommandSynergy.Domain.Decks;
using CommandSynergy.Domain.Rules;
using FluentAssertions;
using NSubstitute;

namespace CommandSynergy.Application.Tests.Decks;

public sealed class DeckValidationServiceTests
{
    [Fact]
    public async Task ValidateAsync_returns_commander_rule_findings_for_the_supplied_snapshot()
    {
        IReadOnlyDictionary<string, CardProfile> profiles = new Dictionary<string, CardProfile>(StringComparer.OrdinalIgnoreCase)
        {
            ["commander"] = CreateCard("commander", new[] { "G" }, 4, "Legendary Creature"),
            ["off-color"] = CreateCard("off-color", new[] { "R" }, 2, "Instant"),
        };
        var gateway = Substitute.For<ICardCatalogGateway>();
        gateway.GetCardProfilesAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(profiles));
        var commanderRules = Substitute.For<ICommanderRules>();
        commanderRules.Validate(Arg.Any<Deck>(), profiles)
            .Returns(new DeckValidationResult(false, 100, [new ValidationFinding("error", "color-identity", "Off-color card detected.", ["off-color"])]));

        var sut = new DeckValidationService(gateway, commanderRules);

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
        await gateway.Received(1).GetCardProfilesAsync(
            Arg.Is<IEnumerable<string>>(cardIds => cardIds.OrderBy(static cardId => cardId).SequenceEqual(new[] { "commander", "off-color" })),
            Arg.Any<CancellationToken>());
        commanderRules.Received(1).Validate(
            Arg.Is<Deck>(deck => deck.Entries.Count == 2
                && deck.Entries.Any(entry => entry.CardId == "commander" && entry.IsCommander)
                && deck.Entries.Any(entry => entry.CardId == "off-color" && entry.Quantity == 99)),
            profiles);
    }

    [Fact]
    public async Task ValidateAsync_returns_commander_eligibility_finding_for_ineligible_commanders()
    {
        IReadOnlyDictionary<string, CardProfile> profiles = new Dictionary<string, CardProfile>(StringComparer.OrdinalIgnoreCase)
        {
            ["commander"] = CreateCard("commander", new[] { "G" }, 4, "Sorcery"),
        };
        var gateway = Substitute.For<ICardCatalogGateway>();
        gateway.GetCardProfilesAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(profiles));
        var commanderRules = Substitute.For<ICommanderRules>();
        commanderRules.Validate(Arg.Any<Deck>(), profiles)
            .Returns(new DeckValidationResult(false, 1, [new ValidationFinding("error", "commander-eligibility", "Commander is not eligible.", ["commander"])]));

        var sut = new DeckValidationService(gateway, commanderRules);

        var response = await sut.ValidateAsync(new DeckSnapshotContract
        {
            CommanderCardId = "commander",
            Entries =
            [
                new DeckEntryContract { CardId = "commander", Quantity = 1, IsCommander = true },
            ],
        });

        response.Findings.Should().Contain(finding => finding.Code == "commander-eligibility");
        await gateway.Received(1).GetCardProfilesAsync(
            Arg.Is<IEnumerable<string>>(cardIds => cardIds.SequenceEqual(new[] { "commander" })),
            Arg.Any<CancellationToken>());
        commanderRules.Received(1).Validate(
            Arg.Is<Deck>(deck => deck.Entries.Count == 1 && deck.Entries[0].CardId == "commander" && deck.Entries[0].IsCommander),
            profiles);
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
}
