using CommandSynergy.Domain.Analysis;

namespace CommandSynergy.Application.Analysis;

/// <summary>
/// Builds user-readable summaries for analysis results.
/// </summary>
public interface IAnalysisExplanationBuilder
{
    /// <summary>
    /// Builds the bracket explanation for the supplied assessment.
    /// </summary>
    string BuildBracketSummary(BracketAssessment assessment, int missingMetadataCount);

    /// <summary>
    /// Builds the synergy explanation for the supplied assessment.
    /// </summary>
    string BuildSynergySummary(SynergyAssessment assessment, int missingMetadataCount);

    /// <summary>
    /// Builds the theme-analysis summary for the supplied score and alignment state.
    /// </summary>
    string BuildThemeSummary(decimal themeScore, decimal finalScore, string qualitativeLabel, CommanderAlignment alignment, int offThemeCardCount, int totalCardCount, bool edhrecEnhanced);

    /// <summary>
    /// Builds the workspace refresh summary for theme analysis.
    /// </summary>
    string BuildThemeRefreshSummary(IReadOnlyList<DeckTheme> primaryThemes, int offThemeCardCount, int totalCardCount);

    /// <summary>
    /// Resolves a user-facing qualitative label from a 0-100 score.
    /// </summary>
    string DetermineQualitativeLabel(decimal finalScore);
}
