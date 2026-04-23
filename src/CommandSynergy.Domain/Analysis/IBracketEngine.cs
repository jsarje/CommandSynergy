namespace CommandSynergy.Domain.Analysis;

/// <summary>
/// Maps weighted bracket factors to an official bracket level.
/// </summary>
public interface IBracketEngine
{
    /// <summary>
    /// Calculates a bracket assessment from a set of weighted factors and threshold boundaries.
    /// </summary>
    BracketAssessment Calculate(
        IEnumerable<BracketFactor> factors,
        IReadOnlyList<decimal> levelThresholds,
        int minimumBracketLevel,
        int maximumBracketLevel,
        string summary);
}
