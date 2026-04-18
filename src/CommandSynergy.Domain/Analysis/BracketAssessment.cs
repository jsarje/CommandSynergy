namespace CommandSynergy.Domain.Analysis;

/// <summary>
/// Represents the calculated bracket result for a deck snapshot.
/// </summary>
/// <param name="BracketLevel">The resulting Commander bracket from 1 through 5.</param>
/// <param name="TotalWeight">The total weighted score accumulated across all factors.</param>
/// <param name="ContributingFactors">The ordered factor list that drove the result.</param>
/// <param name="Summary">The user-readable summary of the bracket result.</param>
/// <param name="CalculatedUtc">The time the assessment was produced.</param>
public sealed record BracketAssessment(
    int BracketLevel,
    decimal TotalWeight,
    IReadOnlyList<BracketFactor> ContributingFactors,
    string Summary,
    DateTimeOffset CalculatedUtc);