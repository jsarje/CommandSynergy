using CommandSynergy.Application.Contracts;
using CommandSynergy.Domain.Analysis;
using CommandSynergy.Domain.Cards;
using CommandSynergy.Domain.Decks;

namespace CommandSynergy.Application.Analysis;

/// <summary>
/// Calculates a heuristic Commander power-level score for a submitted deck.
/// </summary>
public interface IPowerLevelCalculationService
{
    /// <summary>
    /// Calculates the heuristic power level for the supplied deck and combo analysis.
    /// </summary>
    PowerLevelAssessmentContract Calculate(
        Deck deck,
        IReadOnlyDictionary<string, CardProfile> profiles,
        ComboAnalysis comboAnalysis);
}
