using CommandSynergy.Application.Decks.Portability.Formats;
using FluentAssertions;

namespace CommandSynergy.Application.Tests.Decks;

public sealed class GenericPlaintextFormatProfileTests
{    

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