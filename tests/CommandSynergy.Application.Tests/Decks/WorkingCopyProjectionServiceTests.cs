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
                new PortableDeckEntry("atraxa-praetors-voice", null, "Atraxa, Praetors' Voice", 1, "commander", true, false, ParseConfidence.Exact),
                new PortableDeckEntry("sol-ring", null, "Sol Ring", 1, "mainboard", false, false, ParseConfidence.Exact),
            ],
            [
                new DeckSectionState("commander", "Commander", DeckSectionRole.Commander, 0, 1),
                new DeckSectionState("mainboard", "Mainboard", DeckSectionRole.Mainboard, 1, 1),
            ],
            2,
            false));

        result.CommanderCardId.Should().Be("atraxa-praetors-voice");
        result.Entries.Should().HaveCount(2);
        result.Piles.Should().Contain(pile => pile.PileId == "commander");
    }
}