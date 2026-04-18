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
    public CardProfile MapCardProfile(ScryfallCardDocument document) =>
        MapCardProfile(document, CardMetadataSource.UserDrivenScryfallEnrichment, DateTimeOffset.UtcNow);

    /// <summary>
    /// Maps a Scryfall document to an authoritative card profile with explicit metadata provenance.
    /// </summary>
    public CardProfile MapCardProfile(ScryfallCardDocument document, CardMetadataSource metadataSource, DateTimeOffset? lastSyncedUtc) => new()
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
            index == 0)).DefaultIfEmpty(new CardFaceProfile("0", document.Name, document.ManaCost, document.TypeLine ?? string.Empty, document.OracleText, document.ImageUri, true)).ToArray(),
        ImageUri = document.ImageUri,
        IsLegalInCommander = !string.Equals(document.CommanderLegality, "not_legal", StringComparison.OrdinalIgnoreCase),
        CommanderEligibilityBasis = DetermineEligibilityBasis(document),
        MetadataSource = metadataSource,
        LastSyncedUtc = lastSyncedUtc,
    };

    /// <summary>
    /// Determines the commander eligibility basis for a Scryfall document.
    /// </summary>
    private static CommanderEligibilityBasis DetermineEligibilityBasis(ScryfallCardDocument document)
    {
        // Cards explicitly marked as not legal in Commander are never eligible.
        if (string.Equals(document.CommanderLegality, "not_legal", StringComparison.OrdinalIgnoreCase))
        {
            return CommanderEligibilityBasis.NotEligible;
        }

        var typeLine = document.TypeLine ?? string.Empty;
        if (typeLine.Contains("Legendary", StringComparison.OrdinalIgnoreCase)
            && typeLine.Contains("Creature", StringComparison.OrdinalIgnoreCase))
        {
            return CommanderEligibilityBasis.LegendaryCreature;
        }

        var oracleText = document.OracleText ?? string.Empty;
        if (oracleText.Contains("can be your commander", StringComparison.OrdinalIgnoreCase))
        {
            return CommanderEligibilityBasis.OracleTextException;
        }

        return CommanderEligibilityBasis.Unknown;
    }
}