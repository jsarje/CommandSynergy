using Blazored.LocalStorage;
using CommandSynergy.Application.Contracts;
using CommandSynergy.Application.Decks.Portability;

namespace CommandSynergy.Client.Services;

public sealed class ImportedDeckLibraryStore
{
    public const int MaxPersistedPayloadLength = 256 * 1024;

    private readonly ILocalStorageService localStorageService;
    private readonly ImportedDeckLibrarySerializer serializer;
    private readonly TimeProvider timeProvider;

    public ImportedDeckLibraryStore(
        ILocalStorageService localStorageService,
        ImportedDeckLibrarySerializer serializer,
        TimeProvider timeProvider)
    {
        this.localStorageService = localStorageService;
        this.serializer = serializer;
        this.timeProvider = timeProvider;
    }

    public async Task<ImportedDeckLibraryLoadResult> LoadAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var payload = await localStorageService.GetItemAsStringAsync(DeckPortabilityContract.StorageKey).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(payload))
        {
            return new ImportedDeckLibraryLoadResult(ImportedDeckLibraryDocument.Empty, false, null);
        }

        if (payload.Length > MaxPersistedPayloadLength)
        {
            await localStorageService.RemoveItemAsync(DeckPortabilityContract.StorageKey).ConfigureAwait(false);
            return new ImportedDeckLibraryLoadResult(
                ImportedDeckLibraryDocument.Empty,
                true,
                "The saved imported-deck library exceeded the safe browser payload limit and was reset.");
        }

        try
        {
            return new ImportedDeckLibraryLoadResult(serializer.Deserialize(payload), false, null);
        }
        catch
        {
            await localStorageService.RemoveItemAsync(DeckPortabilityContract.StorageKey).ConfigureAwait(false);
            return new ImportedDeckLibraryLoadResult(
                ImportedDeckLibraryDocument.Empty,
                true,
                "Saved imported-deck data was corrupted and has been safely reset.");
        }
    }

    public async Task SaveAsync(ImportedDeckLibraryDocument document, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        cancellationToken.ThrowIfCancellationRequested();

        var stamped = document with
        {
            SchemaVersion = DeckPortabilityContract.CurrentSchemaVersion,
            LastSavedUtc = timeProvider.GetUtcNow(),
        };

        var payload = serializer.Serialize(stamped);
        if (payload.Length > MaxPersistedPayloadLength)
        {
            throw new InvalidOperationException("The imported deck library exceeds the safe local storage payload limit.");
        }

        await localStorageService.SetItemAsStringAsync(DeckPortabilityContract.StorageKey, payload).ConfigureAwait(false);
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await localStorageService.RemoveItemAsync(DeckPortabilityContract.StorageKey).ConfigureAwait(false);
    }
}

public sealed record ImportedDeckLibraryLoadResult(
    ImportedDeckLibraryDocument Document,
    bool RecoveredFromInvalidState,
    string? RecoveryMessage);