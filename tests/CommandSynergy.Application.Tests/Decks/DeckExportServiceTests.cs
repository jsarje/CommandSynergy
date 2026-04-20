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

    private static PortableDeckSnapshot CreateSnapshot(bool hasUnresolvedLines) => new(
        "Atraxa Blink",
        ["atraxa-praetors-voice"],
        null,
        [
            new PortableDeckEntry("atraxa-praetors-voice", "1 Atraxa, Praetors' Voice", "Atraxa, Praetors' Voice", 1, "commander", true, false, ParseConfidence.Exact),
            new PortableDeckEntry("sol-ring", "1 Sol Ring", "Sol Ring", 1, "mainboard", false, false, ParseConfidence.Exact),
        ],
        [
            new DeckSectionState("commander", "Commander", DeckSectionRole.Commander, 0, 1),
            new DeckSectionState("mainboard", "Mainboard", DeckSectionRole.Mainboard, 1, 1),
        ],
        2,
        hasUnresolvedLines);
}