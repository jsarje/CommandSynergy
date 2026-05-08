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
    private const decimal HighlyOptimizedSynergyThreshold = 80.0m;
    private const int MinimumInfiniteCombosForBracketFive = 3;
    private const int MinimumGameChangersWithMassLandDenialForBracketFive = 3;
    private const int MinimumInfiniteCombosWithMassLandDenialForBracketFive = 1;
    private const int MinimumGameChangersWithComboDensityForBracketFive = 5;
    private const int MinimumTwoCardCombosForBracketFive = 2;
    private const int MinimumGameChangersForBracketFive = 6;

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

        var resolvedLevel = ResolveBracketLevel(
            gameChangerCount,
            massLandDenialCount,
            extraTurnCount,
            earlyTwoCardComboCount,
            lateTwoCardComboCount,
            infiniteComboCount,
            synergyAssessment,
            hasMeaningfulSynergy);

        if (resolvedLevel == 5)
        {
            factors.Add(new BracketFactor(
                "optimization",
                OptimizationWeight,
                "The deck shows multiple high-end optimization signals across combos and bracket-defining cards."));
        }

        var weightedAssessment = bracketEngine.Calculate(
            factors,
            bracketOptions.LevelThresholds,
            bracketOptions.MinimumBracketLevel,
            bracketOptions.MaximumBracketLevel,
            "Bracket analysis pending explanation.");

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

    private static int ResolveBracketLevel(
        int gameChangerCount,
        int massLandDenialCount,
        int extraTurnCount,
        int earlyTwoCardComboCount,
        int lateTwoCardComboCount,
        int infiniteComboCount,
        SynergyAssessment synergyAssessment,
        bool hasMeaningfulSynergy)
    {
        var effectiveScore = GetEffectiveSynergyScore(synergyAssessment);
        var isHighlyOptimized = effectiveScore >= HighlyOptimizedSynergyThreshold
            || string.Equals(synergyAssessment.QualitativeLabel, "Tuned", StringComparison.OrdinalIgnoreCase);
        var hasBracketFiveMassLandDenialSignals = massLandDenialCount > 0
            && isHighlyOptimized
            && (gameChangerCount >= MinimumGameChangersWithMassLandDenialForBracketFive
                || infiniteComboCount >= MinimumInfiniteCombosWithMassLandDenialForBracketFive);
        var hasBracketFiveComboDensitySignals = gameChangerCount >= MinimumGameChangersWithComboDensityForBracketFive
            && isHighlyOptimized
            && (earlyTwoCardComboCount + lateTwoCardComboCount >= MinimumTwoCardCombosForBracketFive);

        if (infiniteComboCount >= MinimumInfiniteCombosForBracketFive
            || hasBracketFiveMassLandDenialSignals
            || hasBracketFiveComboDensitySignals
            || (gameChangerCount >= MinimumGameChangersForBracketFive && isHighlyOptimized))
        {
            return 5;
        }

        if (massLandDenialCount > 0 || earlyTwoCardComboCount > 0 || gameChangerCount >= 3 || extraTurnCount >= 2)
        {
            return 4;
        }

        if (gameChangerCount > 0 || lateTwoCardComboCount > 0 || extraTurnCount == 1)
        {
            return 3;
        }

        return hasMeaningfulSynergy ? 2 : 1;
    }

    private static decimal GetEffectiveSynergyScore(SynergyAssessment synergyAssessment) =>
        synergyAssessment.FinalScore == 0m
            ? synergyAssessment.SynergyScore
            : synergyAssessment.FinalScore;
}
