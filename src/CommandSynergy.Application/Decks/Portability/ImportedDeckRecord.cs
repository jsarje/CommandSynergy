using CommandSynergy.Application.Contracts;

namespace CommandSynergy.Application.Decks.Portability;

public sealed record ImportedDeckRecord(
    string ImportedDeckId,
    string Name,
    string SourceFormatId,
    DateTimeOffset ImportedAtUtc,
    DateTimeOffset? LastOpenedUtc,
    string OriginalDocumentText,
    PortableDeckSnapshot NormalizedDeck,
    IReadOnlyList<ImportDiagnostic> Diagnostics,
    IReadOnlyList<string> ExportWarnings,
    IReadOnlyDictionary<string, string> SourceMetadata)
{
    public ImportedDeckRecordContract ToContract() => new()
    {
        ImportedDeckId = ImportedDeckId,
        Name = Name,
        SourceFormatId = SourceFormatId,
        ImportedAtUtc = ImportedAtUtc,
        LastOpenedUtc = LastOpenedUtc,
        OriginalDocumentText = OriginalDocumentText,
        NormalizedDeck = NormalizedDeck.ToContract(),
        Diagnostics = Diagnostics.Select(static diagnostic => diagnostic.ToContract()).ToArray(),
        ExportWarnings = ExportWarnings,
        SourceMetadata = new Dictionary<string, string>(SourceMetadata, StringComparer.OrdinalIgnoreCase),
    };

    public static ImportedDeckRecord FromContract(ImportedDeckRecordContract contract) => new(
        contract.ImportedDeckId,
        contract.Name,
        contract.SourceFormatId,
        contract.ImportedAtUtc,
        contract.LastOpenedUtc,
        contract.OriginalDocumentText,
        PortableDeckSnapshot.FromContract(contract.NormalizedDeck),
        contract.Diagnostics.Select(ImportDiagnostic.FromContract).ToArray(),
        contract.ExportWarnings,
        new Dictionary<string, string>(contract.SourceMetadata, StringComparer.OrdinalIgnoreCase));
}