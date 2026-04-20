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
            parsed.Entries.Select(entry => new CommandSynergy.Application.Decks.Portability.PortableDeckEntry(null, entry.OriginalLine, entry.DisplayName, null, null, Array.Empty<string>(), null, null, false, CommandSynergy.Domain.Cards.CommanderEligibilityBasis.Unknown, entry.Quantity, entry.SectionId, entry.IsCommander, entry.IsCompanion, CommandSynergy.Application.Decks.Portability.ParseConfidence.Normalized)).ToArray(),
            parsed.Sections.Select(section => new CommandSynergy.Application.Decks.Portability.DeckSectionState(section.SectionId, section.DisplayName, section.Role, section.SortOrder, 0)).ToArray(),
            parsed.Entries.Sum(static entry => entry.Quantity),
            false);

        var rendered = sut.Render(snapshot, Array.Empty<string>());

        rendered.Should().Contain("# Commander");
        rendered.Should().Contain("1 Arcane Signet");
    }
}