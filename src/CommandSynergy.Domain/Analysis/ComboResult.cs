namespace CommandSynergy.Domain.Analysis;

/// <summary>
/// Represents a single combo returned by Commander Spellbook.
/// </summary>
public sealed record ComboResult(
    IReadOnlyList<string> CardNames,
    IReadOnlyList<string> Produces,
    string Steps,
    string Prerequisites);
