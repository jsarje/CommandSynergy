using CommandSynergy.Application.Contracts;
using CommandSynergy.Domain.Cards;

namespace CommandSynergy.Infrastructure.Scryfall;

/// <summary>
/// Maps Scryfall card documents to search results and authoritative card profiles.
/// </summary>
public sealed class ScryfallCardMapper
{
    /// <summary>
    /// Maps a Scryfall document to a compact search result.
    /// </summary>
    public CardSearchResultContract MapSearchResult(ScryfallCardDocument document) => new()
    {
        CardId = document.Id,
        Name = document.Name,
        ManaCost = document.ManaCost,
        TypeLine = document.TypeLine ?? string.Empty,
        ColorIdentity = document.ColorIdentity,
        ImageUri = document.ImageUri,
        HasMultipleFaces = document.CardFaces.Count > 1,
    };

    /// <summary>
    /// Maps a Scryfall document to an authoritative card profile.
    /// </summary>
    public CardProfile MapCardProfile(ScryfallCardDocument document) => new()
    {
        CardId = document.Id,
        OracleId = document.OracleId,
        Name = document.Name,
        ManaCost = document.ManaCost,
        ManaValue = document.ManaValue,
        TypeLine = document.TypeLine ?? string.Empty,
        OracleText = document.OracleText,
        ColorIdentity = document.ColorIdentity,
        FaceProfiles = document.CardFaces.Select((face, index) => new CardFaceProfile(
            face.FaceId ?? index.ToString(),
            face.Name ?? document.Name,
            face.ManaCost,
            face.TypeLine ?? document.TypeLine ?? string.Empty,
            face.OracleText,
            face.ImageUri,
            index == 0)).ToArray(),
        ImageUri = document.ImageUri,
        IsLegalInCommander = !string.Equals(document.CommanderLegality, "not_legal", StringComparison.OrdinalIgnoreCase),
    };
}