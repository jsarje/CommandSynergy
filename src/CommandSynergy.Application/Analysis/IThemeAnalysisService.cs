using CommandSynergy.Application.Abstractions;
using CommandSynergy.Domain.Analysis;
using CommandSynergy.Domain.Cards;
using CommandSynergy.Domain.Decks;

namespace CommandSynergy.Application.Analysis;

/// <summary>
/// Aggregates deck-level theme analysis and thematic scoring.
/// </summary>
public interface IThemeAnalysisService
{
    /// <summary>
    /// Analyses a deck and computes theme results plus enhanced synergy scoring.
    /// </summary>
    Task<(ThemeAnalysis Analysis, SynergyAssessment Synergy)> AnalyseAsync(
        Deck deck,
        IReadOnlyDictionary<string, CardProfile> cardProfiles,
        CommanderThemeInsights? edhrecInsights,
        CancellationToken cancellationToken = default);
}
