using System.Text.Json.Serialization;
using CommandSynergy.Domain.Cards;

namespace CommandSynergy.Application.Contracts;

/// <summary>
/// Represents a deck snapshot sent between the UI and server-owned deck services.
/// </summary>
public sealed record DeckSnapshotContract
{
    [JsonPropertyName("deckId")]
    public string? DeckId { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("commanderCardId")]
    public string? CommanderCardId { get; init; }

    [JsonPropertyName("companionCardId")]
    public string? CompanionCardId { get; init; }

    [JsonPropertyName("entries")]
    public required IReadOnlyList<DeckEntryContract> Entries { get; init; }

    [JsonPropertyName("piles")]
    public IReadOnlyList<PileDefinitionContract> Piles { get; init; } = Array.Empty<PileDefinitionContract>();
}

/// <summary>
/// Represents a card entry in a submitted or persisted deck snapshot.
/// </summary>
public sealed record DeckEntryContract
{
    [JsonPropertyName("cardId")]
    public required string CardId { get; init; }

    [JsonPropertyName("quantity")]
    public required int Quantity { get; init; }

    [JsonPropertyName("assignedPileId")]
    public string? AssignedPileId { get; init; }

    [JsonPropertyName("isCommander")]
    public bool IsCommander { get; init; }

    [JsonPropertyName("isCompanion")]
    public bool IsCompanion { get; init; }
}

/// <summary>
/// Represents a user-visible pile definition in the deck workspace.
/// </summary>
public sealed record PileDefinitionContract
{
    [JsonPropertyName("pileId")]
    public required string PileId { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("sortOrder")]
    public int SortOrder { get; init; }
}

/// <summary>
/// Represents a card-search query shape used by application services.
/// </summary>
public sealed record CardSearchQueryContract
{
    public required string Query { get; init; }

    public string? CommanderCardId { get; init; }

    public IReadOnlyList<string> Colors { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Represents a compact card summary returned by search or client search artifacts.
/// </summary>
public sealed record CardSearchResultContract
{
    [JsonPropertyName("cardId")]
    public required string CardId { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("manaCost")]
    public string? ManaCost { get; init; }

    [JsonPropertyName("typeLine")]
    public required string TypeLine { get; init; }

    [JsonPropertyName("colorIdentity")]
    public required IReadOnlyList<string> ColorIdentity { get; init; }

    [JsonPropertyName("saltScore")]
    public decimal? SaltScore { get; init; }

    [JsonPropertyName("imageUri")]
    public string? ImageUri { get; init; }

    [JsonPropertyName("hasMultipleFaces")]
    public bool HasMultipleFaces { get; init; }

    [JsonPropertyName("commanderEligibilityBasis")]
    public CommanderEligibilityBasis CommanderEligibilityBasis { get; init; }
}

/// <summary>
/// Represents the card-search response contract.
/// </summary>
public sealed record CardSearchResponseContract
{
    [JsonPropertyName("snapshotVersion")]
    public string? SnapshotVersion { get; init; }

    [JsonPropertyName("results")]
    public required IReadOnlyList<CardSearchResultContract> Results { get; init; }
}

/// <summary>
/// Represents a validation response for a submitted deck snapshot.
/// </summary>
public sealed record DeckValidationResponseContract
{
    [JsonPropertyName("isValid")]
    public required bool IsValid { get; init; }

    [JsonPropertyName("findings")]
    public required IReadOnlyList<ValidationFindingContract> Findings { get; init; }

    [JsonPropertyName("deckCardCount")]
    public int DeckCardCount { get; init; }
}

/// <summary>
/// Represents a single structured validation finding.
/// </summary>
public sealed record ValidationFindingContract
{
    [JsonPropertyName("severity")]
    public required string Severity { get; init; }

    [JsonPropertyName("code")]
    public required string Code { get; init; }

    [JsonPropertyName("message")]
    public required string Message { get; init; }

    [JsonPropertyName("affectedCardIds")]
    public IReadOnlyList<string> AffectedCardIds { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Represents the combined analysis response for the current deck snapshot.
/// </summary>
public sealed record DeckAnalysisResponseContract
{
    [JsonPropertyName("bracket")]
    public required BracketAssessmentContract Bracket { get; init; }

    [JsonPropertyName("synergy")]
    public required SynergyAssessmentContract Synergy { get; init; }
}

/// <summary>
/// Represents a bracket assessment response payload.
/// </summary>
public sealed record BracketAssessmentContract
{
    [JsonPropertyName("level")]
    public required int Level { get; init; }

    [JsonPropertyName("totalWeight")]
    public required decimal TotalWeight { get; init; }

    [JsonPropertyName("summary")]
    public required string Summary { get; init; }

    [JsonPropertyName("factors")]
    public IReadOnlyList<BracketFactorContract> Factors { get; init; } = Array.Empty<BracketFactorContract>();
}

/// <summary>
/// Represents a single weighted factor in the bracket result.
/// </summary>
public sealed record BracketFactorContract
{
    [JsonPropertyName("sourceCardId")]
    public string? SourceCardId { get; init; }

    [JsonPropertyName("category")]
    public required string Category { get; init; }

    [JsonPropertyName("weight")]
    public required decimal Weight { get; init; }

    [JsonPropertyName("explanation")]
    public required string Explanation { get; init; }
}

/// <summary>
/// Represents the synergy assessment response payload.
/// </summary>
public sealed record SynergyAssessmentContract
{
    [JsonPropertyName("score")]
    public required decimal Score { get; init; }

    [JsonPropertyName("summary")]
    public required string Summary { get; init; }

    [JsonPropertyName("commanderSpecificHits")]
    public IReadOnlyList<string> CommanderSpecificHits { get; init; } = Array.Empty<string>();

    [JsonPropertyName("stapleOverloadIndicators")]
    public IReadOnlyList<string> StapleOverloadIndicators { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Represents the server-generated search artifact shared with the client.
/// </summary>
public sealed record SearchIndexSnapshotContract
{
    public required string Version { get; init; }

    public required DateTimeOffset GeneratedUtc { get; init; }

    public required string SourceSnapshotId { get; init; }

    public required IReadOnlyList<CardSearchResultContract> CardSummaries { get; init; }
}