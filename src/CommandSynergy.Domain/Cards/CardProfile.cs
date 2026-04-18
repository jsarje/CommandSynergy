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

    public IReadOnlyList<string> ColorIdentity { get; init; } = Array.Empty<string>();

    public IReadOnlyList<CardFaceProfile> FaceProfiles { get; init; } = Array.Empty<CardFaceProfile>();

    public string? ImageUri { get; init; }

    public decimal? SaltScore { get; init; }

    public IReadOnlyDictionary<string, decimal> PlayRateByCommander { get; init; } = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

    public decimal? GenericColorStapleRate { get; init; }

    public bool IsLegalInCommander { get; init; } = true;

    public bool AllowsMultipleCopies { get; init; }

    public string? CompanionRequirementCode { get; init; }

    public bool IsBasicLand => TypeLine.Contains("Basic", StringComparison.OrdinalIgnoreCase) && TypeLine.Contains("Land", StringComparison.OrdinalIgnoreCase);

    public bool IsLand => TypeLine.Contains("Land", StringComparison.OrdinalIgnoreCase);

    public bool HasMultipleFaces => FaceProfiles.Count > 1;
}