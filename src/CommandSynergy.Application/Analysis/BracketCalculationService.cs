using CommandSynergy.Application.Configuration;
using CommandSynergy.Domain.Analysis;
using CommandSynergy.Domain.Cards;
using CommandSynergy.Domain.Decks;
using Microsoft.Extensions.Options;

namespace CommandSynergy.Application.Analysis;

/// <summary>
/// Calculates bracket factors and resolves the resulting Commander bracket.
/// </summary>
public sealed class BracketCalculationService : IBracketCalculationService
{
    private readonly IBracketEngine bracketEngine;
    private readonly IAnalysisExplanationBuilder explanationBuilder;
    private readonly BracketOptions options;

    /// <summary>
    /// Creates a bracket-calculation service.
    /// </summary>
    public BracketCalculationService(
        IBracketEngine bracketEngine,
        IAnalysisExplanationBuilder explanationBuilder,
        IOptions<BracketOptions> options)
    {
        this.bracketEngine = bracketEngine;
        this.explanationBuilder = explanationBuilder;
        this.options = options.Value;
    }

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
                    options.GameChangerWeight,
                    $"{profile.Name} is flagged by Scryfall as a game changer.",
                    entry.CardId));
            }

            if (profile.IsMassLandDenial)
            {
                factors.Add(new BracketFactor(
                    "mass-land-denial",
                    options.MassLandDenialWeight,
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
                    options.LowCostAccelerationWeight,
                    $"{profile.Name} adds low-cost acceleration pressure.",
                    entry.CardId));
            }

            if (profile.SaltScore is decimal saltScore && saltScore >= options.HighSaltThreshold)
            {
                factors.Add(new BracketFactor(
                    "pressure",
                    options.HighSaltWeight,
                    $"{profile.Name} carries a high social-friction signal.",
                    entry.CardId));
            }
        }

        var assessment = bracketEngine.Calculate(
            factors,
            options.LevelThresholds,
            options.MinimumBracketLevel,
            options.MaximumBracketLevel,
            "Bracket analysis pending explanation.");

        return assessment with
        {
            Summary = explanationBuilder.BuildBracketSummary(assessment, missingMetadataCount),
        };
    }
}
