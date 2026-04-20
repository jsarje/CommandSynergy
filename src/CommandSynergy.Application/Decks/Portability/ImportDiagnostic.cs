using CommandSynergy.Application.Contracts;

namespace CommandSynergy.Application.Decks.Portability;

public sealed record ImportDiagnostic(
    string DiagnosticId,
    DiagnosticSeverity Severity,
    string Code,
    string Message,
    int? SourceLineNumber,
    string? SourceLineText,
    string? SuggestedAction)
{
    public ImportDiagnosticContract ToContract() => new()
    {
        DiagnosticId = DiagnosticId,
        Severity = Severity.ToContractValue(),
        Code = Code,
        Message = Message,
        SourceLineNumber = SourceLineNumber,
        SourceLineText = SourceLineText,
        SuggestedAction = SuggestedAction,
    };

    public static ImportDiagnostic FromContract(ImportDiagnosticContract contract) => new(
        contract.DiagnosticId,
        DiagnosticSeverityExtensions.FromContractValue(contract.Severity),
        contract.Code,
        contract.Message,
        contract.SourceLineNumber,
        contract.SourceLineText,
        contract.SuggestedAction);
}

public enum DiagnosticSeverity
{
    Info,
    Warning,
    Error,
}

internal static class DiagnosticSeverityExtensions
{
    public static string ToContractValue(this DiagnosticSeverity severity) => severity switch
    {
        DiagnosticSeverity.Info => "info",
        DiagnosticSeverity.Warning => "warning",
        _ => "error",
    };

    public static DiagnosticSeverity FromContractValue(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "info" => DiagnosticSeverity.Info,
        "warning" => DiagnosticSeverity.Warning,
        _ => DiagnosticSeverity.Error,
    };
}