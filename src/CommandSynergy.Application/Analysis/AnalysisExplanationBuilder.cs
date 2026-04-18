using CommandSynergy.Domain.Analysis;

namespace CommandSynergy.Application.Analysis;

/// <summary>
/// Builds user-readable summaries for bracket and synergy results.
/// </summary>
public sealed class AnalysisExplanationBuilder
{
    /// <summary>
    /// Builds the bracket explanation for the supplied assessment.
    /// </summary>
    public string BuildBracketSummary(BracketAssessment assessment, int missingMetadataCount)
    {
        ArgumentNullException.ThrowIfNull(assessment);

        var prefix = $"Bracket {assessment.BracketLevel} with total weight {assessment.TotalWeight:0.##}.";
        var topFactors = assessment.ContributingFactors.Take(2).Select(factor => factor.Explanation).ToArray();

        var factorText = topFactors.Length == 0
            ? " No weighted game changers or pressure signals were detected."
            : $" Top drivers: {string.Join("; ", topFactors)}.";

        var metadataText = missingMetadataCount == 0
            ? string.Empty
            : $" {missingMetadataCount} card(s) used partial metadata during analysis.";

        return string.Concat(prefix, factorText, metadataText);
    }

    /// <summary>
    /// Builds the synergy explanation for the supplied assessment.
    /// </summary>
    public string BuildSynergySummary(SynergyAssessment assessment, int missingMetadataCount)
    {
        ArgumentNullException.ThrowIfNull(assessment);

        var hitText = assessment.CommanderSpecificHits.Count == 0
            ? "No strong commander-specific hits were detected"
            : $"Commander-specific hits: {string.Join(", ", assessment.CommanderSpecificHits.Take(3))}";

        var stapleText = assessment.StapleOverloadIndicators.Count == 0
            ? "no staple overload indicators"
            : $"staple overload indicators: {string.Join(", ", assessment.StapleOverloadIndicators.Take(3))}";

        var metadataText = missingMetadataCount == 0
            ? string.Empty
            : $" {missingMetadataCount} card(s) used partial metadata during scoring.";

        return $"Synergy score {assessment.SynergyScore:0.#}/100. {hitText}; {stapleText}.{metadataText}";
    }
}