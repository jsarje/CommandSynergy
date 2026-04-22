using System.Text.RegularExpressions;

namespace CommandSynergy.Domain.Analysis;

/// <summary>
/// Describes a single theme in the static Commander taxonomy.
/// </summary>
public sealed record ThemeDefinition(
    string Name,
    string Description,
    IReadOnlyList<string> KeywordPatterns,
    IReadOnlyList<Regex> OracleTextPatterns,
    Regex? TypePattern = null,
    decimal KeywordWeight = 0.4m,
    decimal OracleTextWeight = 0.25m,
    decimal TypeWeight = 0.3m,
    string SignalConfidence = "High");