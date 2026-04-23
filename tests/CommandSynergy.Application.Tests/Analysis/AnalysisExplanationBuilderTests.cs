using CommandSynergy.Application.Analysis;
using CommandSynergy.Domain.Analysis;
using FluentAssertions;

namespace CommandSynergy.Application.Tests.Analysis;

public sealed class AnalysisExplanationBuilderTests
{
    private readonly AnalysisExplanationBuilder sut = new();

    [Fact]
    public void BuildBracketSummary_includes_top_two_factors_and_metadata_notice()
    {
        var assessment = new BracketAssessment(
            4,
            6.5m,
            [
                new BracketFactor("game-changer", 2.5m, "Fast mana pushed the deck upward.", "sol-ring"),
                new BracketFactor("mass-land-denial", 2.0m, "Armageddon effects add pressure.", "armageddon"),
                new BracketFactor("stax", 2.0m, "Lock pieces constrain opponents.", "winter-orb"),
            ],
            "unused",
            DateTimeOffset.UtcNow);

        var summary = sut.BuildBracketSummary(assessment, 3);

        summary.Should().Be("Bracket 4 with total weight 6.5. Top drivers: Fast mana pushed the deck upward.; Armageddon effects add pressure.. 3 card(s) used partial metadata during analysis.");
        summary.Should().NotContain("Lock pieces constrain opponents.");
    }

    [Fact]
    public void BuildBracketSummary_reports_absence_of_weighted_signals()
    {
        var assessment = new BracketAssessment(1, 0m, [], "unused", DateTimeOffset.UtcNow);

        var summary = sut.BuildBracketSummary(assessment, 0);

        summary.Should().Be("Bracket 1 with total weight 0. No weighted game changers or pressure signals were detected.");
    }

    [Fact]
    public void BuildSynergySummary_formats_hits_staples_and_metadata()
    {
        var assessment = new SynergyAssessment(
            72.4m,
            ["Smothering Tithe", "Skullclamp", "Anointed Procession", "Mondrak"],
            ["Sol Ring", "Rhystic Study", "Cyclonic Rift", "Mana Crypt"],
            "unused",
            DateTimeOffset.UtcNow);

        var summary = sut.BuildSynergySummary(assessment, 2);

        summary.Should().Be("Synergy score 72.4/100. Commander-specific hits: Smothering Tithe, Skullclamp, Anointed Procession; staple overload indicators: Sol Ring, Rhystic Study, Cyclonic Rift. 2 card(s) used partial metadata during scoring.");
        summary.Should().NotContain("Mondrak");
        summary.Should().NotContain("Mana Crypt");
    }

    [Fact]
    public void BuildSynergySummary_uses_fallback_text_when_hits_and_staples_are_empty()
    {
        var assessment = new SynergyAssessment(18m, [], [], "unused", DateTimeOffset.UtcNow);

        var summary = sut.BuildSynergySummary(assessment, 0);

        summary.Should().Be("Synergy score 18/100. No strong commander-specific hits were detected; no staple overload indicators.");
    }

    [Fact]
    public void BuildThemeSummary_and_refresh_summary_cover_edhrec_and_theme_presence_variants()
    {
        var alignment = new CommanderAlignment(AlignmentLevel.Strong, "Tokens", 0.88m, ["card-a", "card-b"], "Commander strongly supports the deck's token plan.");

        var themeSummary = sut.BuildThemeSummary(77.5m, 81m, "Tuned", alignment, 0, 24, edhrecEnhanced: true);
        var refreshSummary = sut.BuildThemeRefreshSummary(
            [
                new DeckTheme("Tokens", "Go wide", 0.92m, "High", ["card-a"], 1, [ new ThemeContributor("card-a", "Card A", 0.9m, "Token maker") ]),
                new DeckTheme("Aristocrats", "Sacrifice value", 0.61m, "Medium", ["card-b"], 1, [ new ThemeContributor("card-b", "Card B", 0.6m, "Death trigger") ]),
                new DeckTheme("Ramp", "Mana acceleration", 0.40m, "Low", ["card-c"], 1, [ new ThemeContributor("card-c", "Card C", 0.4m, "Mana source") ]),
            ],
            2,
            24);
        var emptyRefreshSummary = sut.BuildThemeRefreshSummary([], 0, 12);

        themeSummary.Should().Be("Theme score 77.5/100, final score 81/100 (Tuned). Commander strongly supports the deck's token plan. Every analysed card reinforced at least one theme. EDHREC data nudged the final score.");
        refreshSummary.Should().Be("Primary themes: Tokens, Aristocrats. 2 card(s) are currently off-theme.");
        emptyRefreshSummary.Should().Be("No dominant themes surfaced yet across 12 analysed cards.");
    }

    [Theory]
    [InlineData(80, "Tuned")]
    [InlineData(60, "Focused")]
    [InlineData(40, "Developing")]
    [InlineData(20, "Unfocused")]
    [InlineData(19.9, "Pile")]
    public void DetermineQualitativeLabel_maps_thresholds(decimal finalScore, string expectedLabel)
    {
        sut.DetermineQualitativeLabel(finalScore).Should().Be(expectedLabel);
    }
}
