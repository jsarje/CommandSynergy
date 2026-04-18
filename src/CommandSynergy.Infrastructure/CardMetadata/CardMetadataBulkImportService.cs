using CommandSynergy.Domain.Cards;
using CommandSynergy.Infrastructure.Scryfall;
using Microsoft.Extensions.Logging;

namespace CommandSynergy.Infrastructure.CardMetadata;

/// <summary>
/// Imports Scryfall oracle-cards bulk metadata into the local Parquet snapshot.
/// </summary>
public sealed class CardMetadataBulkImportService
{
    private readonly ParquetCardMetadataStore metadataStore;
    private readonly ScryfallClient scryfallClient;
    private readonly ScryfallCardMapper scryfallCardMapper;
    private readonly ILogger<CardMetadataBulkImportService> logger;

    /// <summary>
    /// Creates a bulk importer that refreshes the local Parquet snapshot from Scryfall bulk data.
    /// </summary>
    public CardMetadataBulkImportService(
        ParquetCardMetadataStore metadataStore,
        ScryfallClient scryfallClient,
        ScryfallCardMapper scryfallCardMapper,
        ILogger<CardMetadataBulkImportService> logger)
    {
        this.metadataStore = metadataStore;
        this.scryfallClient = scryfallClient;
        this.scryfallCardMapper = scryfallCardMapper;
        this.logger = logger;
    }

    /// <summary>
    /// Downloads the Scryfall oracle-cards bulk feed and replaces the local Parquet snapshot.
    /// </summary>
    public async Task<CardMetadataBulkImportResult> ImportOracleCardsAsync(CancellationToken cancellationToken = default)
    {
        var bulkDownload = await scryfallClient.DownloadOracleCardsAsync(cancellationToken).ConfigureAwait(false);
        if (bulkDownload is null)
        {
            throw new InvalidOperationException("Scryfall oracle_cards bulk data could not be downloaded.");
        }

        var syncedUtc = bulkDownload.UpdatedAt ?? DateTimeOffset.UtcNow;
        var cards = bulkDownload.Cards
            .Select(document => scryfallCardMapper.MapCardProfile(document, CardMetadataSource.BulkSnapshotImport, syncedUtc))
            .ToArray();

        await metadataStore.ReplaceSnapshotAsync(cards, cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Imported {CardCount} oracle_cards records from {DownloadUri} updated at {UpdatedAtUtc}",
            cards.Length,
            bulkDownload.DownloadUri,
            syncedUtc);

        return new CardMetadataBulkImportResult(cards.Length, syncedUtc, bulkDownload.DownloadUri);
    }
}

/// <summary>
/// Describes the outcome of a bulk card metadata import.
/// </summary>
public sealed record CardMetadataBulkImportResult(int CardCount, DateTimeOffset ImportedAtUtc, string SourceUri);