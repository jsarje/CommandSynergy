using System.Text.Json.Serialization;
using CommandSynergy.Domain.Cards;

namespace CommandSynergy.Application.Contracts;

/// <summary>
/// Represents a deck snapshot sent between the UI and server-owned deck services.
/// </summary>
public sealed record DeckSnapshotContract
{
    [JsonPropertyName("deckId")]
    public string? DeckId { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("commanderCardId")]
    public string? CommanderCardId { get; init; }

    [JsonPropertyName("companionCardId")]
    public string? CompanionCardId { get; init; }

    [JsonPropertyName("entries")]
    public required IReadOnlyList<DeckEntryContract> Entries { get; init; }

    [JsonPropertyName("piles")]
    public IReadOnlyList<PileDefinitionContract> Piles { get; init; } = Array.Empty<PileDefinitionContract>();
}

/// <summary>
/// Represents a card entry in a submitted or persisted deck snapshot.
/// </summary>
public sealed record DeckEntryContract
{
    [JsonPropertyName("cardId")]
    public required string CardId { get; init; }

    [JsonPropertyName("quantity")]
    public required int Quantity { get; init; }

    [JsonPropertyName("assignedPileId")]
    public string? AssignedPileId { get; init; }

    [JsonPropertyName("isCommander")]
    public bool IsCommander { get; init; }

    [JsonPropertyName("isCompanion")]
    public bool IsCompanion { get; init; }
}

/// <summary>
/// Represents a user-visible pile definition in the deck workspace.
/// </summary>
public sealed record PileDefinitionContract
{
    [JsonPropertyName("pileId")]
    public required string PileId { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("sortOrder")]
    public int SortOrder { get; init; }
}

/// <summary>
/// Represents a card-search query shape used by application services.
/// </summary>
public sealed record CardSearchQueryContract
{
    public required string Query { get; init; }

    public string? CommanderCardId { get; init; }

    public IReadOnlyList<string> Colors { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Represents a compact card summary returned by search or client search artifacts.
/// </summary>
public sealed record CardSearchResultContract
{
    [JsonPropertyName("cardId")]
    public required string CardId { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("manaCost")]
    public string? ManaCost { get; init; }

    [JsonPropertyName("manaValue")]
    public decimal ManaValue { get; init; }

    [JsonPropertyName("typeLine")]
    public required string TypeLine { get; init; }

    [JsonPropertyName("colorIdentity")]
    public required IReadOnlyList<string> ColorIdentity { get; init; }

    [JsonPropertyName("saltScore")]
    public decimal? SaltScore { get; init; }

    [JsonPropertyName("imageUri")]
    public string? ImageUri { get; init; }

    [JsonPropertyName("eurPrice")]
    public decimal? EurPrice { get; init; }

    [JsonPropertyName("hasMultipleFaces")]
    public bool HasMultipleFaces { get; init; }

    [JsonPropertyName("allowsMultipleCopies")]
    public bool AllowsMultipleCopies { get; init; }

    [JsonPropertyName("commanderEligibilityBasis")]
    public CommanderEligibilityBasis CommanderEligibilityBasis { get; init; }
}

/// <summary>
/// Represents the card-search response contract.
/// </summary>
public sealed record CardSearchResponseContract
{
    [JsonPropertyName("snapshotVersion")]
    public string? SnapshotVersion { get; init; }

    [JsonPropertyName("results")]
    public required IReadOnlyList<CardSearchResultContract> Results { get; init; }
}

/// <summary>
/// Represents a validation response for a submitted deck snapshot.
/// </summary>
public sealed record DeckValidationResponseContract
{
    [JsonPropertyName("isValid")]
    public required bool IsValid { get; init; }

    [JsonPropertyName("findings")]
    public required IReadOnlyList<ValidationFindingContract> Findings { get; init; }

    [JsonPropertyName("deckCardCount")]
    public int DeckCardCount { get; init; }
}

/// <summary>
/// Represents a single structured validation finding.
/// </summary>
public sealed record ValidationFindingContract
{
    [JsonPropertyName("severity")]
    public required string Severity { get; init; }

    [JsonPropertyName("code")]
    public required string Code { get; init; }

    [JsonPropertyName("message")]
    public required string Message { get; init; }

    [JsonPropertyName("affectedCardIds")]
    public IReadOnlyList<string> AffectedCardIds { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Represents the combined analysis response for the current deck snapshot.
/// </summary>
public sealed record DeckAnalysisResponseContract
{
    [JsonPropertyName("bracket")]
    public required BracketAssessmentContract Bracket { get; init; }

    [JsonPropertyName("powerLevel")]
    public PowerLevelAssessmentContract PowerLevel { get; init; } = PowerLevelAssessmentContract.Baseline();

    [JsonPropertyName("synergy")]
    public required SynergyAssessmentContract Synergy { get; init; }

    [JsonPropertyName("themeAnalysis")]
    public ThemeAnalysisContract? ThemeAnalysis { get; init; }

    [JsonPropertyName("comboAnalysis")]
    public ComboAnalysisContract? ComboAnalysis { get; init; }

    [JsonPropertyName("deckStats")]
    public DeckStatsContract? DeckStats { get; init; }
}

/// <summary>
/// Represents a deck-suggestion request for the active workspace.
/// </summary>
public sealed record DeckSuggestionsRequestContract
{
    [JsonPropertyName("deck")]
    public required DeckSnapshotContract Deck { get; init; }

    [JsonPropertyName("filters")]
    public DeckSuggestionFiltersContract Filters { get; init; } = new();

    [JsonPropertyName("excludedCardIds")]
    public IReadOnlyList<string> ExcludedCardIds { get; init; } = Array.Empty<string>();

    [JsonPropertyName("limit")]
    public int Limit { get; init; } = 3;
}

/// <summary>
/// Represents the optional user-selected filters applied to deck suggestions.
/// </summary>
public sealed record DeckSuggestionFiltersContract
{
    [JsonPropertyName("cardType")]
    public string? CardType { get; init; }

    [JsonPropertyName("manaValue")]
    public int? ManaValue { get; init; }

    [JsonPropertyName("colorIdentity")]
    public IReadOnlyList<string> ColorIdentity { get; init; } = Array.Empty<string>();

    [JsonPropertyName("maxEurPrice")]
    public decimal? MaxEurPrice { get; init; }
}

/// <summary>
/// Represents a single suggested card with its blended recommendation scores.
/// </summary>
public sealed record DeckSuggestionCardContract
{
    [JsonPropertyName("card")]
    public required CardSearchResultContract Card { get; init; }

    [JsonPropertyName("combinedScore")]
    public decimal CombinedScore { get; init; }

    [JsonPropertyName("themeScore")]
    public decimal ThemeScore { get; init; }

    [JsonPropertyName("edhrecScore")]
    public decimal? EdhrecScore { get; init; }
}

/// <summary>
/// Represents the current deck-suggestion response payload.
/// </summary>
public sealed record DeckSuggestionsResponseContract
{
    [JsonPropertyName("commanderCardId")]
    public string? CommanderCardId { get; init; }

    [JsonPropertyName("suggestions")]
    public required IReadOnlyList<DeckSuggestionCardContract> Suggestions { get; init; }
}

/// <summary>
/// Represents the chart-ready deck statistics shown in the workspace insights panel.
/// </summary>
public sealed record DeckStatsContract
{
    [JsonPropertyName("manaValueHistogram")]
    public IReadOnlyList<DeckStatSliceContract> ManaValueHistogram { get; init; } = Array.Empty<DeckStatSliceContract>();

    [JsonPropertyName("manaCostDistribution")]
    public IReadOnlyList<DeckStatSliceContract> ManaCostDistribution { get; init; } = Array.Empty<DeckStatSliceContract>();

    [JsonPropertyName("manaGenerationDistribution")]
    public IReadOnlyList<DeckStatSliceContract> ManaGenerationDistribution { get; init; } = Array.Empty<DeckStatSliceContract>();

    [JsonPropertyName("cardTypeDistribution")]
    public IReadOnlyList<DeckStatSliceContract> CardTypeDistribution { get; init; } = Array.Empty<DeckStatSliceContract>();

    [JsonPropertyName("manaCurve")]
    public ManaCurveContract ManaCurve { get; init; } = ManaCurveContract.Empty();
}

/// <summary>
/// Represents a single labelled slice or bucket in a deck-stat chart.
/// </summary>
public sealed record DeckStatSliceContract
{
    [JsonPropertyName("label")]
    public required string Label { get; init; }

    [JsonPropertyName("value")]
    public decimal Value { get; init; }

    [JsonPropertyName("share")]
    public decimal Share { get; init; }
}

/// <summary>
/// Represents the spell-only mana-curve summary.
/// </summary>
public sealed record ManaCurveContract
{
    [JsonPropertyName("buckets")]
    public IReadOnlyList<DeckStatSliceContract> Buckets { get; init; } = Array.Empty<DeckStatSliceContract>();

    [JsonPropertyName("averageManaValue")]
    public decimal AverageManaValue { get; init; }

    [JsonPropertyName("spellCount")]
    public int SpellCount { get; init; }

    /// <summary>
    /// Returns the baseline empty mana-curve payload.
    /// </summary>
    public static ManaCurveContract Empty() => new();
}

/// <summary>
/// Represents the heuristic power-level response payload.
/// </summary>
public sealed record PowerLevelAssessmentContract
{
    [JsonPropertyName("score")]
    public decimal Score { get; init; }

    [JsonPropertyName("summary")]
    public required string Summary { get; init; }

    /// <summary>
    /// Gets a baseline power-level payload used when older cached responses omit this field.
    /// </summary>
    public static PowerLevelAssessmentContract Baseline() => new()
    {
        Score = 4.0m,
        Summary = "Baseline heuristic score before card-speed and combo adjustments.",
    };
}

/// <summary>
/// Represents the combo-analysis response payload.
/// </summary>
public sealed record ComboAnalysisContract
{
    [JsonPropertyName("includedCombos")]
    public IReadOnlyList<ComboResultContract> IncludedCombos { get; init; } = Array.Empty<ComboResultContract>();

    [JsonPropertyName("almostIncludedCombos")]
    public IReadOnlyList<ComboResultContract> AlmostIncludedCombos { get; init; } = Array.Empty<ComboResultContract>();

    [JsonPropertyName("missingOneCount")]
    public int MissingOneCount { get; init; }

    [JsonPropertyName("analysedAtUtc")]
    public DateTimeOffset AnalysedAtUtc { get; init; }
}

/// <summary>
/// Represents a single combo in the analysis response.
/// </summary>
public sealed record ComboResultContract
{
    [JsonPropertyName("cardNames")]
    public IReadOnlyList<string> CardNames { get; init; } = Array.Empty<string>();

    [JsonPropertyName("produces")]
    public IReadOnlyList<string> Produces { get; init; } = Array.Empty<string>();

    [JsonPropertyName("steps")]
    public required string Steps { get; init; }

    [JsonPropertyName("prerequisites")]
    public required string Prerequisites { get; init; }
}

/// <summary>
/// Represents a bracket assessment response payload.
/// </summary>
public sealed record BracketAssessmentContract
{
    [JsonPropertyName("level")]
    public required int Level { get; init; }

    [JsonPropertyName("totalWeight")]
    public required decimal TotalWeight { get; init; }

    [JsonPropertyName("summary")]
    public required string Summary { get; init; }

    [JsonPropertyName("factors")]
    public IReadOnlyList<BracketFactorContract> Factors { get; init; } = Array.Empty<BracketFactorContract>();
}

/// <summary>
/// Represents a single weighted factor in the bracket result.
/// </summary>
public sealed record BracketFactorContract
{
    [JsonPropertyName("sourceCardId")]
    public string? SourceCardId { get; init; }

    [JsonPropertyName("category")]
    public required string Category { get; init; }

    [JsonPropertyName("weight")]
    public required decimal Weight { get; init; }

    [JsonPropertyName("explanation")]
    public required string Explanation { get; init; }
}

/// <summary>
/// Represents the synergy assessment response payload.
/// </summary>
public sealed record SynergyAssessmentContract
{
    [JsonPropertyName("score")]
    public required decimal Score { get; init; }

    [JsonPropertyName("themeScore")]
    public decimal ThemeScore { get; init; }

    [JsonPropertyName("finalScore")]
    public decimal FinalScore { get; init; }

    [JsonPropertyName("qualitativeLabel")]
    public string QualitativeLabel { get; init; } = "Pile";

    [JsonPropertyName("edhrecEnhanced")]
    public bool EdhrecEnhanced { get; init; }

    [JsonPropertyName("summary")]
    public required string Summary { get; init; }

    [JsonPropertyName("commanderSpecificHits")]
    public IReadOnlyList<string> CommanderSpecificHits { get; init; } = Array.Empty<string>();

    [JsonPropertyName("stapleOverloadIndicators")]
    public IReadOnlyList<string> StapleOverloadIndicators { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Represents the theme-analysis response payload.
/// </summary>
public sealed record ThemeAnalysisContract
{
    [JsonPropertyName("rankedThemes")]
    public IReadOnlyList<DeckThemeContract> RankedThemes { get; init; } = Array.Empty<DeckThemeContract>();

    [JsonPropertyName("primaryThemes")]
    public IReadOnlyList<DeckThemeContract> PrimaryThemes { get; init; } = Array.Empty<DeckThemeContract>();

    [JsonPropertyName("offThemeCards")]
    public IReadOnlyList<OffThemeCardContract> OffThemeCards { get; init; } = Array.Empty<OffThemeCardContract>();

    [JsonPropertyName("commanderAlignment")]
    public required CommanderAlignmentContract CommanderAlignment { get; init; }

    [JsonPropertyName("analysedCardCount")]
    public int AnalysedCardCount { get; init; }

    [JsonPropertyName("isInsufficient")]
    public bool IsInsufficient { get; init; }

    [JsonPropertyName("analysedAtUtc")]
    public DateTimeOffset AnalysedAtUtc { get; init; }

    [JsonPropertyName("usedEdhrecFallback")]
    public bool UsedEdhrecFallback { get; init; }

    [JsonPropertyName("refreshSummary")]
    public string? RefreshSummary { get; init; }
}

/// <summary>
/// Represents a detected theme in the analysis response.
/// </summary>
public sealed record DeckThemeContract
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("description")]
    public required string Description { get; init; }

    [JsonPropertyName("strength")]
    public decimal Strength { get; init; }

    [JsonPropertyName("strengthLabel")]
    public required string StrengthLabel { get; init; }

    [JsonPropertyName("contributingCardIds")]
    public IReadOnlyList<string> ContributingCardIds { get; init; } = Array.Empty<string>();

    [JsonPropertyName("contributingCardCount")]
    public int ContributingCardCount { get; init; }

    [JsonPropertyName("contributors")]
    public IReadOnlyList<ThemeContributorContract> Contributors { get; init; } = Array.Empty<ThemeContributorContract>();

    [JsonPropertyName("signalConfidence")]
    public string SignalConfidence { get; init; } = "High";
}

/// <summary>
/// Represents a contributing card explanation for a detected theme.
/// </summary>
public sealed record ThemeContributorContract
{
    [JsonPropertyName("cardId")]
    public required string CardId { get; init; }

    [JsonPropertyName("cardName")]
    public required string CardName { get; init; }

    [JsonPropertyName("signal")]
    public decimal Signal { get; init; }

    [JsonPropertyName("reason")]
    public required string Reason { get; init; }
}

/// <summary>
/// Represents a card currently classified as off-theme.
/// </summary>
public sealed record OffThemeCardContract
{
    [JsonPropertyName("cardId")]
    public required string CardId { get; init; }

    [JsonPropertyName("cardName")]
    public required string CardName { get; init; }

    [JsonPropertyName("reason")]
    public required string Reason { get; init; }

    [JsonPropertyName("metadataUnavailable")]
    public bool MetadataUnavailable { get; init; }
}

/// <summary>
/// Represents commander alignment details in the analysis response.
/// </summary>
public sealed record CommanderAlignmentContract
{
    [JsonPropertyName("level")]
    public required string Level { get; init; }

    [JsonPropertyName("commanderTopTheme")]
    public string? CommanderTopTheme { get; init; }

    [JsonPropertyName("deckStrengthForCommanderTheme")]
    public decimal DeckStrengthForCommanderTheme { get; init; }

    [JsonPropertyName("evidenceCardIds")]
    public IReadOnlyList<string> EvidenceCardIds { get; init; } = Array.Empty<string>();

    [JsonPropertyName("summary")]
    public required string Summary { get; init; }
}

/// <summary>
/// Represents the server-generated search artifact shared with the client.
/// </summary>
public sealed record SearchIndexSnapshotContract
{
    public required string Version { get; init; }

    public required DateTimeOffset GeneratedUtc { get; init; }

    public required string SourceSnapshotId { get; init; }

    public required IReadOnlyList<CardSearchResultContract> CardSummaries { get; init; }
}
