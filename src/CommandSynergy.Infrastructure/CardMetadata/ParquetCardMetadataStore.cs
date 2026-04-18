using CommandSynergy.Application.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CommandSynergy.Infrastructure.CardMetadata;

/// <summary>
/// Loads the authoritative local card metadata snapshot from the configured Parquet location.
/// </summary>
public sealed class ParquetCardMetadataStore
{
    private readonly CardMetadataOptions options;
    private readonly ILogger<ParquetCardMetadataStore> logger;

    /// <summary>
    /// Creates a snapshot loader for local Parquet-backed card metadata.
    /// </summary>
    public ParquetCardMetadataStore(IOptions<CardMetadataOptions> options, ILogger<ParquetCardMetadataStore> logger)
    {
        this.options = options.Value;
        this.logger = logger;
    }

    /// <summary>
    /// Loads the configured metadata snapshot or returns an empty skeleton when it is unavailable.
    /// </summary>
    public Task<CardMetadataSnapshot> LoadSnapshotAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var snapshotPath = Path.Combine(options.SnapshotDirectory, options.SnapshotFileName);
        if (!File.Exists(snapshotPath))
        {
            logger.LogWarning("Card metadata snapshot was not found at {SnapshotPath}", snapshotPath);
            return Task.FromResult(CardMetadataSnapshot.Empty(snapshotPath));
        }

        var snapshot = new CardMetadataSnapshot(
            Path.GetFileNameWithoutExtension(snapshotPath),
            snapshotPath,
            File.GetLastWriteTimeUtc(snapshotPath),
            Array.Empty<CardMetadataRecord>());

        logger.LogInformation("Loaded metadata snapshot descriptor for {SnapshotPath}", snapshotPath);
        return Task.FromResult(snapshot);
    }
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
    decimal? GenericColorStapleRate = null);