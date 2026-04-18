using CommandSynergy.Application.Configuration;
using CommandSynergy.Application.Contracts;
using Microsoft.Extensions.Options;

namespace CommandSynergy.Infrastructure.CardMetadata;

/// <summary>
/// Builds the derived client search artifact from the authoritative metadata snapshot.
/// </summary>
public sealed class SearchIndexSnapshotBuilder
{
    private readonly CardMetadataOptions options;

    /// <summary>
    /// Creates a builder for the lightweight client search index.
    /// </summary>
    public SearchIndexSnapshotBuilder(IOptions<CardMetadataOptions> options)
    {
        this.options = options.Value;
    }

    /// <summary>
    /// Produces a compact client search snapshot from the supplied metadata snapshot.
    /// </summary>
    public SearchIndexSnapshotContract Build(CardMetadataSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var summaries = snapshot.Cards
            .Select(card => new CardSearchResultContract
            {
                CardId = card.CardId,
                Name = card.Name,
                ManaCost = card.ManaCost,
                TypeLine = card.TypeLine,
                ColorIdentity = card.ColorIdentity,
                SaltScore = card.SaltScore,
                ImageUri = card.ImageUri,
                HasMultipleFaces = card.HasMultipleFaces,
            })
            .ToArray();

        return new SearchIndexSnapshotContract
        {
            Version = options.SearchIndexVersion,
            GeneratedUtc = DateTimeOffset.UtcNow,
            SourceSnapshotId = snapshot.SnapshotId,
            CardSummaries = summaries,
        };
    }
}