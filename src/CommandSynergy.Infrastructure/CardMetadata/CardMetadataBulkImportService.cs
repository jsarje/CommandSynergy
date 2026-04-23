using CommandSynergy.Domain.Cards;
using CommandSynergy.Infrastructure.Scryfall;
using Microsoft.Extensions.Logging;

namespace CommandSynergy.Infrastructure.CardMetadata;

/// <summary>
/// Imports Scryfall oracle-cards bulk metadata into the local Parquet snapshot.
/// </summary>
public sealed class CardMetadataBulkImportService : ICardMetadataBulkImportService
{
    private readonly IParquetCardMetadataStore metadataStore;
    private readonly IScryfallClient scryfallClient;
    private readonly IScryfallCardMapper scryfallCardMapper;
    private readonly ILogger<CardMetadataBulkImportService> logger;

    /// <summary>
    /// Creates a bulk importer that refreshes the local Parquet snapshot from Scryfall bulk data.
    /// </summary>
    public CardMetadataBulkImportService(
        IParquetCardMetadataStore metadataStore,
        IScryfallClient scryfallClient,
        IScryfallCardMapper scryfallCardMapper,
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

        var massLandDenialIds = await scryfallClient.FetchAllByOracleTagAsync("mass-land-denial", cancellationToken).ConfigureAwait(false);

        var syncedUtc = bulkDownload.UpdatedAt ?? DateTimeOffset.UtcNow;
        var cards = bulkDownload.Cards
            .Select(document => scryfallCardMapper.MapCardProfile(document, CardMetadataSource.BulkSnapshotImport, syncedUtc, massLandDenialIds))
            .ToArray();

        await metadataStore.ReplaceSnapshotAsync(cards, cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Imported {CardCount} oracle_cards records from {DownloadUri} updated at {UpdatedAtUtc}; tagged {MassLandDenialCount} cards as mass land denial",
            cards.Length,
            bulkDownload.DownloadUri,
            syncedUtc,
            massLandDenialIds.Count);

        return new CardMetadataBulkImportResult(cards.Length, syncedUtc, bulkDownload.DownloadUri);
    }
}

/// <summary>
/// Describes the outcome of a bulk card metadata import.
/// </summary>
public sealed record CardMetadataBulkImportResult(int CardCount, DateTimeOffset ImportedAtUtc, string SourceUri);
