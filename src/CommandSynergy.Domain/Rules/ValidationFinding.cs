namespace CommandSynergy.Domain.Rules;

/// <summary>
/// Represents a structured commander-rules finding.
/// </summary>
public sealed record ValidationFinding(string Severity, string Code, string Message, IReadOnlyList<string> AffectedCardIds);