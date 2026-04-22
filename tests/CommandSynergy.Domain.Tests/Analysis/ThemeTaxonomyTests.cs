using CommandSynergy.Domain.Analysis;
using FluentAssertions;

namespace CommandSynergy.Domain.Tests.Analysis;

public sealed class ThemeTaxonomyTests
{
    [Fact]
    public void Default_contains_the_expected_canonical_themes()
    {
        ThemeTaxonomy.Default.Should().Contain(definition => definition.Name == "Tokens");
        ThemeTaxonomy.Default.Should().Contain(definition => definition.Name == "Reanimator");
        ThemeTaxonomy.Default.Should().Contain(definition => definition.Name == "Tribal");
        ThemeTaxonomy.Default.Count.Should().BeGreaterThanOrEqualTo(30);
    }

    [Fact]
    public void GetByName_returns_case_insensitive_matches()
    {
        var theme = ThemeTaxonomy.GetByName("tokens");

        theme.Should().NotBeNull();
        theme!.Description.Should().NotBeNullOrWhiteSpace();
    }
}