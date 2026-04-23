using CommandSynergy.Domain.Analysis;
using CommandSynergy.Domain.Cards;
using CommandSynergy.Domain.Decks;

namespace CommandSynergy.Application.Analysis;

/// <summary>
/// Scores commander-specific synergy against generic staple pressure.
/// </summary>
public sealed class SynergyScoringService(IAnalysisExplanationBuilder explanationBuilder) : ISynergyScoringService
{
    private const decimal CommanderSpecificThreshold = 0.20m;
    private const decimal CommanderSpecificDelta = 0.10m;
    private const decimal StapleRateThreshold = 0.35m;

    /// <summary>
    /// Calculates the synergy result for the supplied deck and card profiles.
    /// </summary>
    public SynergyAssessment Calculate(Deck deck, IReadOnlyDictionary<string, CardProfile> cardProfiles)
    {
        ArgumentNullException.ThrowIfNull(deck);
        ArgumentNullException.ThrowIfNull(cardProfiles);

        var commanderEntry = deck.Entries.SingleOrDefault(entry => entry.IsCommander);
        if (commanderEntry is null || !cardProfiles.TryGetValue(commanderEntry.CardId, out var commanderProfile))
        {
            var emptyAssessment = new SynergyAssessment(50m, Array.Empty<string>(), Array.Empty<string>(), string.Empty, DateTimeOffset.UtcNow);
            return emptyAssessment with { Summary = explanationBuilder.BuildSynergySummary(emptyAssessment, deck.Entries.Count) };
        }

        var commanderKeys = new[] { commanderProfile.CardId, commanderProfile.OracleId }
            .Where(static key => !string.IsNullOrWhiteSpace(key))
            .Cast<string>()
            .ToArray();

        var commanderSpecificHits = new List<string>();
        var stapleOverloads = new List<string>();
        var totalDelta = 0m;
        var scoredCardCount = 0;
        var missingMetadataCount = 0;

        foreach (var entry in deck.Entries.Where(entry => !entry.IsCommander))
        {
            if (!cardProfiles.TryGetValue(entry.CardId, out var profile))
            {
                missingMetadataCount++;
                continue;
            }

            var commanderPlayRate = ResolveCommanderPlayRate(profile, commanderKeys);
            var stapleRate = profile.GenericColorStapleRate ?? 0m;
            var delta = commanderPlayRate - stapleRate;

            totalDelta += delta;
            scoredCardCount++;

            if (commanderPlayRate >= CommanderSpecificThreshold && delta >= CommanderSpecificDelta)
            {
                commanderSpecificHits.Add(profile.Name);
            }

            if (stapleRate >= StapleRateThreshold && commanderPlayRate <= stapleRate / 2m)
            {
                stapleOverloads.Add(profile.Name);
            }
        }

        var averageDelta = scoredCardCount == 0 ? 0m : totalDelta / scoredCardCount;
        var score = Math.Clamp(50m + (averageDelta * 100m), 0m, 100m);
        var assessment = new SynergyAssessment(
            decimal.Round(score, 1, MidpointRounding.AwayFromZero),
            commanderSpecificHits.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray(),
            stapleOverloads.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray(),
            string.Empty,
            DateTimeOffset.UtcNow);

        return assessment with
        {
            Summary = explanationBuilder.BuildSynergySummary(assessment, missingMetadataCount),
        };
    }

    private static decimal ResolveCommanderPlayRate(CardProfile profile, IEnumerable<string> commanderKeys)
    {
        foreach (var commanderKey in commanderKeys)
        {
            if (profile.PlayRateByCommander.TryGetValue(commanderKey, out var playRate))
            {
                return playRate;
            }
        }

        return 0m;
    }
}
