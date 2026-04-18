namespace CommandSynergy.Domain.Cards;

/// <summary>
/// Describes the origin of a card metadata record.
/// </summary>
/// <remarks>
/// Tracking metadata source allows the application to distinguish curated bulk-imported
/// records from cards resolved through Scryfall during runtime or server-side refresh workflows.
/// </remarks>
public enum CardMetadataSource
{
    /// <summary>Metadata source is unknown or has not been assigned.</summary>
    Unknown,

    /// <summary>Populated from a curated bulk snapshot import.</summary>
    BulkSnapshotImport,

    /// <summary>
    /// Resolved through Scryfall during a user interaction.
    /// </summary>
    UserDrivenScryfallEnrichment,

    /// <summary>Populated or refreshed by a server-side administrative operation.</summary>
    ServerSideRefresh,
}
