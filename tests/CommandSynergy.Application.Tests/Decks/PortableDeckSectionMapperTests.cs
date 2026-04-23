using CommandSynergy.Application.Decks.Portability;
using CommandSynergy.Domain.Cards;
using FluentAssertions;

namespace CommandSynergy.Application.Tests.Decks;

public sealed class PortableDeckSectionMapperTests
{
    [Fact]
    public void NormalizeImportedSnapshot_remaps_known_sections_merges_duplicates_and_infers_missing_sections()
    {
        var snapshot = new PortableDeckSnapshot(
            "Mapper Deck",
            ["commander"],
            "companion",
            [
                CreateEntry("commander", "Commander", "Commander ", 1, isCommander: true),
                CreateEntry("spell-a", "Spell A", "Deck", 2),
                CreateEntry("spell-b", "Spell B", "Mainboard", 1),
                CreateEntry("maybe-card", "Maybe Card", "Maybeboard", 1),
                CreateEntry("companion", "Companion", "Unknown", 1, isCompanion: true),
                CreateEntry("custom-card", "Custom Card", "Spice Pile", 3),
            ],
            [
                new DeckSectionState("Commander ", " Commander Zone ", DeckSectionRole.Commander, 2, 0),
                new DeckSectionState("Deck", " Deck ", DeckSectionRole.Mainboard, 1, 0),
                new DeckSectionState("Mainboard", "Mainboard", DeckSectionRole.Mainboard, 0, 0),
                new DeckSectionState("Maybeboard", "Maybe", DeckSectionRole.Maybeboard, 3, 0),
            ],
            9,
            false);

        var normalized = PortableDeckSectionMapper.NormalizeImportedSnapshot(snapshot);

        normalized.Entries.Should().Contain(entry => entry.IsCommander && entry.SectionId == PortableDeckSectionMapper.CommandZonePileId);
        normalized.Entries.Should().Contain(entry => entry.SectionId == PortableDeckSectionMapper.MainboardPileId && entry.Quantity == 2);
        normalized.Entries.Should().Contain(entry => entry.IsCompanion && entry.SectionId == PortableDeckSectionMapper.CompanionPileId);
        normalized.Entries.Should().Contain(entry => entry.SectionId == "spice-pile" && entry.Quantity == 3);

        normalized.Sections.Should().ContainSingle(section =>
            section.SectionId == PortableDeckSectionMapper.MainboardPileId
            && section.DisplayName == "Mainboard"
            && section.EntryCount == 3
            && section.SortOrder == 0);
        normalized.Sections.Should().ContainSingle(section =>
            section.SectionId == PortableDeckSectionMapper.CommandZonePileId
            && section.DisplayName == "Command Zone"
            && section.EntryCount == 1);
        normalized.Sections.Should().ContainSingle(section =>
            section.SectionId == PortableDeckSectionMapper.CompanionPileId
            && section.DisplayName == "Companion"
            && section.Role == DeckSectionRole.Companion
            && section.EntryCount == 1);
        normalized.Sections.Should().ContainSingle(section =>
            section.SectionId == "spice-pile"
            && section.DisplayName == "spice-pile"
            && section.Role == DeckSectionRole.Custom
            && section.EntryCount == 3);
    }

    [Fact]
    public void ToExternalSnapshot_converts_internal_workspace_sections_back_to_external_identifiers()
    {
        var snapshot = new PortableDeckSnapshot(
            "Mapper Deck",
            ["commander"],
            "companion",
            [
                CreateEntry("commander", "Commander", PortableDeckSectionMapper.CommandZonePileId, 1, isCommander: true),
                CreateEntry("spell-a", "Spell A", PortableDeckSectionMapper.MainboardPileId, 2),
                CreateEntry("side-card", "Side Card", PortableDeckSectionMapper.SideboardPileId, 1),
                CreateEntry("companion", "Companion", PortableDeckSectionMapper.CompanionPileId, 1, isCompanion: true),
                CreateEntry("custom-card", "Custom Card", "spice-pile", 1),
            ],
            [
                new DeckSectionState(PortableDeckSectionMapper.CommandZonePileId, "Command Zone", DeckSectionRole.Commander, 0, 1),
                new DeckSectionState(PortableDeckSectionMapper.MainboardPileId, "Mainboard", DeckSectionRole.Mainboard, 1, 2),
                new DeckSectionState(PortableDeckSectionMapper.SideboardPileId, "Sideboard", DeckSectionRole.Sideboard, 2, 1),
                new DeckSectionState(PortableDeckSectionMapper.CompanionPileId, "Companion", DeckSectionRole.Companion, 3, 1),
                new DeckSectionState("spice-pile", "Spice Pile", DeckSectionRole.Custom, 4, 1),
            ],
            6,
            false);

        var external = PortableDeckSectionMapper.ToExternalSnapshot(snapshot);

        external.Entries.Should().Contain(entry => entry.IsCommander && entry.SectionId == "commander");
        external.Entries.Should().Contain(entry => entry.SectionId == "mainboard" && entry.Quantity == 2);
        external.Entries.Should().Contain(entry => entry.SectionId == "sideboard");
        external.Entries.Should().Contain(entry => entry.IsCompanion && entry.SectionId == "companion");
        external.Entries.Should().Contain(entry => entry.SectionId == "spice-pile");

        external.Sections.Should().Contain(section => section.SectionId == "commander" && section.DisplayName == "Commander");
        external.Sections.Should().Contain(section => section.SectionId == "mainboard" && section.DisplayName == "Mainboard");
        external.Sections.Should().Contain(section => section.SectionId == "sideboard" && section.DisplayName == "Sideboard");
        external.Sections.Should().Contain(section => section.SectionId == "companion" && section.DisplayName == "Companion");
        external.Sections.Should().Contain(section => section.SectionId == "spice-pile" && section.DisplayName == "Spice Pile");
        external.Sections.Should().NotContain(section => section.SectionId == PortableDeckSectionMapper.CommandZonePileId);
    }

    [Theory]
    [InlineData("Commander", DeckSectionRole.Commander)]
    [InlineData("command-zone:", DeckSectionRole.Commander)]
    [InlineData("companion", DeckSectionRole.Companion)]
    [InlineData("sideboard", DeckSectionRole.Sideboard)]
    [InlineData("Deck", DeckSectionRole.Mainboard)]
    [InlineData(null, DeckSectionRole.Mainboard)]
    [InlineData("mystery pile", DeckSectionRole.Custom)]
    public void InferRole_maps_known_aliases_and_custom_values(string? sectionId, DeckSectionRole expectedRole)
    {
        PortableDeckSectionMapper.InferRole(sectionId).Should().Be(expectedRole);
    }

    private static PortableDeckEntry CreateEntry(
        string cardId,
        string displayName,
        string sectionId,
        int quantity,
        bool isCommander = false,
        bool isCompanion = false) =>
        new(
            cardId,
            $"{quantity} {displayName}",
            displayName,
            null,
            "Creature",
            Array.Empty<string>(),
            null,
            null,
            false,
            CommanderEligibilityBasis.Unknown,
            quantity,
            sectionId,
            isCommander,
            isCompanion,
            ParseConfidence.Exact);
}
