namespace CommandSynergy.Domain.Analysis;

/// <summary>
/// Represents combo-analysis results for a deck snapshot.
/// </summary>
public sealed record ComboAnalysis(
    IReadOnlyList<ComboResult> IncludedCombos,
    IReadOnlyList<ComboResult> AlmostIncludedCombos,
    int MissingOneCount,
    DateTimeOffset AnalysedAtUtc)
{
    /// <summary>
    /// Gets an empty combo-analysis result.
    /// </summary>
    public static ComboAnalysis Empty() => new([], [], 0, DateTimeOffset.UtcNow);
}
