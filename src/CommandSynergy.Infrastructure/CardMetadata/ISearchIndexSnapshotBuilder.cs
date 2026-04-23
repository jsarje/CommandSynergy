using CommandSynergy.Application.Contracts;

namespace CommandSynergy.Infrastructure.CardMetadata;

/// <summary>
/// Builds the derived client search artifact from the authoritative metadata snapshot.
/// </summary>
public interface ISearchIndexSnapshotBuilder
{
    /// <summary>
    /// Produces a compact client search snapshot from the supplied metadata snapshot.
    /// </summary>
    SearchIndexSnapshotContract Build(CardMetadataSnapshot snapshot);
}
