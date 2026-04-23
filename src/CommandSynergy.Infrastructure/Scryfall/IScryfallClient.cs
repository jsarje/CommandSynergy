namespace CommandSynergy.Infrastructure.Scryfall;

/// <summary>
/// Wraps outbound Scryfall requests behind a typed client with validated inputs.
/// </summary>
public interface IScryfallClient
{
    /// <summary>
    /// Searches Scryfall cards using the supplied query string.
    /// </summary>
    Task<ScryfallSearchResponse> SearchCardsAsync(string query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches all card ids matching a Scryfall oracle tag.
    /// </summary>
    Task<IReadOnlySet<string>> FetchAllByOracleTagAsync(string tag, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads card-name suggestions for a partial search term.
    /// </summary>
    Task<IReadOnlyList<string>> AutocompleteCardNamesAsync(string query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Requests an exact card match from Scryfall by card name.
    /// </summary>
    Task<ScryfallCardDocument?> GetNamedCardAsync(string exactName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a card document by Scryfall card identifier.
    /// </summary>
    Task<ScryfallCardDocument?> GetCardByIdAsync(string cardId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a card document using a Scryfall id when present, otherwise falls back to exact-name lookup.
    /// </summary>
    Task<ScryfallCardDocument?> GetCardAsync(string cardIdOrName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads the current oracle-cards bulk dataset from Scryfall.
    /// </summary>
    Task<ScryfallBulkDownloadResult?> DownloadOracleCardsAsync(CancellationToken cancellationToken = default);
}
