using CommandSynergy.Domain.Analysis;
using FluentAssertions;

namespace CommandSynergy.Domain.Tests.Analysis;

public sealed class BracketEngineTests
{
    private readonly BracketEngine sut = new();

    [Fact]
    public void Calculate_accumulates_factor_weights_and_sorts_factors_by_weight()
    {
        var assessment = sut.Calculate(
            [
                new BracketFactor("pressure", 1.0m, "Pressure factor"),
                new BracketFactor("game-changer", 4.0m, "Game changer factor"),
                new BracketFactor("acceleration", 2.0m, "Acceleration factor"),
            ],
            [0m, 5m, 10m, 15m, 20m],
            1,
            5,
            "Bracket summary");

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
        var assessment = sut.Calculate(
            [ new BracketFactor("game-changer", totalWeight, "Weight test") ],
            [0m, 5m, 10m, 15m, 20m],
            1,
            5,
            "Bracket summary");

        assessment.BracketLevel.Should().Be(expectedLevel);
    }
}