using CommandSynergy.Application.Contracts;

namespace CommandSynergy.Application.Decks.Portability;

public sealed record ImportedDeckLibraryDocument(
    int SchemaVersion,
    string? ActiveDeckId,
    IReadOnlyList<ImportedDeckRecord> Decks,
    DateTimeOffset? LastSavedUtc)
{
    public static ImportedDeckLibraryDocument Empty { get; } = new(DeckPortabilityContract.CurrentSchemaVersion, null, Array.Empty<ImportedDeckRecord>(), null);

    public ImportedDeckLibraryDocumentContract ToContract() => new()
    {
        SchemaVersion = SchemaVersion,
        ActiveDeckId = ActiveDeckId,
        LastSavedUtc = LastSavedUtc,
        Decks = Decks.Select(static deck => deck.ToContract()).ToArray(),
    };

    public static ImportedDeckLibraryDocument FromContract(ImportedDeckLibraryDocumentContract contract) => new(
        contract.SchemaVersion,
        contract.ActiveDeckId,
        contract.Decks.Select(ImportedDeckRecord.FromContract).ToArray(),
        contract.LastSavedUtc);
}