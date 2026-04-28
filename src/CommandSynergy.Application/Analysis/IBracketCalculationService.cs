using CommandSynergy.Domain.Analysis;
using CommandSynergy.Domain.Cards;
using CommandSynergy.Domain.Decks;

namespace CommandSynergy.Application.Analysis;

/// <summary>
/// Calculates bracket factors and resolves the resulting Commander bracket.
/// </summary>
public interface IBracketCalculationService
{
    /// <summary>
    /// Calculates the bracket result for the supplied deck and card profiles.
    /// </summary>
    BracketAssessment Calculate(
        Deck deck,
        IReadOnlyDictionary<string, CardProfile> cardProfiles,
        ComboAnalysis comboAnalysis,
        SynergyAssessment synergyAssessment);
}
