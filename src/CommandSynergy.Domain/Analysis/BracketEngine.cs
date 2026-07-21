namespace CommandSynergy.Domain.Analysis;

/// <summary>
/// Maps weighted bracket factors to an official bracket level.
/// </summary>
public sealed class BracketEngine : IBracketEngine
{
    private const decimal HighlyOptimizedSynergyThreshold = 80.0m;
    private const int MinimumInfiniteCombosForBracketFive = 3;
    private const int MinimumGameChangersWithMassLandDenialForBracketFive = 3;
    private const int MinimumInfiniteCombosWithMassLandDenialForBracketFive = 1;
    private const int MinimumGameChangersWithComboDensityForBracketFive = 5;
    private const int MinimumTwoCardCombosForBracketFive = 2;
    private const int MinimumGameChangersForBracketFive = 6;

    /// <summary>
    /// Calculates a bracket assessment from the supplied normalized bracket inputs.
    /// </summary>
    public BracketAssessment Calculate(BracketResolutionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(input.Factors);
        ArgumentNullException.ThrowIfNull(input.LevelThresholds);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.Summary);

        if (input.LevelThresholds.Count == 0)
        {
            throw new ArgumentException("At least one bracket threshold is required.", nameof(input.LevelThresholds));
        }

        var orderedFactors = input.Factors
            .OrderByDescending(factor => factor.Weight)
            .ThenBy(factor => factor.Category, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var totalWeight = orderedFactors.Sum(factor => factor.Weight);
        var bracketLevel = ResolveBracketLevel(input);

        for (var index = 0; index < input.LevelThresholds.Count; index++)
        {
            if (totalWeight < input.LevelThresholds[index])
            {
                break;
            }

            bracketLevel = Math.Max(bracketLevel, Math.Clamp(index + 1, input.MinimumBracketLevel, input.MaximumBracketLevel));
        }

        return new BracketAssessment(
            Math.Clamp(bracketLevel, input.MinimumBracketLevel, input.MaximumBracketLevel),
            totalWeight,
            orderedFactors,
            input.Summary,
            DateTimeOffset.UtcNow);
    }

    private static int ResolveBracketLevel(BracketResolutionInput input)
    {
        var isHighlyOptimized = input.EffectiveSynergyScore >= HighlyOptimizedSynergyThreshold
            || string.Equals(input.QualitativeLabel, "Tuned", StringComparison.OrdinalIgnoreCase);
        var hasBracketFiveMassLandDenialSignals = input.MassLandDenialCount > 0
            && isHighlyOptimized
            && (input.GameChangerCount >= MinimumGameChangersWithMassLandDenialForBracketFive
                || input.InfiniteComboCount >= MinimumInfiniteCombosWithMassLandDenialForBracketFive);
        var hasBracketFiveComboDensitySignals = input.GameChangerCount >= MinimumGameChangersWithComboDensityForBracketFive
            && isHighlyOptimized
            && (input.EarlyTwoCardComboCount + input.LateTwoCardComboCount >= MinimumTwoCardCombosForBracketFive);

        if (input.InfiniteComboCount >= MinimumInfiniteCombosForBracketFive
            || hasBracketFiveMassLandDenialSignals
            || hasBracketFiveComboDensitySignals
            || (input.GameChangerCount >= MinimumGameChangersForBracketFive && isHighlyOptimized))
        {
            return 5;
        }

        if (input.MassLandDenialCount > 0 || input.EarlyTwoCardComboCount > 0 || input.GameChangerCount >= 3 || input.ExtraTurnCount >= 2)
        {
            return 4;
        }

        if (input.GameChangerCount > 0 || input.LateTwoCardComboCount > 0 || input.ExtraTurnCount == 1)
        {
            return 3;
        }

        if (input.HasAnySynergy || input.HasMeaningfulSynergy || input.CommanderSpecificHitCount > 0)
        {
            return 2;
        }

        return 1;
    }
}
