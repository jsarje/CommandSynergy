namespace CommandSynergy.Client.Services;

/// <summary>
/// Provides raw string access to browser local storage for large payloads.
/// </summary>
public interface ILocalStorageStringStore
{
    /// <summary>
    /// Reads a raw string value from browser local storage.
    /// </summary>
    ValueTask<string?> GetItemAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes a raw string value to browser local storage.
    /// </summary>
    ValueTask SetItemAsync(string key, string value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a value from browser local storage.
    /// </summary>
    ValueTask RemoveItemAsync(string key, CancellationToken cancellationToken = default);
}