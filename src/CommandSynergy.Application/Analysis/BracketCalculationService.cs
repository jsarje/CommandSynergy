using CommandSynergy.Application.Configuration;
using CommandSynergy.Domain.Analysis;
using CommandSynergy.Domain.Cards;
using CommandSynergy.Domain.Decks;
using Microsoft.Extensions.Options;

namespace CommandSynergy.Application.Analysis;

/// <summary>
/// Calculates bracket factors and resolves the resulting Commander bracket.
/// </summary>
public sealed class BracketCalculationService(
    IBracketEngine bracketEngine,
    IAnalysisExplanationBuilder explanationBuilder,
    IOptions<BracketOptions> options) : IBracketCalculationService
{
    private readonly BracketOptions bracketOptions = options.Value;

    /// <summary>
    /// Calculates the bracket result for the supplied deck and card profiles.
    /// </summary>
    public BracketAssessment Calculate(Deck deck, IReadOnlyDictionary<string, CardProfile> cardProfiles)
    {
        ArgumentNullException.ThrowIfNull(deck);
        ArgumentNullException.ThrowIfNull(cardProfiles);

        var factors = new List<BracketFactor>();
        var missingMetadataCount = 0;

        foreach (var entry in deck.Entries)
        {
            if (!cardProfiles.TryGetValue(entry.CardId, out var profile))
            {
                missingMetadataCount++;
                continue;
            }

            if (profile.IsGameChanger)
            {
                factors.Add(new BracketFactor(
                    "game-changer",
                    bracketOptions.GameChangerWeight,
                    $"{profile.Name} is flagged by Scryfall as a game changer.",
                    entry.CardId));
            }

            if (profile.IsMassLandDenial)
            {
                factors.Add(new BracketFactor(
                    "mass-land-denial",
                    bracketOptions.MassLandDenialWeight,
                    $"{profile.Name} is a mass land denial card.",
                    entry.CardId));
            }

            if (!profile.IsLand
                && profile.ManaValue <= 2m
                && !string.IsNullOrWhiteSpace(profile.OracleText)
                && profile.OracleText.Contains("add ", StringComparison.OrdinalIgnoreCase))
            {
                factors.Add(new BracketFactor(
                    "acceleration",
                    bracketOptions.LowCostAccelerationWeight,
                    $"{profile.Name} adds low-cost acceleration pressure.",
                    entry.CardId));
            }

            if (profile.SaltScore is decimal saltScore && saltScore >= bracketOptions.HighSaltThreshold)
            {
                factors.Add(new BracketFactor(
                    "pressure",
                    bracketOptions.HighSaltWeight,
                    $"{profile.Name} carries a high social-friction signal.",
                    entry.CardId));
            }
        }

        var assessment = bracketEngine.Calculate(
            factors,
            bracketOptions.LevelThresholds,
            bracketOptions.MinimumBracketLevel,
            bracketOptions.MaximumBracketLevel,
            "Bracket analysis pending explanation.");

        return assessment with
        {
            Summary = explanationBuilder.BuildBracketSummary(assessment, missingMetadataCount),
        };
    }
}
