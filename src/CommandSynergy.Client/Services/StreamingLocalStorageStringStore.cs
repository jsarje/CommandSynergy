using System.IO;
using System.Text;
using Microsoft.JSInterop;

namespace CommandSynergy.Client.Services;

/// <summary>
/// Streams raw localStorage payloads through JS interop to avoid SignalR message size limits.
/// </summary>
public sealed class StreamingLocalStorageStringStore : ILocalStorageStringStore
{
    private const long MaxStreamSize = ImportedDeckLibraryStore.MaxPersistedPayloadLength;

    private readonly IJSRuntime jsRuntime;

    /// <summary>
    /// Creates a new streaming browser storage adapter.
    /// </summary>
    public StreamingLocalStorageStringStore(IJSRuntime jsRuntime)
    {
        this.jsRuntime = jsRuntime;
    }

    /// <inheritdoc />
    public async ValueTask<string?> GetItemAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        cancellationToken.ThrowIfCancellationRequested();

        var containsKey = await jsRuntime
            .InvokeAsync<bool>("CommandSynergy.localStorage.containsKey", cancellationToken, key)
            .ConfigureAwait(false);

        if (!containsKey)
        {
            return null;
        }

        var streamReference = await jsRuntime
            .InvokeAsync<IJSStreamReference>("CommandSynergy.localStorage.getItem", cancellationToken, key)
            .ConfigureAwait(false);

        await using var stream = await streamReference
            .OpenReadStreamAsync(MaxStreamSize, cancellationToken)
            .ConfigureAwait(false);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        return await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask SetItemAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);
        cancellationToken.ThrowIfCancellationRequested();

        var buffer = Encoding.UTF8.GetBytes(value);
        using var stream = new MemoryStream(buffer, writable: false);
        using var streamReference = new DotNetStreamReference(stream);

        await jsRuntime
            .InvokeVoidAsync("CommandSynergy.localStorage.setItem", cancellationToken, key, streamReference)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public ValueTask RemoveItemAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        cancellationToken.ThrowIfCancellationRequested();

        return jsRuntime.InvokeVoidAsync("CommandSynergy.localStorage.removeItem", cancellationToken, key);
    }
}