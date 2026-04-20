using CommandSynergy.Application.Decks.Portability;
using FluentAssertions;

namespace CommandSynergy.Application.Tests.Decks;

public sealed class WorkingCopyProjectionServiceTests
{
    [Fact]
    public void CreateWorkingCopy_projects_resolved_entries_into_workspace_contract_shape()
    {
        var sut = new WorkingCopyProjectionService();

        var result = sut.CreateWorkingCopy(new PortableDeckSnapshot(
            "Atraxa Blink",
            ["atraxa-praetors-voice"],
            null,
            [
                new PortableDeckEntry("atraxa-praetors-voice", null, "Atraxa, Praetors' Voice", null, "Legendary Creature", ["W", "U", "B", "G"], null, "https://cards.example/atraxa.jpg", false, Domain.Cards.CommanderEligibilityBasis.LegendaryCreature, 1, "commander", true, false, ParseConfidence.Exact),
                new PortableDeckEntry("sol-ring", null, "Sol Ring", null, "Artifact", Array.Empty<string>(), null, "https://cards.example/sol-ring.jpg", false, Domain.Cards.CommanderEligibilityBasis.Unknown, 1, "mainboard", false, false, ParseConfidence.Exact),
            ],
            [
                new DeckSectionState("commander", "Commander", DeckSectionRole.Commander, 0, 1),
                new DeckSectionState("mainboard", "Mainboard", DeckSectionRole.Mainboard, 1, 1),
            ],
            2,
            false));

        result.CommanderCardId.Should().Be("atraxa-praetors-voice");
        result.Entries.Should().HaveCount(2);
        result.Piles.Should().Contain(pile => pile.PileId == "command-zone");
        result.Entries.Should().Contain(entry => entry.IsCommander && entry.AssignedPileId == "command-zone");
    }

    [Fact]
    public void CreateWorkingCopy_normalizes_standard_import_sections_to_internal_piles()
    {
        var sut = new WorkingCopyProjectionService();

        var result = sut.CreateWorkingCopy(new PortableDeckSnapshot(
            "Isshin Pressure",
            ["isshin-two-heavens-as-one"],
            null,
            [
                new PortableDeckEntry("isshin-two-heavens-as-one", null, "Isshin, Two Heavens as One", null, "Legendary Creature", ["R", "W", "B"], null, "https://cards.example/isshin.jpg", false, Domain.Cards.CommanderEligibilityBasis.LegendaryCreature, 1, "commander", true, false, ParseConfidence.Exact),
                new PortableDeckEntry("sol-ring", null, "Sol Ring", null, "Artifact", Array.Empty<string>(), null, "https://cards.example/sol-ring.jpg", false, Domain.Cards.CommanderEligibilityBasis.Unknown, 1, "mainboard", false, false, ParseConfidence.Exact),
                new PortableDeckEntry("wear-tear", null, "Wear // Tear", null, "Instant", ["R", "W"], null, "https://cards.example/wear-tear.jpg", true, Domain.Cards.CommanderEligibilityBasis.Unknown, 1, "maybeboard", false, false, ParseConfidence.Exact),
            ],
            [
                new DeckSectionState("commander", "Commander", DeckSectionRole.Commander, 0, 1),
                new DeckSectionState("mainboard", "Mainboard", DeckSectionRole.Mainboard, 1, 1),
                new DeckSectionState("maybeboard", "Maybeboard", DeckSectionRole.Maybeboard, 2, 1),
            ],
            3,
            false));

        result.Piles.Should().Contain(pile => pile.PileId == "command-zone" && pile.Name == "Command Zone");
        result.Piles.Should().Contain(pile => pile.PileId == "mainboard");
        result.Piles.Should().Contain(pile => pile.PileId == "maybeboard");
        result.Entries.Should().Contain(entry => entry.IsCommander && entry.AssignedPileId == "command-zone");
    }
}