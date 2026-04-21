namespace CommandSynergy.Domain.Analysis;

/// <summary>
/// Represents the complete theme-analysis result for a deck snapshot.
/// </summary>
public sealed record ThemeAnalysis(
    IReadOnlyList<DeckTheme> RankedThemes,
    IReadOnlyList<DeckTheme> PrimaryThemes,
    IReadOnlyList<OffThemeCard> OffThemeCards,
    CommanderAlignment CommanderAlignment,
    int AnalysedCardCount,
    bool IsInsufficient,
    DateTimeOffset AnalysedAtUtc,
    bool UsedEdhrecFallback = false,
    string? RefreshSummary = null);

/// <summary>
/// Represents a card that did not materially contribute to a detected theme.
/// </summary>
public sealed record OffThemeCard(
    string CardId,
    string CardName,
    string Reason,
    bool MetadataUnavailable);