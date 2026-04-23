using System.Net.Http.Json;
using System.Text;
using CommandSynergy.Application.Contracts;

namespace CommandSynergy.Client.Services;

/// <summary>
/// Loads and caches search responses for the interactive deck workspace.
/// </summary>
public class CardSearchIndexClient(HttpClient httpClient) : ICardSearchIndexClient
{
    private readonly HttpClient httpClient = httpClient;
    private readonly Dictionary<string, CardSearchResponseContract> cachedResponses = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Searches the current server-backed card index and keeps recent responses in memory.
    /// </summary>
    public virtual async Task<CardSearchResponseContract> SearchAsync(
        string query,
        string? commanderCardId,
        IReadOnlyList<string> colors,
        CancellationToken cancellationToken = default)
    {
        var normalizedQuery = query.Trim();
        if (string.IsNullOrWhiteSpace(normalizedQuery))
        {
            return new CardSearchResponseContract
            {
                Results = Array.Empty<CardSearchResultContract>(),
            };
        }

        var cacheKey = CreateCacheKey(normalizedQuery, commanderCardId, colors);
        if (cachedResponses.TryGetValue(cacheKey, out var cachedResponse))
        {
            return cachedResponse;
        }

        var requestUri = BuildRequestUri(normalizedQuery, commanderCardId, colors);
        var response = await httpClient.GetFromJsonAsync<CardSearchResponseContract>(requestUri, cancellationToken).ConfigureAwait(false)
            ?? new CardSearchResponseContract
            {
                Results = Array.Empty<CardSearchResultContract>(),
            };

        cachedResponses[cacheKey] = response;
        return response;
    }

    /// <summary>
    /// Clears the in-memory search cache so subsequent calls reload fresh results.
    /// </summary>
    public virtual void InvalidateCache()
    {
        cachedResponses.Clear();
    }

    private static string CreateCacheKey(string query, string? commanderCardId, IReadOnlyList<string> colors)
    {
        var keyBuilder = new StringBuilder(query.Trim().ToUpperInvariant());
        keyBuilder.Append('|').Append(commanderCardId?.Trim().ToUpperInvariant());

        foreach (var color in colors.Where(static color => !string.IsNullOrWhiteSpace(color)).OrderBy(static color => color, StringComparer.OrdinalIgnoreCase))
        {
            keyBuilder.Append('|').Append(color.Trim().ToUpperInvariant());
        }

        return keyBuilder.ToString();
    }

    private static string BuildRequestUri(string query, string? commanderCardId, IReadOnlyList<string> colors)
    {
        var requestBuilder = new StringBuilder("/api/cards/search?q=")
            .Append(Uri.EscapeDataString(query));

        if (!string.IsNullOrWhiteSpace(commanderCardId))
        {
            requestBuilder.Append("&commanderCardId=")
                .Append(Uri.EscapeDataString(commanderCardId));
        }

        foreach (var color in colors.Where(static color => !string.IsNullOrWhiteSpace(color)))
        {
            requestBuilder.Append("&colors=")
                .Append(Uri.EscapeDataString(color));
        }

        return requestBuilder.ToString();
    }
}
