using CommandSynergy.Domain.Analysis;

namespace CommandSynergy.Application.Analysis;

/// <summary>
/// Builds user-readable summaries for bracket and synergy results.
/// </summary>
public sealed class AnalysisExplanationBuilder : IAnalysisExplanationBuilder
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
            ? " No bracket-defining signals were detected."
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

    /// <summary>
    /// Builds the theme-analysis summary for the supplied score and alignment state.
    /// </summary>
    public string BuildThemeSummary(decimal themeScore, decimal finalScore, string qualitativeLabel, CommanderAlignment alignment, int offThemeCardCount, int totalCardCount, bool edhrecEnhanced)
    {
        var offThemeText = offThemeCardCount == 0
            ? "Every analysed card reinforced at least one theme"
            : $"{offThemeCardCount} of {totalCardCount} cards currently read as off-theme";

        return $"Theme score {finalScore:0.#}/100 ({qualitativeLabel}). {alignment.Summary} {offThemeText}.";
    }

    /// <summary>
    /// Builds the workspace refresh summary for theme analysis.
    /// </summary>
    public string BuildThemeRefreshSummary(IReadOnlyList<DeckTheme> primaryThemes, int offThemeCardCount, int totalCardCount)
    {
        if (primaryThemes.Count == 0)
        {
            return $"No dominant themes surfaced yet across {totalCardCount} analysed cards.";
        }

        var themeNames = string.Join(", ", primaryThemes.Take(2).Select(static theme => theme.Name));
        return $"Primary themes: {themeNames}. {offThemeCardCount} card(s) are currently off-theme.";
    }

    /// <summary>
    /// Resolves a user-facing qualitative label from a 0-100 score.
    /// </summary>
    public string DetermineQualitativeLabel(decimal finalScore) => finalScore switch
    {
        >= 80m => "Tuned",
        >= 60m => "Focused",
        >= 40m => "Developing",
        >= 20m => "Unfocused",
        _ => "Pile",
    };
}
