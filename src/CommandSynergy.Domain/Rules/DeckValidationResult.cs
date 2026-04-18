namespace CommandSynergy.Domain.Rules;

/// <summary>
/// Represents the outcome of commander-rules validation.
/// </summary>
public sealed record DeckValidationResult(bool IsValid, int DeckCardCount, IReadOnlyList<ValidationFinding> Findings);