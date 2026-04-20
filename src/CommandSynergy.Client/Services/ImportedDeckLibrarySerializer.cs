using System.Text.Json;
using CommandSynergy.Application.Contracts;
using CommandSynergy.Application.Decks.Portability;

namespace CommandSynergy.Client.Services;

public sealed class ImportedDeckLibrarySerializer
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false,
    };

    public string Serialize(ImportedDeckLibraryDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return JsonSerializer.Serialize(document.ToContract(), SerializerOptions);
    }

    public ImportedDeckLibraryDocument Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return ImportedDeckLibraryDocument.Empty;
        }

        var contract = JsonSerializer.Deserialize<ImportedDeckLibraryDocumentContract>(json, SerializerOptions)
            ?? throw new JsonException("The imported deck library payload was empty.");

        if (contract.SchemaVersion < 1)
        {
            throw new JsonException("The imported deck library schema version is not supported.");
        }

        return ImportedDeckLibraryDocument.FromContract(contract);
    }
}