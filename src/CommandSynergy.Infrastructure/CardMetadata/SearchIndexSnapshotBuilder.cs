using CommandSynergy.Application.Configuration;
using CommandSynergy.Application.Contracts;
using Microsoft.Extensions.Options;

namespace CommandSynergy.Infrastructure.CardMetadata;

/// <summary>
/// Builds the derived client search artifact from the authoritative metadata snapshot.
/// </summary>
public sealed class SearchIndexSnapshotBuilder(IOptions<CardMetadataOptions> options) : ISearchIndexSnapshotBuilder
{
    private readonly CardMetadataOptions cardMetadataOptions = options.Value;

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
                ManaValue = card.ManaValue,
                TypeLine = card.TypeLine,
                ColorIdentity = card.ColorIdentity,
                SaltScore = card.SaltScore,
                ImageUri = card.ImageUri,
                EurPrice = card.EurPrice,
                HasMultipleFaces = card.HasMultipleFaces,
                AllowsMultipleCopies = card.AllowsMultipleCopies,
                CommanderEligibilityBasis = card.CommanderEligibilityBasis,
            })
            .ToArray();

        return new SearchIndexSnapshotContract
        {
            Version = cardMetadataOptions.SearchIndexVersion,
            GeneratedUtc = DateTimeOffset.UtcNow,
            SourceSnapshotId = snapshot.SnapshotId,
            CardSummaries = summaries,
        };
    }
}
