using System.Text.Json.Serialization;
using CommandSynergy.Domain.Cards;

namespace CommandSynergy.Application.Contracts;

public static class DeckPortabilityContract
{
    public const string StorageKey = "command-synergy.imported-decks";
    public const int CurrentSchemaVersion = 2;
}

public sealed record DeckImportRequestContract
{
    [JsonPropertyName("rawDocumentText")]
    public required string RawDocumentText { get; init; }

    [JsonPropertyName("hintedFormatId")]
    public string? HintedFormatId { get; init; }

    [JsonPropertyName("sourceFileName")]
    public string? SourceFileName { get; init; }
}

public sealed record DeckImportResultContract
{
    [JsonPropertyName("detectedFormatId")]
    public string? DetectedFormatId { get; init; }

    [JsonPropertyName("requiresFormatConfirmation")]
    public required bool RequiresFormatConfirmation { get; init; }

    [JsonPropertyName("candidateFormatIds")]
    public IReadOnlyList<string> CandidateFormatIds { get; init; } = Array.Empty<string>();

    [JsonPropertyName("importedDeck")]
    public required ImportedDeckRecordContract ImportedDeck { get; init; }

    [JsonPropertyName("diagnostics")]
    public required IReadOnlyList<ImportDiagnosticContract> Diagnostics { get; init; }
}

public sealed record DeckExportRequestContract
{
    [JsonPropertyName("importedDeckId")]
    public required string ImportedDeckId { get; init; }

    [JsonPropertyName("targetFormatId")]
    public required string TargetFormatId { get; init; }
}

public sealed record DeckExportResultContract
{
    [JsonPropertyName("targetFormatId")]
    public required string TargetFormatId { get; init; }

    [JsonPropertyName("documentText")]
    public required string DocumentText { get; init; }

    [JsonPropertyName("warnings")]
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
}

public sealed record ImportedDeckLibraryDocumentContract
{
    [JsonPropertyName("schemaVersion")]
    public required int SchemaVersion { get; init; }

    [JsonPropertyName("activeDeckId")]
    public string? ActiveDeckId { get; init; }

    [JsonPropertyName("lastSavedUtc")]
    public DateTimeOffset? LastSavedUtc { get; init; }

    [JsonPropertyName("decks")]
    public required IReadOnlyList<ImportedDeckRecordContract> Decks { get; init; }
}

public sealed record ImportedDeckRecordContract
{
    [JsonPropertyName("importedDeckId")]
    public required string ImportedDeckId { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("sourceFormatId")]
    public required string SourceFormatId { get; init; }

    [JsonPropertyName("importedAtUtc")]
    public required DateTimeOffset ImportedAtUtc { get; init; }

    [JsonPropertyName("lastOpenedUtc")]
    public DateTimeOffset? LastOpenedUtc { get; init; }

    [JsonPropertyName("originalDocumentText")]
    public string OriginalDocumentText { get; init; } = string.Empty;

    [JsonPropertyName("normalizedDeck")]
    public required PortableDeckSnapshotContract NormalizedDeck { get; init; }

    [JsonPropertyName("diagnostics")]
    public required IReadOnlyList<ImportDiagnosticContract> Diagnostics { get; init; }

    [JsonPropertyName("exportWarnings")]
    public IReadOnlyList<string> ExportWarnings { get; init; } = Array.Empty<string>();

    [JsonPropertyName("sourceMetadata")]
    public IReadOnlyDictionary<string, string> SourceMetadata { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}

public sealed record PortableDeckSnapshotContract
{
    [JsonPropertyName("deckName")]
    public required string DeckName { get; init; }

    [JsonPropertyName("commanderCardIds")]
    public IReadOnlyList<string> CommanderCardIds { get; init; } = Array.Empty<string>();

    [JsonPropertyName("companionCardId")]
    public string? CompanionCardId { get; init; }

    [JsonPropertyName("importedCardCount")]
    public required int ImportedCardCount { get; init; }

    [JsonPropertyName("hasUnresolvedLines")]
    public required bool HasUnresolvedLines { get; init; }

    [JsonPropertyName("entries")]
    public required IReadOnlyList<PortableDeckEntryContract> Entries { get; init; }

    [JsonPropertyName("sections")]
    public required IReadOnlyList<DeckSectionStateContract> Sections { get; init; }
}

public sealed record PortableDeckEntryContract
{
    [JsonPropertyName("cardId")]
    public string? CardId { get; init; }

    [JsonPropertyName("originalCardText")]
    public string? OriginalCardText { get; init; }

    [JsonPropertyName("displayName")]
    public required string DisplayName { get; init; }

    [JsonPropertyName("manaCost")]
    public string? ManaCost { get; init; }

    [JsonPropertyName("typeLine")]
    public string? TypeLine { get; init; }

    [JsonPropertyName("colorIdentity")]
    public IReadOnlyList<string> ColorIdentity { get; init; } = Array.Empty<string>();

    [JsonPropertyName("saltScore")]
    public decimal? SaltScore { get; init; }

    [JsonPropertyName("imageUri")]
    public string? ImageUri { get; init; }

    [JsonPropertyName("hasMultipleFaces")]
    public bool HasMultipleFaces { get; init; }

    [JsonPropertyName("commanderEligibilityBasis")]
    public CommanderEligibilityBasis CommanderEligibilityBasis { get; init; } = CommanderEligibilityBasis.Unknown;

    [JsonPropertyName("quantity")]
    public required int Quantity { get; init; }

    [JsonPropertyName("sectionId")]
    public required string SectionId { get; init; }

    [JsonPropertyName("isCommander")]
    public bool IsCommander { get; init; }

    [JsonPropertyName("isCompanion")]
    public bool IsCompanion { get; init; }

    [JsonPropertyName("parseConfidence")]
    public required string ParseConfidence { get; init; }
}

public sealed record DeckSectionStateContract
{
    [JsonPropertyName("sectionId")]
    public required string SectionId { get; init; }

    [JsonPropertyName("displayName")]
    public required string DisplayName { get; init; }

    [JsonPropertyName("role")]
    public required string Role { get; init; }

    [JsonPropertyName("sortOrder")]
    public int SortOrder { get; init; }

    [JsonPropertyName("entryCount")]
    public int EntryCount { get; init; }
}

public sealed record ImportDiagnosticContract
{
    [JsonPropertyName("diagnosticId")]
    public required string DiagnosticId { get; init; }

    [JsonPropertyName("severity")]
    public required string Severity { get; init; }

    [JsonPropertyName("code")]
    public required string Code { get; init; }

    [JsonPropertyName("message")]
    public required string Message { get; init; }

    [JsonPropertyName("sourceLineNumber")]
    public int? SourceLineNumber { get; init; }

    [JsonPropertyName("sourceLineText")]
    public string? SourceLineText { get; init; }

    [JsonPropertyName("suggestedAction")]
    public string? SuggestedAction { get; init; }
}

public sealed record ExportPreviewContract
{
    [JsonPropertyName("targetFormatId")]
    public required string TargetFormatId { get; init; }

    [JsonPropertyName("documentText")]
    public required string DocumentText { get; init; }

    [JsonPropertyName("warnings")]
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

    [JsonPropertyName("generatedUtc")]
    public required DateTimeOffset GeneratedUtc { get; init; }
}