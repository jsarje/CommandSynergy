using CommandSynergy.Application.Decks.Portability;

namespace CommandSynergy.Client.Services;

/// <summary>
/// Manages the interactive imported deck library state.
/// </summary>
public interface IImportedDeckLibraryState
{
    /// <summary>
    /// Gets the current imported deck library document.
    /// </summary>
    ImportedDeckLibraryDocument Library { get; }

    /// <summary>
    /// Gets whether hydration is in progress.
    /// </summary>
    bool IsHydrating { get; }

    /// <summary>
    /// Gets whether the library has been hydrated.
    /// </summary>
    bool IsHydrated { get; }

    /// <summary>
    /// Gets the latest recovery message, if one exists.
    /// </summary>
    string? RecoveryMessage { get; }

    /// <summary>
    /// Hydrates the library from browser storage.
    /// </summary>
    Task HydrateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves or replaces an imported deck.
    /// </summary>
    Task SaveImportedDeckAsync(ImportedDeckRecord record, bool setActive, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the active imported deck.
    /// </summary>
    Task SetActiveDeckAsync(string? importedDeckId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing imported deck.
    /// </summary>
    Task UpdateDeckAsync(ImportedDeckRecord record, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes an imported deck.
    /// </summary>
    Task RemoveImportedDeckAsync(string importedDeckId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears the imported deck library.
    /// </summary>
    Task ClearAsync(CancellationToken cancellationToken = default);
}
