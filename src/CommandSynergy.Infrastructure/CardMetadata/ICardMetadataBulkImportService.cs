namespace CommandSynergy.Infrastructure.CardMetadata;

/// <summary>
/// Refreshes the local Parquet snapshot from Scryfall bulk data.
/// </summary>
public interface ICardMetadataBulkImportService
{
    /// <summary>
    /// Downloads the Scryfall oracle-cards bulk feed and replaces the local Parquet snapshot.
    /// </summary>
    Task<CardMetadataBulkImportResult> ImportOracleCardsAsync(CancellationToken cancellationToken = default);
}
