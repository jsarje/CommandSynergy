namespace CommandSynergy.Domain.Cards;

/// <summary>
/// Represents the canonical card metadata needed for commander validation and deck search.
/// </summary>
public sealed class CardProfile
{
    public required string CardId { get; init; }

    public string? OracleId { get; init; }

    public required string Name { get; init; }

    public string? ManaCost { get; init; }

    public decimal ManaValue { get; init; }

    public required string TypeLine { get; init; }

    public string? OracleText { get; init; }

    public IReadOnlyList<string> Keywords { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> ColorIdentity { get; init; } = Array.Empty<string>();

    public IReadOnlyList<CardFaceProfile> FaceProfiles { get; init; } = Array.Empty<CardFaceProfile>();

    public string? ImageUri { get; init; }

    public decimal? SaltScore { get; init; }

    public IReadOnlyDictionary<string, decimal> PlayRateByCommander { get; init; } = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, decimal> ThemeSignals { get; init; } = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

    public decimal? GenericColorStapleRate { get; init; }

    public bool IsGameChanger { get; init; }

    public bool IsMassLandDenial { get; init; }

    public bool IsLegalInCommander { get; init; } = true;

    public bool AllowsMultipleCopies { get; init; }

    public string? CompanionRequirementCode { get; init; }

    /// <summary>
    /// Gets the basis under which this card is eligible to serve as a commander.
    /// </summary>
    public CommanderEligibilityBasis CommanderEligibilityBasis { get; init; } = CommanderEligibilityBasis.Unknown;

    /// <summary>
    /// Gets whether this card is eligible to be selected as a commander under official Commander rules.
    /// </summary>
    public bool IsCommanderEligible =>
        CommanderEligibilityBasis is CommanderEligibilityBasis.LegendaryCreature or CommanderEligibilityBasis.OracleTextException;

    /// <summary>
    /// Gets the source of the metadata record for this card.
    /// </summary>
    public CardMetadataSource MetadataSource { get; init; } = CardMetadataSource.Unknown;

    /// <summary>
    /// Gets the UTC timestamp of the most recent local metadata sync for this card.
    /// </summary>
    public DateTimeOffset? LastSyncedUtc { get; init; }

    public bool IsBasicLand => TypeLine.Contains("Basic", StringComparison.OrdinalIgnoreCase) && TypeLine.Contains("Land", StringComparison.OrdinalIgnoreCase);

    public bool IsLand => TypeLine.Contains("Land", StringComparison.OrdinalIgnoreCase);

    public bool HasMultipleFaces => FaceProfiles.Count > 1;
}