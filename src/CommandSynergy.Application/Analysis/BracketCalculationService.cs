using CommandSynergy.Application.Configuration;
using CommandSynergy.Domain.Analysis;
using CommandSynergy.Domain.Cards;
using CommandSynergy.Domain.Decks;
using Microsoft.Extensions.Options;

namespace CommandSynergy.Application.Analysis;

/// <summary>
/// Calculates bracket factors and resolves the resulting Commander bracket.
/// </summary>
public sealed class BracketCalculationService
{
    private readonly GameChangerCatalog gameChangerCatalog;
    private readonly BracketEngine bracketEngine;
    private readonly AnalysisExplanationBuilder explanationBuilder;
    private readonly BracketOptions options;

    /// <summary>
    /// Creates a bracket-calculation service.
    /// </summary>
    public BracketCalculationService(
        GameChangerCatalog gameChangerCatalog,
        BracketEngine bracketEngine,
        AnalysisExplanationBuilder explanationBuilder,
        IOptions<BracketOptions> options)
    {
        this.gameChangerCatalog = gameChangerCatalog;
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

            if (gameChangerCatalog.TryGetDefinition(entry.CardId, profile.OracleId, out var definition) && definition is not null)
            {
                factors.Add(new BracketFactor(definition.Category, definition.Weight, definition.Explanation, entry.CardId));
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