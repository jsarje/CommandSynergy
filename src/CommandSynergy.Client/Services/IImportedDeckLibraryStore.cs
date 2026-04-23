using CommandSynergy.Application.Decks.Portability;

namespace CommandSynergy.Client.Services;

/// <summary>
/// Persists the imported deck library to browser storage.
/// </summary>
public interface IImportedDeckLibraryStore
{
    /// <summary>
    /// Loads the imported deck library from storage.
    /// </summary>
    Task<ImportedDeckLibraryLoadResult> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the imported deck library to storage.
    /// </summary>
    Task SaveAsync(ImportedDeckLibraryDocument document, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears the imported deck library from storage.
    /// </summary>
    Task ClearAsync(CancellationToken cancellationToken = default);
}
