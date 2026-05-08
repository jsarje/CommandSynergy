namespace CommandSynergy.Domain.Analysis;

/// <summary>
/// Represents the normalized inputs required to resolve a final Commander bracket level.
/// </summary>
public sealed record BracketResolutionInput(
    IReadOnlyList<BracketFactor> Factors,
    IReadOnlyList<decimal> LevelThresholds,
    int MinimumBracketLevel,
    int MaximumBracketLevel,
    string Summary,
    int GameChangerCount,
    int MassLandDenialCount,
    int ExtraTurnCount,
    int EarlyTwoCardComboCount,
    int LateTwoCardComboCount,
    int InfiniteComboCount,
    decimal EffectiveSynergyScore,
    string QualitativeLabel,
    int CommanderSpecificHitCount,
    bool HasMeaningfulSynergy,
    bool HasAnySynergy);
