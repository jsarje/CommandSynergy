using CommandSynergy.Domain.Cards;
using CommandSynergy.Domain.Decks;

namespace CommandSynergy.Domain.Rules;

/// <summary>
/// Validates a commander deck against rules and metadata constraints.
/// </summary>
public interface ICommanderRules
{
    /// <summary>
    /// Validates the supplied deck against commander rules using authoritative card profiles.
    /// </summary>
    DeckValidationResult Validate(Deck deck, IReadOnlyDictionary<string, CardProfile> cardProfiles);
}
