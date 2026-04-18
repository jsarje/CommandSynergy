namespace CommandSynergy.Domain.Cards;

/// <summary>
/// Describes the origin of a card's locally stored metadata record.
/// </summary>
/// <remarks>
/// Tracking metadata source allows the application to distinguish curated bulk-imported
/// records from cards resolved on-demand through Scryfall fallback during user interactions.
/// </remarks>
public enum CardMetadataSource
{
    /// <summary>Metadata source is unknown or has not been assigned.</summary>
    Unknown,

    /// <summary>Populated from a curated bulk snapshot import.</summary>
    BulkSnapshotImport,

    /// <summary>
    /// Populated by write-through enrichment from a Scryfall fallback triggered during
    /// a user interaction. These records reduce future external lookups for the same card.
    /// </summary>
    UserDrivenScryfallEnrichment,

    /// <summary>Populated or refreshed by a server-side administrative operation.</summary>
    ServerSideRefresh,
}
