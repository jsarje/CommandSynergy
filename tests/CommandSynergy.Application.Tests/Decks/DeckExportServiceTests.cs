using CommandSynergy.Application.Contracts;
using CommandSynergy.Application.Decks.Portability;
using CommandSynergy.Application.Decks.Portability.Formats;
using FluentAssertions;

namespace CommandSynergy.Application.Tests.Decks;

public sealed class DeckExportServiceTests
{
    [Fact]
    public async Task ExportAsync_renders_named_format_with_expected_ordering()
    {
        var sut = new DeckExportService(new DeckFormatRegistry([new MoxfieldTextFormatProfile(), new ManaBoxTextFormatProfile(), new GenericPlaintextFormatProfile()]));

        var result = await sut.ExportAsync(new DeckExportRequestContract
        {
            ImportedDeckId = "deck-1",
            TargetFormatId = "manabox-text",
        }, CreateSnapshot(hasUnresolvedLines: false));

        result.DocumentText.Should().Contain("[Commander]");
        result.DocumentText.Should().Contain("1 Atraxa, Praetors' Voice");
        result.Warnings.Should().BeEmpty();
    }

    [Fact]
    public async Task ExportAsync_includes_warning_when_snapshot_has_unresolved_lines()
    {
        var sut = new DeckExportService(new DeckFormatRegistry([new MoxfieldTextFormatProfile(), new ManaBoxTextFormatProfile(), new GenericPlaintextFormatProfile()]));

        var result = await sut.ExportAsync(new DeckExportRequestContract
        {
            ImportedDeckId = "deck-1",
            TargetFormatId = "moxfield-text",
        }, CreateSnapshot(hasUnresolvedLines: true));

        result.Warnings.Should().ContainSingle();
    }

    [Fact]
    public async Task ExportAsync_transforms_internal_pile_ids_back_to_external_section_identifiers()
    {
        var sut = new DeckExportService(new DeckFormatRegistry([new MoxfieldTextFormatProfile(), new ManaBoxTextFormatProfile(), new GenericPlaintextFormatProfile()]));

        var result = await sut.ExportAsync(new DeckExportRequestContract
        {
            ImportedDeckId = "deck-1",
            TargetFormatId = "manabox-text",
        }, new PortableDeckSnapshot(
            "Isshin Pressure",
            ["isshin-two-heavens-as-one"],
            null,
            [
                new PortableDeckEntry("isshin-two-heavens-as-one", "1 Isshin, Two Heavens as One", "Isshin, Two Heavens as One", null, "Legendary Creature", ["R", "W", "B"], null, "https://cards.example/isshin.jpg", false, Domain.Cards.CommanderEligibilityBasis.LegendaryCreature, 1, "command-zone", true, false, ParseConfidence.Exact),
                new PortableDeckEntry("sol-ring", "1 Sol Ring", "Sol Ring", null, "Artifact", Array.Empty<string>(), null, "https://cards.example/sol-ring.jpg", false, Domain.Cards.CommanderEligibilityBasis.Unknown, 1, "mainboard", false, false, ParseConfidence.Exact),
                new PortableDeckEntry("wear-tear", "1 Wear // Tear", "Wear // Tear", null, "Instant", ["R", "W"], null, "https://cards.example/wear-tear.jpg", true, Domain.Cards.CommanderEligibilityBasis.Unknown, 1, "maybeboard", false, false, ParseConfidence.Exact),
            ],
            [
                new DeckSectionState("command-zone", "Command Zone", DeckSectionRole.Commander, 0, 1),
                new DeckSectionState("mainboard", "Mainboard", DeckSectionRole.Mainboard, 1, 1),
                new DeckSectionState("maybeboard", "Maybeboard", DeckSectionRole.Maybeboard, 2, 1),
            ],
            3,
            false));

        result.DocumentText.Should().Contain("[Commander]");
        result.DocumentText.Should().Contain("[Mainboard]");
        result.DocumentText.Should().Contain("[Maybeboard]");
        result.DocumentText.Should().NotContain("[Command Zone]");
    }

    private static PortableDeckSnapshot CreateSnapshot(bool hasUnresolvedLines) => new(
        "Atraxa Blink",
        ["atraxa-praetors-voice"],
        null,
        [
            new PortableDeckEntry("atraxa-praetors-voice", "1 Atraxa, Praetors' Voice", "Atraxa, Praetors' Voice", null, "Legendary Creature", ["W", "U", "B", "G"], null, "https://cards.example/atraxa.jpg", false, Domain.Cards.CommanderEligibilityBasis.LegendaryCreature, 1, "commander", true, false, ParseConfidence.Exact),
            new PortableDeckEntry("sol-ring", "1 Sol Ring", "Sol Ring", null, "Artifact", Array.Empty<string>(), null, "https://cards.example/sol-ring.jpg", false, Domain.Cards.CommanderEligibilityBasis.Unknown, 1, "mainboard", false, false, ParseConfidence.Exact),
        ],
        [
            new DeckSectionState("commander", "Commander", DeckSectionRole.Commander, 0, 1),
            new DeckSectionState("mainboard", "Mainboard", DeckSectionRole.Mainboard, 1, 1),
        ],
        2,
        hasUnresolvedLines);
}