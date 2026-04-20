using CommandSynergy.Application.Decks.Portability.Formats;
using FluentAssertions;

namespace CommandSynergy.Application.Tests.Decks;

public sealed class GenericPlaintextFormatProfileTests
{
    [Fact]
    public void Generic_plaintext_profile_round_trips_core_structure()
    {
        var sut = new GenericPlaintextFormatProfile();

        var parsed = sut.Parse(DeckPortabilityFixtureLoader.Load("generic-plaintext-sample.txt"));
        var snapshot = new CommandSynergy.Application.Decks.Portability.PortableDeckSnapshot(
            parsed.DeckName ?? "Atraxa Blink",
            Array.Empty<string>(),
            null,
            parsed.Entries.Select(entry => new CommandSynergy.Application.Decks.Portability.PortableDeckEntry(null, entry.OriginalLine, entry.DisplayName, null, null, Array.Empty<string>(), null, null, false, CommandSynergy.Domain.Cards.CommanderEligibilityBasis.Unknown, entry.Quantity, entry.SectionId, entry.IsCommander, entry.IsCompanion, CommandSynergy.Application.Decks.Portability.ParseConfidence.Normalized, entry.SourceSetCode, entry.SourceCollectorNumber, entry.SourceTag)).ToArray(),
            parsed.Sections.Select(section => new CommandSynergy.Application.Decks.Portability.DeckSectionState(section.SectionId, section.DisplayName, section.Role, section.SortOrder, 0)).ToArray(),
            parsed.Entries.Sum(static entry => entry.Quantity),
            false);

        var rendered = sut.Render(snapshot, Array.Empty<string>());

        rendered.Should().Contain("# Commander");
        rendered.Should().Contain("1 Arcane Signet");
    }

    [Fact]
    public void Generic_plaintext_profile_parses_optional_set_collector_and_tag_metadata()
    {
        var sut = new GenericPlaintextFormatProfile();

        var parsed = sut.Parse("Magar of the Magic Strings (UNF) 171\n1x Aftermath Analyst (eoc) 91 [Mill]");

        parsed.Entries.Should().HaveCount(2);
        parsed.Entries[0].DisplayName.Should().Be("Magar of the Magic Strings");
        parsed.Entries[0].Quantity.Should().Be(1);
        parsed.Entries[0].SourceSetCode.Should().Be("UNF");
        parsed.Entries[0].SourceCollectorNumber.Should().Be("171");
        parsed.Entries[0].SourceTag.Should().BeNull();
        parsed.Entries[1].DisplayName.Should().Be("Aftermath Analyst");
        parsed.Entries[1].Quantity.Should().Be(1);
        parsed.Entries[1].SourceSetCode.Should().Be("eoc");
        parsed.Entries[1].SourceCollectorNumber.Should().Be("91");
        parsed.Entries[1].SourceTag.Should().Be("Mill");
    }
}