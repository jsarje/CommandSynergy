namespace CommandSynergy.Domain.Analysis;

/// <summary>
/// Represents a single weighted contributor to the current deck bracket.
/// </summary>
/// <param name="Category">The official or derived category for the factor.</param>
/// <param name="Weight">The numeric contribution to the total bracket weight.</param>
/// <param name="Explanation">The user-visible explanation for this factor.</param>
/// <param name="SourceCardId">The related card identifier when the factor is card-specific.</param>
public sealed record BracketFactor(string Category, decimal Weight, string Explanation, string? SourceCardId = null);