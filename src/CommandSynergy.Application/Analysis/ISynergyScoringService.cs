using CommandSynergy.Domain.Analysis;
using CommandSynergy.Domain.Cards;
using CommandSynergy.Domain.Decks;

namespace CommandSynergy.Application.Analysis;

/// <summary>
/// Scores commander-specific synergy against generic staple pressure.
/// </summary>
public interface ISynergyScoringService
{
    /// <summary>
    /// Calculates the synergy result for the supplied deck and card profiles.
    /// </summary>
    SynergyAssessment Calculate(Deck deck, IReadOnlyDictionary<string, CardProfile> cardProfiles);
}
