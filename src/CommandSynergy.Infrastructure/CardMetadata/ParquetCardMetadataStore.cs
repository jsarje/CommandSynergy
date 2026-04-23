using CommandSynergy.Application.Configuration;
using CommandSynergy.Domain.Cards;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Parquet.Serialization;

namespace CommandSynergy.Infrastructure.CardMetadata;

/// <summary>
/// Loads and persists the authoritative local card metadata snapshot from the configured Parquet location.
/// </summary>
public sealed class ParquetCardMetadataStore(IOptions<CardMetadataOptions> options, ILogger<ParquetCardMetadataStore> logger) : IParquetCardMetadataStore
{
    private readonly CardMetadataOptions cardMetadataOptions = options.Value;
    private readonly SemaphoreSlim snapshotCacheLock = new(1, 1);

    private SnapshotCacheEntry? cachedSnapshot;

    /// <summary>
    /// Loads the configured metadata snapshot or returns an empty skeleton when it is unavailable.
    /// </summary>
    public async Task<CardMetadataSnapshot> LoadSnapshotAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var snapshotPath = Path.Combine(cardMetadataOptions.SnapshotDirectory, cardMetadataOptions.SnapshotFileName);
        if (TryGetCachedSnapshot(snapshotPath, out var cachedSnapshot))
        {
            return cachedSnapshot;
        }

        await snapshotCacheLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (TryGetCachedSnapshot(snapshotPath, out cachedSnapshot))
            {
                return cachedSnapshot;
            }

            if (!File.Exists(snapshotPath))
            {
                logger.LogWarning("Card metadata snapshot was not found at {SnapshotPath}", snapshotPath);
                return CardMetadataSnapshot.Empty(snapshotPath);
            }

            try
            {
                await using var stream = File.OpenRead(snapshotPath);
                var rows = await ParquetSerializer.DeserializeAsync<ParquetCardMetadataRow>(
                    stream,
                    new ParquetSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                    },
                    cancellationToken).ConfigureAwait(false);

                var snapshot = new CardMetadataSnapshot(
                    Path.GetFileNameWithoutExtension(snapshotPath),
                    snapshotPath,
                    File.GetLastWriteTimeUtc(snapshotPath),
                    rows.Select(MapRecord).ToArray());

                logger.LogInformation(
                    "Loaded metadata snapshot {SnapshotPath} with {CardCount} cards",
                    snapshotPath,
                    snapshot.Cards.Count);

                UpdateSnapshotCache(snapshot);
                return snapshot;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Failed to read card metadata snapshot from {SnapshotPath}", snapshotPath);
                return CardMetadataSnapshot.Empty(snapshotPath);
            }
        }
        finally
        {
            snapshotCacheLock.Release();
        }
    }

    /// <summary>
    /// Upserts a card profile into the local Parquet snapshot using id-based deterministic merge semantics.
    /// Any existing record with the same <c>CardId</c> is replaced; new cards are appended.
    /// </summary>
    public async Task UpsertCardAsync(CardProfile card, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(card);

        var snapshotPath = Path.Combine(cardMetadataOptions.SnapshotDirectory, cardMetadataOptions.SnapshotFileName);
        var newRow = MapToRow(card);

        // Load existing rows so we can perform an id-keyed merge.
        var existingRows = new List<ParquetCardMetadataRow>();
        if (File.Exists(snapshotPath))
        {
            try
            {
                await using var readStream = File.OpenRead(snapshotPath);
                var loaded = await ParquetSerializer.DeserializeAsync<ParquetCardMetadataRow>(
                    readStream,
                    new ParquetSerializerOptions { PropertyNameCaseInsensitive = true },
                    cancellationToken).ConfigureAwait(false);
                existingRows.AddRange(loaded);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Failed to read existing snapshot before upsert at {SnapshotPath}; starting fresh", snapshotPath);
            }
        }

        var mergedRows = existingRows
            .Where(row => !string.Equals(row.CardId, card.CardId, StringComparison.OrdinalIgnoreCase))
            .Append(newRow)
            .ToArray();

        await ReplaceRowsAsync(
            snapshotPath,
            mergedRows,
            cancellationToken,
            successLogTemplate: "Upserted card {CardId} into snapshot {SnapshotPath} ({TotalCardCount} total cards)",
            successLogArgs: [card.CardId, snapshotPath, mergedRows.Length],
            failureLogTemplate: "Failed to upsert card {CardId} into snapshot {SnapshotPath}",
            failureLogArgs: [card.CardId, snapshotPath]).ConfigureAwait(false);
    }

    /// <summary>
    /// Replaces the local Parquet snapshot with the supplied card set using deterministic id-based semantics.
    /// </summary>
    public async Task ReplaceSnapshotAsync(IEnumerable<CardProfile> cards, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cards);

        var snapshotPath = Path.Combine(cardMetadataOptions.SnapshotDirectory, cardMetadataOptions.SnapshotFileName);
        var rows = cards
            .GroupBy(card => card.CardId, StringComparer.OrdinalIgnoreCase)
            .Select(group => MapToRow(group.Last()))
            .ToArray();

        await ReplaceRowsAsync(
            snapshotPath,
            rows,
            cancellationToken,
            successLogTemplate: "Replaced metadata snapshot {SnapshotPath} with {TotalCardCount} cards",
            successLogArgs: [snapshotPath, rows.Length],
            failureLogTemplate: "Failed to replace metadata snapshot {SnapshotPath}",
            failureLogArgs: [snapshotPath]).ConfigureAwait(false);
    }

    private async Task ReplaceRowsAsync(
        string snapshotPath,
        IReadOnlyCollection<ParquetCardMetadataRow> rows,
        CancellationToken cancellationToken,
        string successLogTemplate,
        object?[] successLogArgs,
        string failureLogTemplate,
        object?[] failureLogArgs)
    {
        // Write atomically via a temp file to prevent data loss if the process is interrupted.
        var directory = Path.GetDirectoryName(snapshotPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var tempPath = snapshotPath + ".tmp";
        try
        {
            await using (var writeStream = File.Create(tempPath))
            {
                await ParquetSerializer.SerializeAsync(rows, writeStream, cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            File.Move(tempPath, snapshotPath, overwrite: true);
            UpdateSnapshotCache(CreateSnapshot(snapshotPath, rows));

            logger.LogInformation(successLogTemplate, successLogArgs);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Clean up temp file on cancellation before re-throwing.
            TryDeleteTempFile(tempPath);
            throw;
        }
        catch (Exception exception)
        {
            TryDeleteTempFile(tempPath);
            logger.LogWarning(exception, failureLogTemplate, failureLogArgs);
        }
    }

    private void TryDeleteTempFile(string tempPath)
    {
        try
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Could not delete temp file {TempPath} after failed upsert", tempPath);
        }
    }

    private bool TryGetCachedSnapshot(string snapshotPath, out CardMetadataSnapshot snapshot)
    {
        var currentCache = cachedSnapshot;
        if (currentCache is not null
            && string.Equals(currentCache.SnapshotPath, snapshotPath, StringComparison.OrdinalIgnoreCase)
            && File.Exists(snapshotPath))
        {
            var fileInfo = new FileInfo(snapshotPath);
            if (fileInfo.Length == currentCache.FileLength && fileInfo.LastWriteTimeUtc == currentCache.LastWriteTimeUtc)
            {
                snapshot = currentCache.Snapshot;
                return true;
            }
        }

        snapshot = null!;
        return false;
    }

    private void UpdateSnapshotCache(CardMetadataSnapshot snapshot)
    {
        if (!File.Exists(snapshot.SourcePath))
        {
            cachedSnapshot = null;
            return;
        }

        var fileInfo = new FileInfo(snapshot.SourcePath);
        cachedSnapshot = new SnapshotCacheEntry(snapshot.SourcePath, fileInfo.Length, fileInfo.LastWriteTimeUtc, snapshot);
    }

    private CardMetadataSnapshot CreateSnapshot(string snapshotPath, IReadOnlyCollection<ParquetCardMetadataRow> rows) => new(
        Path.GetFileNameWithoutExtension(snapshotPath),
        snapshotPath,
        File.GetLastWriteTimeUtc(snapshotPath),
        rows.Select(MapRecord).ToArray());

    private static CardMetadataRecord MapRecord(ParquetCardMetadataRow row) => new(
        row.CardId,
        row.OracleId,
        row.Name,
        row.TypeLine,
        row.ColorIdentity ?? Array.Empty<string>(),
        row.ManaCost,
        row.ManaValue,
        row.SaltScore,
        row.ImageUri,
        row.HasMultipleFaces,
        row.OracleText,
        row.PlayRateByCommander,
        row.ThemeSignals,
        row.GenericColorStapleRate,
        row.IsGameChanger,
        row.IsMassLandDenial,
        row.IsLegalInCommander,
        row.AllowsMultipleCopies,
        row.CompanionRequirementCode,
        (CommanderEligibilityBasis)row.CommanderEligibilityBasis,
        (CardMetadataSource)row.MetadataSource,
        row.LastSyncedUtcTicks.HasValue ? new DateTimeOffset(row.LastSyncedUtcTicks.Value, TimeSpan.Zero) : null);

    private static ParquetCardMetadataRow MapToRow(CardProfile card) => new()
    {
        CardId = card.CardId,
        OracleId = card.OracleId,
        Name = card.Name,
        TypeLine = card.TypeLine,
        ColorIdentity = card.ColorIdentity.ToArray(),
        ManaCost = card.ManaCost,
        ManaValue = card.ManaValue,
        SaltScore = card.SaltScore,
        ImageUri = card.ImageUri,
        HasMultipleFaces = card.HasMultipleFaces,
        OracleText = card.OracleText,
        PlayRateByCommander = card.PlayRateByCommander.Count > 0
            ? new Dictionary<string, decimal>(card.PlayRateByCommander, StringComparer.OrdinalIgnoreCase)
            : null,
        ThemeSignals = card.ThemeSignals.Count > 0
            ? new Dictionary<string, decimal>(card.ThemeSignals, StringComparer.OrdinalIgnoreCase)
            : null,
        GenericColorStapleRate = card.GenericColorStapleRate,
        IsGameChanger = card.IsGameChanger,
        IsMassLandDenial = card.IsMassLandDenial,
        IsLegalInCommander = card.IsLegalInCommander,
        AllowsMultipleCopies = card.AllowsMultipleCopies,
        CompanionRequirementCode = card.CompanionRequirementCode,
        CommanderEligibilityBasis = (int)card.CommanderEligibilityBasis,
        MetadataSource = (int)card.MetadataSource,
        // Store as UTC ticks (long) for maximum Parquet.Net compatibility.
        LastSyncedUtcTicks = card.LastSyncedUtc?.UtcTicks,
    };

    private sealed class ParquetCardMetadataRow
    {
        public string CardId { get; init; } = string.Empty;

        public string? OracleId { get; init; }

        public string Name { get; init; } = string.Empty;

        public string TypeLine { get; init; } = string.Empty;

        public string[]? ColorIdentity { get; init; }

        public string? ManaCost { get; init; }

        public decimal ManaValue { get; init; }

        public decimal? SaltScore { get; init; }

        public string? ImageUri { get; init; }

        public bool HasMultipleFaces { get; init; }

        public string? OracleText { get; init; }

        public Dictionary<string, decimal>? PlayRateByCommander { get; init; }

        public Dictionary<string, decimal>? ThemeSignals { get; init; }

        public decimal? GenericColorStapleRate { get; init; }

        public bool IsGameChanger { get; init; }

        public bool IsMassLandDenial { get; init; }

        public bool IsLegalInCommander { get; init; } = true;

        public bool AllowsMultipleCopies { get; init; }

        public string? CompanionRequirementCode { get; init; }

        public int CommanderEligibilityBasis { get; init; }

        public int MetadataSource { get; init; }

        /// <summary>UTC ticks stored as a nullable long for Parquet compatibility.</summary>
        public long? LastSyncedUtcTicks { get; init; }
    }

    private sealed record SnapshotCacheEntry(string SnapshotPath, long FileLength, DateTime LastWriteTimeUtc, CardMetadataSnapshot Snapshot);
}

/// <summary>
/// Represents the current metadata snapshot descriptor returned by the store.
/// </summary>
public sealed record CardMetadataSnapshot(string SnapshotId, string SourcePath, DateTime LastUpdatedUtc, IReadOnlyList<CardMetadataRecord> Cards)
{
    /// <summary>
    /// Creates an empty snapshot descriptor for unavailable metadata.
    /// </summary>
    public static CardMetadataSnapshot Empty(string sourcePath) => new("missing", sourcePath, DateTime.UnixEpoch, Array.Empty<CardMetadataRecord>());
}

/// <summary>
/// Represents the minimum searchable card metadata record derived from the authoritative snapshot.
/// </summary>
public sealed record CardMetadataRecord(
    string CardId,
    string? OracleId,
    string Name,
    string TypeLine,
    IReadOnlyList<string> ColorIdentity,
    string? ManaCost,
    decimal ManaValue,
    decimal? SaltScore,
    string? ImageUri,
    bool HasMultipleFaces,
    string? OracleText = null,
    IReadOnlyDictionary<string, decimal>? PlayRateByCommander = null,
    IReadOnlyDictionary<string, decimal>? ThemeSignals = null,
    decimal? GenericColorStapleRate = null,
    bool IsGameChanger = false,
    bool IsMassLandDenial = false,
    bool IsLegalInCommander = true,
    bool AllowsMultipleCopies = false,
    string? CompanionRequirementCode = null,
    CommanderEligibilityBasis CommanderEligibilityBasis = CommanderEligibilityBasis.Unknown,
    CardMetadataSource MetadataSource = CardMetadataSource.Unknown,
    DateTimeOffset? LastSyncedUtc = null);
