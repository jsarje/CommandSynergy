using CommandSynergy.Domain.Cards;

namespace CommandSynergy.Infrastructure.CardMetadata;

/// <summary>
/// Loads and persists the authoritative local card metadata snapshot.
/// </summary>
public interface IParquetCardMetadataStore
{
    /// <summary>
    /// Loads the configured metadata snapshot.
    /// </summary>
    Task<CardMetadataSnapshot> LoadSnapshotAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Upserts a single card profile into the local snapshot.
    /// </summary>
    Task UpsertCardAsync(CardProfile card, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces the local snapshot with the supplied card set.
    /// </summary>
    Task ReplaceSnapshotAsync(IEnumerable<CardProfile> cards, CancellationToken cancellationToken = default);
}
