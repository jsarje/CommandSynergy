using CommandSynergy.Application.Decks.Portability;

namespace CommandSynergy.Client.Services;

/// <summary>
/// Serializes and deserializes the imported deck library payload.
/// </summary>
public interface IImportedDeckLibrarySerializer
{
    /// <summary>
    /// Serializes a library document for browser storage.
    /// </summary>
    string Serialize(ImportedDeckLibraryDocument document);

    /// <summary>
    /// Deserializes a browser payload into a library document.
    /// </summary>
    ImportedDeckLibraryDocument Deserialize(string json);
}
