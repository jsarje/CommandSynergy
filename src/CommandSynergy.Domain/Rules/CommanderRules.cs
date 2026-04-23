using CommandSynergy.Domain.Cards;
using CommandSynergy.Domain.Decks;

namespace CommandSynergy.Domain.Rules;

/// <summary>
/// Validates commander decks for deck size, singleton rules, color identity, companion restrictions, and multi-face metadata integrity.
/// </summary>
public sealed class CommanderRules : ICommanderRules
{
    /// <summary>
    /// Validates the supplied deck against commander rules using authoritative card profiles.
    /// </summary>
    public DeckValidationResult Validate(Deck deck, IReadOnlyDictionary<string, CardProfile> cardProfiles)
    {
        ArgumentNullException.ThrowIfNull(deck);
        ArgumentNullException.ThrowIfNull(cardProfiles);

        var findings = new List<ValidationFinding>();
        var commanderEntries = deck.Entries.Where(entry => entry.IsCommander).ToArray();
        if (commanderEntries.Length != 1)
        {
            findings.Add(new ValidationFinding("error", "commander-count", "Commander decks must have exactly one commander.", commanderEntries.Select(entry => entry.CardId).ToArray()));
        }

        if (deck.TotalCardCount != 100)
        {
            findings.Add(new ValidationFinding("error", "deck-size", "Commander decks must contain exactly 100 cards.", Array.Empty<string>()));
        }

        var commanderColors = ResolveCommanderColors(commanderEntries.SingleOrDefault(), cardProfiles, findings);

        foreach (var entry in deck.Entries)
        {
            if (!cardProfiles.TryGetValue(entry.CardId, out var cardProfile))
            {
                findings.Add(new ValidationFinding("error", "missing-card-profile", $"Card metadata is unavailable for '{entry.CardId}'.", new[] { entry.CardId }));
                continue;
            }

            if (!cardProfile.IsLegalInCommander)
            {
                findings.Add(new ValidationFinding("error", "format-legality", $"{cardProfile.Name} is not legal in Commander.", new[] { entry.CardId }));
            }

            if (cardProfile.HasMultipleFaces && cardProfile.FaceProfiles.Any(face => face.IsPrimaryFace) is false)
            {
                findings.Add(new ValidationFinding("error", "multiface-primary-face", $"{cardProfile.Name} is missing a primary face.", new[] { entry.CardId }));
            }

            if (cardProfile.FaceProfiles.Count == 1 && cardProfile.OracleText?.Contains("//", StringComparison.OrdinalIgnoreCase) == true)
            {
                findings.Add(new ValidationFinding("error", "multiface-metadata", $"{cardProfile.Name} is missing alternate face metadata.", new[] { entry.CardId }));
            }

            if (commanderColors.Count > 0 && !entry.IsCommander)
            {
                var offColorSymbols = cardProfile.ColorIdentity
                    .Where(color => !commanderColors.Contains(color, StringComparer.OrdinalIgnoreCase))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                if (offColorSymbols.Length > 0)
                {
                    findings.Add(new ValidationFinding("error", "color-identity", $"{cardProfile.Name} exceeds the commander's color identity.", new[] { entry.CardId }));
                }
            }
        }

        foreach (var duplicateGroup in deck.Entries
                     .Where(entry => cardProfiles.TryGetValue(entry.CardId, out var card) && !card.IsBasicLand && !card.AllowsMultipleCopies)
                     .GroupBy(entry => cardProfiles[entry.CardId].OracleId ?? entry.CardId, StringComparer.OrdinalIgnoreCase)
                     .Where(group => group.Sum(entry => entry.Quantity) > 1))
        {
            findings.Add(new ValidationFinding("error", "singleton", "Commander decks may not contain duplicate non-basic cards.", duplicateGroup.Select(entry => entry.CardId).ToArray()));
        }

        ValidateCompanion(deck, cardProfiles, findings);

        return new DeckValidationResult(findings.Count == 0, deck.TotalCardCount, findings);
    }

    private static HashSet<string> ResolveCommanderColors(DeckEntry? commanderEntry, IReadOnlyDictionary<string, CardProfile> cardProfiles, List<ValidationFinding> findings)
    {
        if (commanderEntry is null)
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        if (!cardProfiles.TryGetValue(commanderEntry.CardId, out var commanderProfile))
        {
            findings.Add(new ValidationFinding("error", "missing-commander-profile", "Commander metadata is unavailable.", new[] { commanderEntry.CardId }));
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        if (!commanderProfile.IsCommanderEligible)
        {
            findings.Add(new ValidationFinding("error", "commander-eligibility", $"{commanderProfile.Name} is not a legal commander.", new[] { commanderEntry.CardId }));
        }

        return new HashSet<string>(commanderProfile.ColorIdentity, StringComparer.OrdinalIgnoreCase);
    }

    private static void ValidateCompanion(Deck deck, IReadOnlyDictionary<string, CardProfile> cardProfiles, List<ValidationFinding> findings)
    {
        var companionEntry = deck.Entries.SingleOrDefault(entry => entry.IsCompanion);
        if (companionEntry is null)
        {
            return;
        }

        if (!cardProfiles.TryGetValue(companionEntry.CardId, out var companionProfile))
        {
            findings.Add(new ValidationFinding("error", "missing-companion-profile", "Companion metadata is unavailable.", new[] { companionEntry.CardId }));
            return;
        }

        if (!string.Equals(companionProfile.CompanionRequirementCode, "even-mana-value", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var invalidEntries = deck.Entries
            .Where(entry => !entry.IsCommander && !entry.IsCompanion)
            .Where(entry => cardProfiles.TryGetValue(entry.CardId, out var cardProfile) && !cardProfile.IsLand && cardProfile.ManaValue % 2 != 0)
            .Select(entry => entry.CardId)
            .ToArray();

        if (invalidEntries.Length > 0)
        {
            findings.Add(new ValidationFinding("error", "companion-restriction", "The selected companion requires every nonland card to have an even mana value.", invalidEntries));
        }
    }
}
