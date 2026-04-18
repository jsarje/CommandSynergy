namespace CommandSynergy.Domain.Analysis;

/// <summary>
/// Represents the synergy result for a commander deck snapshot.
/// </summary>
/// <param name="SynergyScore">The normalized synergy score on a 0-100 scale.</param>
/// <param name="CommanderSpecificHits">The cards that overperform for the selected commander.</param>
/// <param name="StapleOverloadIndicators">The cards that read as generic staples more than commander-specific synergies.</param>
/// <param name="Summary">The user-readable summary of the synergy result.</param>
/// <param name="CalculatedUtc">The time the assessment was produced.</param>
public sealed record SynergyAssessment(
    decimal SynergyScore,
    IReadOnlyList<string> CommanderSpecificHits,
    IReadOnlyList<string> StapleOverloadIndicators,
    string Summary,
    DateTimeOffset CalculatedUtc);