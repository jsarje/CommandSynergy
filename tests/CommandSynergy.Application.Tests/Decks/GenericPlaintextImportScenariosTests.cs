using CommandSynergy.Application.Decks.Portability.Formats;
using FluentAssertions;

namespace CommandSynergy.Application.Tests.Decks;

public sealed class GenericPlaintextImportScenariosTests
{    

    [Fact]
    public void Generic_plaintext_parser_reports_ambiguous_lines_as_diagnostics()
    {
        var sut = new GenericPlaintextFormatProfile();

        var result = sut.Parse("# Commander\nAtraxa, Praetors' Voice");

        result.Diagnostics.Should().ContainSingle();
    }
}