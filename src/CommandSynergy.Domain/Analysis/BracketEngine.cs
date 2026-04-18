namespace CommandSynergy.Domain.Analysis;

/// <summary>
/// Maps weighted bracket factors to an official bracket level.
/// </summary>
public sealed class BracketEngine
{
    /// <summary>
    /// Calculates a bracket assessment from a set of weighted factors and threshold boundaries.
    /// </summary>
    public BracketAssessment Calculate(
        IEnumerable<BracketFactor> factors,
        IReadOnlyList<decimal> levelThresholds,
        int minimumBracketLevel,
        int maximumBracketLevel,
        string summary)
    {
        ArgumentNullException.ThrowIfNull(factors);
        ArgumentNullException.ThrowIfNull(levelThresholds);
        ArgumentException.ThrowIfNullOrWhiteSpace(summary);

        if (levelThresholds.Count == 0)
        {
            throw new ArgumentException("At least one bracket threshold is required.", nameof(levelThresholds));
        }

        var orderedFactors = factors
            .OrderByDescending(factor => factor.Weight)
            .ThenBy(factor => factor.Category, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var totalWeight = orderedFactors.Sum(factor => factor.Weight);
        var bracketLevel = minimumBracketLevel;

        for (var index = 0; index < levelThresholds.Count; index++)
        {
            if (totalWeight < levelThresholds[index])
            {
                break;
            }

            bracketLevel = Math.Clamp(index + 1, minimumBracketLevel, maximumBracketLevel);
        }

        return new BracketAssessment(bracketLevel, totalWeight, orderedFactors, summary, DateTimeOffset.UtcNow);
    }
}