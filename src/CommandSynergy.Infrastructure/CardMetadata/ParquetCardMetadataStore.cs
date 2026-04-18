using CommandSynergy.Application.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Parquet.Serialization;

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
    public async Task<CardMetadataSnapshot> LoadSnapshotAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var snapshotPath = Path.Combine(options.SnapshotDirectory, options.SnapshotFileName);
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
        row.GenericColorStapleRate,
        row.IsLegalInCommander,
        row.AllowsMultipleCopies,
        row.CompanionRequirementCode);

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

        public decimal? GenericColorStapleRate { get; init; }

        public bool IsLegalInCommander { get; init; } = true;

        public bool AllowsMultipleCopies { get; init; }

        public string? CompanionRequirementCode { get; init; }
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
    decimal? GenericColorStapleRate = null,
    bool IsLegalInCommander = true,
    bool AllowsMultipleCopies = false,
    string? CompanionRequirementCode = null);