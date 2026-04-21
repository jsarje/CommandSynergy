namespace CommandSynergy.Domain.Analysis;

/// <summary>
/// Represents a deck-level theme result.
/// </summary>
public sealed record DeckTheme(
    string Name,
    string Description,
    decimal Strength,
    string StrengthLabel,
    IReadOnlyList<string> ContributingCardIds,
    int ContributingCardCount,
    IReadOnlyList<ThemeContributor> Contributors,
    string SignalConfidence = "High");

/// <summary>
/// Represents a contributing card and explanation for a detected theme.
/// </summary>
public sealed record ThemeContributor(
    string CardId,
    string CardName,
    decimal Signal,
    string Reason);