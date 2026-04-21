using CommandSynergy.Application.Analysis;
using CommandSynergy.Domain.Cards;
using FluentAssertions;

namespace CommandSynergy.Application.Tests.Analysis;

public sealed class ThemeMatchingServiceTests
{
    private readonly ThemeMatchingService sut = new();

    [Fact]
    public void ComputeThemeSignals_detects_tokens_from_oracle_text()
    {
        var profile = CreateCard(
            "token-maker",
            "Token Maker",
            oracleText: "Create two 1/1 white Soldier creature tokens.",
            typeLine: "Sorcery");

        var result = sut.ComputeThemeSignals(profile);

        result.Should().ContainKey("Tokens");
        result["Tokens"].Should().BeGreaterThan(0m);
    }

    [Fact]
    public void ComputeThemeSignals_detects_ramp_from_keywords_and_oracle_text()
    {
        var profile = CreateCard(
            "landfall-ramp",
            "Landfall Ramp",
            oracleText: "Search your library for a basic land card, put it onto the battlefield tapped.",
            keywords: ["Landfall"],
            typeLine: "Sorcery");

        var result = sut.ComputeThemeSignals(profile);

        result.Should().ContainKey("Ramp");
        result["Ramp"].Should().BeGreaterThanOrEqualTo(0.4m);
    }

    private static CardProfile CreateCard(string cardId, string name, string? oracleText = null, IReadOnlyList<string>? keywords = null, string typeLine = "Artifact") => new()
    {
        CardId = cardId,
        Name = name,
        OracleId = cardId + "-oracle",
        ManaValue = 2,
        TypeLine = typeLine,
        OracleText = oracleText,
        Keywords = keywords ?? Array.Empty<string>(),
        FaceProfiles = [ new CardFaceProfile("0", name, null, typeLine, oracleText, null, true) ],
    };
}