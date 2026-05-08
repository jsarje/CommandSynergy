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
    private const decimal LateTwoCardComboWeight = 2.0m;
    private const decimal EarlyTwoCardComboWeight = 4.0m;
    private const decimal ExtraTurnWeight = 2.0m;
    private const decimal SynergyWeight = 1.0m;
    private const decimal OptimizationWeight = 3.0m;
    private const decimal LateGameTwoCardComboManaThreshold = 7.0m;
    private const decimal MeaningfulSynergyThreshold = 60.0m;

    private readonly BracketOptions bracketOptions = options.Value;

    /// <summary>
    /// Calculates the bracket result for the supplied deck and card profiles.
    /// </summary>
    public BracketAssessment Calculate(
        Deck deck,
        IReadOnlyDictionary<string, CardProfile> cardProfiles,
        ComboAnalysis comboAnalysis,
        SynergyAssessment synergyAssessment)
    {
        ArgumentNullException.ThrowIfNull(deck);
        ArgumentNullException.ThrowIfNull(cardProfiles);
        ArgumentNullException.ThrowIfNull(comboAnalysis);
        ArgumentNullException.ThrowIfNull(synergyAssessment);

        var factors = new List<BracketFactor>();
        var missingMetadataCount = 0;
        var gameChangerCount = 0;
        var massLandDenialCount = 0;
        var extraTurnCount = 0;

        foreach (var entry in deck.Entries)
        {
            if (!cardProfiles.TryGetValue(entry.CardId, out var profile))
            {
                missingMetadataCount++;
                continue;
            }

            if (profile.IsGameChanger)
            {
                gameChangerCount++;
                factors.Add(new BracketFactor(
                    "game-changer",
                    bracketOptions.GameChangerWeight,
                    $"{profile.Name} is flagged as a game changer.",
                    entry.CardId));
            }

            if (profile.IsMassLandDenial)
            {
                massLandDenialCount++;
                factors.Add(new BracketFactor(
                    "mass-land-denial",
                    bracketOptions.MassLandDenialWeight,
                    $"{profile.Name} is a mass land denial card.",
                    entry.CardId));
            }

            if (IsExtraTurnCard(profile))
            {
                extraTurnCount++;
                factors.Add(new BracketFactor(
                    "extra-turn",
                    ExtraTurnWeight,
                    $"{profile.Name} adds an extra-turn effect.",
                    entry.CardId));
            }
        }

        var profilesByName = cardProfiles.Values
            .GroupBy(static profile => profile.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(static group => group.Key, static group => group.First(), StringComparer.OrdinalIgnoreCase);

        var earlyTwoCardComboCount = 0;
        var lateTwoCardComboCount = 0;
        var infiniteComboCount = 0;

        foreach (var combo in comboAnalysis.IncludedCombos)
        {
            if (IsInfiniteCombo(combo))
            {
                infiniteComboCount++;
            }

            var comboCardNames = combo.CardNames
                .Where(static name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (comboCardNames.Length != 2)
            {
                continue;
            }

            var isLateGameCombo = IsLateGameTwoCardCombo(comboCardNames, profilesByName);
            if (isLateGameCombo)
            {
                lateTwoCardComboCount++;
                factors.Add(new BracketFactor(
                    "late-two-card-combo",
                    LateTwoCardComboWeight,
                    $"{comboCardNames[0]} and {comboCardNames[1]} form a late-game two-card combo."));
                continue;
            }

            earlyTwoCardComboCount++;
            factors.Add(new BracketFactor(
                "early-two-card-combo",
                EarlyTwoCardComboWeight,
                $"{comboCardNames[0]} and {comboCardNames[1]} form an early two-card combo."));
        }

        var hasMeaningfulSynergy = HasMeaningfulSynergy(synergyAssessment);
        if (hasMeaningfulSynergy)
        {
            factors.Add(new BracketFactor(
                "synergy",
                SynergyWeight,
                "The deck shows clear internal synergy between its cards."));
        }

        var effectiveSynergyScore = GetEffectiveSynergyScore(synergyAssessment);
        var hasAnySynergy = HasAnySynergy(synergyAssessment);

        if (ShouldAddOptimizationFactor(
            gameChangerCount,
            massLandDenialCount,
            earlyTwoCardComboCount,
            lateTwoCardComboCount,
            infiniteComboCount,
            effectiveSynergyScore,
            synergyAssessment.QualitativeLabel))
        {
            factors.Add(new BracketFactor(
                "optimization",
                OptimizationWeight,
                "The deck shows multiple high-end optimization signals across combos and bracket-defining cards."));
        }

        var engineInput = new BracketResolutionInput(
            factors,
            bracketOptions.LevelThresholds,
            bracketOptions.MinimumBracketLevel,
            bracketOptions.MaximumBracketLevel,
            "Bracket analysis pending explanation.",
            gameChangerCount,
            massLandDenialCount,
            extraTurnCount,
            earlyTwoCardComboCount,
            lateTwoCardComboCount,
            infiniteComboCount,
            effectiveSynergyScore,
            synergyAssessment.QualitativeLabel,
            synergyAssessment.CommanderSpecificHits.Count,
            hasMeaningfulSynergy,
            hasAnySynergy);

        var weightedAssessment = bracketEngine.Calculate(engineInput);

        var assessment = weightedAssessment with
        {
            BracketLevel = Math.Clamp(weightedAssessment.BracketLevel, bracketOptions.MinimumBracketLevel, bracketOptions.MaximumBracketLevel),
        };

        return assessment with
        {
            Summary = explanationBuilder.BuildBracketSummary(assessment, missingMetadataCount),
        };
    }

    private static bool IsExtraTurnCard(CardProfile profile)
    {
        var oracleText = string.Join(
            '\n',
            profile.FaceProfiles
                .Select(static face => face.OracleText)
                .Append(profile.OracleText)
                .OfType<string>()
                .Where(static text => !string.IsNullOrWhiteSpace(text)));

        return oracleText.Contains("extra turn", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsInfiniteCombo(ComboResult combo) =>
        combo.Produces is not null && combo.Produces.Any(static produce => produce.Contains("infinite", StringComparison.OrdinalIgnoreCase))
        || (combo.Steps?.Contains("infinite", StringComparison.OrdinalIgnoreCase) ?? false)
        || (combo.Prerequisites?.Contains("infinite", StringComparison.OrdinalIgnoreCase) ?? false);

    private static bool IsLateGameTwoCardCombo(
        IReadOnlyList<string> comboCardNames,
        IReadOnlyDictionary<string, CardProfile> profilesByName)
    {
        var totalManaValue = 0m;

        foreach (var comboCardName in comboCardNames)
        {
            if (!profilesByName.TryGetValue(comboCardName, out var profile))
            {
                return false;
            }

            totalManaValue += profile.ManaValue;
        }

        return totalManaValue >= LateGameTwoCardComboManaThreshold;
    }

    private static bool HasMeaningfulSynergy(SynergyAssessment synergyAssessment)
    {
        var effectiveScore = GetEffectiveSynergyScore(synergyAssessment);

        return synergyAssessment.CommanderSpecificHits.Count > 0
            || effectiveScore >= MeaningfulSynergyThreshold
            || string.Equals(synergyAssessment.QualitativeLabel, "Focused", StringComparison.OrdinalIgnoreCase)
            || string.Equals(synergyAssessment.QualitativeLabel, "Tuned", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasAnySynergy(SynergyAssessment synergyAssessment)
    {
        var effectiveScore = GetEffectiveSynergyScore(synergyAssessment);

        // Only return false for truly random piles with no synergy signals at all
        return synergyAssessment.CommanderSpecificHits.Count > 0
            || effectiveScore > 20m
            || !string.Equals(synergyAssessment.QualitativeLabel, "Unfocused", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ShouldAddOptimizationFactor(
        int gameChangerCount,
        int massLandDenialCount,
        int earlyTwoCardComboCount,
        int lateTwoCardComboCount,
        int infiniteComboCount,
        decimal effectiveSynergyScore,
        string qualitativeLabel)
    {
        var isHighlyOptimized = effectiveSynergyScore >= 80.0m
            || string.Equals(qualitativeLabel, "Tuned", StringComparison.OrdinalIgnoreCase);
        var hasBracketFiveMassLandDenialSignals = massLandDenialCount > 0
            && isHighlyOptimized
            && (gameChangerCount >= 3 || infiniteComboCount >= 1);
        var hasBracketFiveComboDensitySignals = gameChangerCount >= 5
            && isHighlyOptimized
            && (earlyTwoCardComboCount + lateTwoCardComboCount >= 2);

        return infiniteComboCount >= 3
            || hasBracketFiveMassLandDenialSignals
            || hasBracketFiveComboDensitySignals
            || (gameChangerCount >= 6 && isHighlyOptimized);
    }

    private static decimal GetEffectiveSynergyScore(SynergyAssessment synergyAssessment) =>
        synergyAssessment.FinalScore == 0m
            ? synergyAssessment.SynergyScore
            : synergyAssessment.FinalScore;
}
