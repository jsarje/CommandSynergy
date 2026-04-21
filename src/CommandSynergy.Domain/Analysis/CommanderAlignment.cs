namespace CommandSynergy.Domain.Analysis;

/// <summary>
/// Represents commander-to-deck theme alignment strength.
/// </summary>
public sealed record CommanderAlignment(
    AlignmentLevel Level,
    string? CommanderTopTheme,
    decimal DeckStrengthForCommanderTheme,
    IReadOnlyList<string> EvidenceCardIds,
    string Summary);

/// <summary>
/// Represents the commander alignment level.
/// </summary>
public enum AlignmentLevel
{
    None = 0,
    Low = 1,
    Moderate = 2,
    Strong = 3,
}