using CommandSynergy.Application.Contracts;

namespace CommandSynergy.Client.Services;

/// <summary>
/// Loads and caches search responses for the interactive deck workspace.
/// </summary>
public interface ICardSearchIndexClient
{
    /// <summary>
    /// Searches the current server-backed card index and keeps recent responses in memory.
    /// </summary>
    Task<CardSearchResponseContract> SearchAsync(
        string query,
        string? commanderCardId,
        IReadOnlyList<string> colors,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears the in-memory search cache so subsequent calls reload fresh results.
    /// </summary>
    void InvalidateCache();
}
