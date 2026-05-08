using CommandSynergy.Domain.Analysis;
using FluentAssertions;

namespace CommandSynergy.Domain.Tests.Analysis;

public sealed class BracketEngineTests
{
    private readonly BracketEngine sut = new();

    [Fact]
    public void Calculate_accumulates_factor_weights_and_sorts_factors_by_weight()
    {
        var assessment = sut.Calculate(CreateInput(
            [
                new BracketFactor("pressure", 1.0m, "Pressure factor"),
                new BracketFactor("game-changer", 4.0m, "Game changer factor"),
                new BracketFactor("acceleration", 2.0m, "Acceleration factor"),
            ]));

        assessment.TotalWeight.Should().Be(7.0m);
        assessment.ContributingFactors.Select(factor => factor.Category).Should().ContainInOrder("game-changer", "acceleration", "pressure");
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(5, 2)]
    [InlineData(10, 3)]
    [InlineData(15, 4)]
    [InlineData(20, 5)]
    [InlineData(30, 5)]
    public void Calculate_maps_total_weight_to_the_expected_level_boundaries(decimal totalWeight, int expectedLevel)
    {
        var assessment = sut.Calculate(CreateInput(
            [ new BracketFactor("game-changer", totalWeight, "Weight test") ],
            effectiveSynergyScore: 0m,
            qualitativeLabel: "Unfocused",
            commanderSpecificHitCount: 0,
            hasMeaningfulSynergy: false,
            hasAnySynergy: false));

        assessment.BracketLevel.Should().Be(expectedLevel);
    }

    [Fact]
    public void Calculate_uses_explicit_signal_rules_as_the_baseline_before_threshold_escalation()
    {
        var assessment = sut.Calculate(CreateInput(
            [],
            lateTwoCardComboCount: 1,
            levelThresholds: [100m, 200m, 300m, 400m, 500m]));

        assessment.BracketLevel.Should().Be(3);
    }

    [Fact]
    public void Calculate_returns_bracket_one_only_when_no_synergy_or_optimization_signals_exist()
    {
        var assessment = sut.Calculate(CreateInput(
            [],
            effectiveSynergyScore: 0m,
            qualitativeLabel: "Unfocused",
            hasAnySynergy: false,
            hasMeaningfulSynergy: false,
            commanderSpecificHitCount: 0,
            levelThresholds: [100m, 200m, 300m, 400m, 500m]));

        assessment.BracketLevel.Should().Be(1);
    }

    private static BracketResolutionInput CreateInput(
        IReadOnlyList<BracketFactor> factors,
        IReadOnlyList<decimal>? levelThresholds = null,
        int gameChangerCount = 0,
        int massLandDenialCount = 0,
        int extraTurnCount = 0,
        int earlyTwoCardComboCount = 0,
        int lateTwoCardComboCount = 0,
        int infiniteComboCount = 0,
        decimal effectiveSynergyScore = 35m,
        string qualitativeLabel = "Developing",
        int commanderSpecificHitCount = 1,
        bool hasMeaningfulSynergy = true,
        bool hasAnySynergy = true) => new(
            factors,
            levelThresholds ?? [0m, 5m, 10m, 15m, 20m],
            1,
            5,
            "Bracket summary",
            gameChangerCount,
            massLandDenialCount,
            extraTurnCount,
            earlyTwoCardComboCount,
            lateTwoCardComboCount,
            infiniteComboCount,
            effectiveSynergyScore,
            qualitativeLabel,
            commanderSpecificHitCount,
            hasMeaningfulSynergy,
            hasAnySynergy);
}