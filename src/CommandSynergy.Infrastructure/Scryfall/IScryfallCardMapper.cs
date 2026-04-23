using CommandSynergy.Application.Contracts;
using CommandSynergy.Domain.Cards;

namespace CommandSynergy.Infrastructure.Scryfall;

/// <summary>
/// Maps Scryfall card documents to search results and authoritative card profiles.
/// </summary>
public interface IScryfallCardMapper
{
    /// <summary>
    /// Maps a Scryfall document to a compact search result.
    /// </summary>
    CardSearchResultContract MapSearchResult(ScryfallCardDocument document);

    /// <summary>
    /// Maps a Scryfall document to an authoritative card profile.
    /// </summary>
    CardProfile MapCardProfile(ScryfallCardDocument document);

    /// <summary>
    /// Maps a Scryfall document to an authoritative card profile with explicit metadata provenance.
    /// </summary>
    CardProfile MapCardProfile(ScryfallCardDocument document, CardMetadataSource metadataSource, DateTimeOffset? lastSyncedUtc);

    /// <summary>
    /// Maps a Scryfall document to an authoritative card profile with explicit metadata provenance and external oracle-tag matches.
    /// </summary>
    CardProfile MapCardProfile(ScryfallCardDocument document, CardMetadataSource metadataSource, DateTimeOffset? lastSyncedUtc, IReadOnlySet<string> massLandDenialIds);
}
