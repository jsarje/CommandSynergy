using CommandSynergy.Application.Contracts;
using CommandSynergy.Application.Analysis;
using CommandSynergy.Domain.Cards;

namespace CommandSynergy.Infrastructure.Scryfall;

/// <summary>
/// Maps Scryfall card documents to search results and authoritative card profiles.
/// </summary>
public sealed class ScryfallCardMapper : IScryfallCardMapper
{
    private readonly IThemeMatchingService themeMatchingService;

    /// <summary>
    /// Creates a mapper for Scryfall card documents.
    /// </summary>
    public ScryfallCardMapper(IThemeMatchingService themeMatchingService)
    {
        this.themeMatchingService = themeMatchingService;
    }

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
        CommanderEligibilityBasis = DetermineEligibilityBasis(document),
    };

    /// <summary>
    /// Maps a Scryfall document to an authoritative card profile.
    /// </summary>
    public CardProfile MapCardProfile(ScryfallCardDocument document) =>
        MapCardProfile(document, CardMetadataSource.UserDrivenScryfallEnrichment, DateTimeOffset.UtcNow, new HashSet<string>(StringComparer.OrdinalIgnoreCase));

    /// <summary>
    /// Maps a Scryfall document to an authoritative card profile with explicit metadata provenance.
    /// </summary>
    public CardProfile MapCardProfile(ScryfallCardDocument document, CardMetadataSource metadataSource, DateTimeOffset? lastSyncedUtc) =>
        MapCardProfile(document, metadataSource, lastSyncedUtc, new HashSet<string>(StringComparer.OrdinalIgnoreCase));

    /// <summary>
    /// Maps a Scryfall document to an authoritative card profile with explicit metadata provenance and external oracle-tag matches.
    /// </summary>
    public CardProfile MapCardProfile(ScryfallCardDocument document, CardMetadataSource metadataSource, DateTimeOffset? lastSyncedUtc, IReadOnlySet<string> massLandDenialIds)
    {
        var provisionalProfile = new CardProfile
        {
            CardId = document.Id,
            OracleId = document.OracleId,
            Name = document.Name,
            ManaCost = document.ManaCost,
            ManaValue = document.ManaValue,
            TypeLine = document.TypeLine ?? string.Empty,
            OracleText = document.OracleText,
            Keywords = document.Keywords,
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
            IsGameChanger = document.IsGameChanger,
            IsMassLandDenial = massLandDenialIds.Contains(document.Id),
            IsLegalInCommander = !string.Equals(document.CommanderLegality, "not_legal", StringComparison.OrdinalIgnoreCase),
            CommanderEligibilityBasis = DetermineEligibilityBasis(document),
            MetadataSource = metadataSource,
            LastSyncedUtc = lastSyncedUtc,
        };

        return new CardProfile
        {
            CardId = provisionalProfile.CardId,
            OracleId = provisionalProfile.OracleId,
            Name = provisionalProfile.Name,
            ManaCost = provisionalProfile.ManaCost,
            ManaValue = provisionalProfile.ManaValue,
            TypeLine = provisionalProfile.TypeLine,
            OracleText = provisionalProfile.OracleText,
            Keywords = provisionalProfile.Keywords,
            ColorIdentity = provisionalProfile.ColorIdentity,
            FaceProfiles = provisionalProfile.FaceProfiles,
            ImageUri = provisionalProfile.ImageUri,
            PlayRateByCommander = provisionalProfile.PlayRateByCommander,
            ThemeSignals = themeMatchingService.ComputeThemeSignals(provisionalProfile),
            GenericColorStapleRate = provisionalProfile.GenericColorStapleRate,
            SaltScore = provisionalProfile.SaltScore,
            IsGameChanger = provisionalProfile.IsGameChanger,
            IsMassLandDenial = provisionalProfile.IsMassLandDenial,
            IsLegalInCommander = provisionalProfile.IsLegalInCommander,
            AllowsMultipleCopies = provisionalProfile.AllowsMultipleCopies,
            CompanionRequirementCode = provisionalProfile.CompanionRequirementCode,
            CommanderEligibilityBasis = provisionalProfile.CommanderEligibilityBasis,
            MetadataSource = provisionalProfile.MetadataSource,
            LastSyncedUtc = provisionalProfile.LastSyncedUtc,
        };
    }

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
