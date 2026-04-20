using CommandSynergy.Application.Decks.Portability;

namespace CommandSynergy.Client.Services;

public sealed class ImportedDeckLibraryState
{
    private readonly ImportedDeckLibraryStore store;
    private readonly TimeProvider timeProvider;

    public ImportedDeckLibraryState(ImportedDeckLibraryStore store, TimeProvider timeProvider)
    {
        this.store = store;
        this.timeProvider = timeProvider;
        Library = ImportedDeckLibraryDocument.Empty;
    }

    public ImportedDeckLibraryDocument Library { get; private set; }

    public bool IsHydrating { get; private set; }

    public bool IsHydrated { get; private set; }

    public string? RecoveryMessage { get; private set; }

    public async Task HydrateAsync(CancellationToken cancellationToken = default)
    {
        IsHydrating = true;
        try
        {
            var result = await store.LoadAsync(cancellationToken).ConfigureAwait(false);
            Library = result.Document;
            RecoveryMessage = result.RecoveryMessage;
            IsHydrated = true;
        }
        finally
        {
            IsHydrating = false;
        }
    }

    public async Task SaveImportedDeckAsync(ImportedDeckRecord record, bool setActive, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        var existingDecks = Library.Decks
            .Where(deck => !string.Equals(deck.ImportedDeckId, record.ImportedDeckId, StringComparison.OrdinalIgnoreCase))
            .ToList();

        existingDecks.Add(record);

        var nextLibrary = Library with
        {
            Decks = existingDecks.OrderByDescending(static deck => deck.ImportedAtUtc).ToArray(),
            ActiveDeckId = setActive ? record.ImportedDeckId : Library.ActiveDeckId,
        };

        await PersistAsync(nextLibrary, cancellationToken).ConfigureAwait(false);
    }

    public async Task SetActiveDeckAsync(string? importedDeckId, CancellationToken cancellationToken = default)
    {
        ImportedDeckRecord[] nextDecks = Library.Decks
            .Select(deck => string.Equals(deck.ImportedDeckId, importedDeckId, StringComparison.OrdinalIgnoreCase)
                ? deck with { LastOpenedUtc = timeProvider.GetUtcNow() }
                : deck)
            .ToArray();

        await PersistAsync(Library with
        {
            ActiveDeckId = importedDeckId,
            Decks = nextDecks,
        }, cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateDeckAsync(ImportedDeckRecord record, CancellationToken cancellationToken = default)
    {
        await SaveImportedDeckAsync(record, string.Equals(record.ImportedDeckId, Library.ActiveDeckId, StringComparison.OrdinalIgnoreCase), cancellationToken).ConfigureAwait(false);
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await store.ClearAsync(cancellationToken).ConfigureAwait(false);
        Library = ImportedDeckLibraryDocument.Empty;
        RecoveryMessage = null;
        IsHydrated = true;
    }

    private async Task PersistAsync(ImportedDeckLibraryDocument nextLibrary, CancellationToken cancellationToken)
    {
        await store.SaveAsync(nextLibrary, cancellationToken).ConfigureAwait(false);
        Library = nextLibrary with { LastSavedUtc = timeProvider.GetUtcNow() };
        RecoveryMessage = null;
        IsHydrated = true;
    }
}