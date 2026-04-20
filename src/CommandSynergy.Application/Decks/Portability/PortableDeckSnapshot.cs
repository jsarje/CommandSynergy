using CommandSynergy.Application.Contracts;
using CommandSynergy.Domain.Cards;

namespace CommandSynergy.Application.Decks.Portability;

public sealed record PortableDeckSnapshot(
    string DeckName,
    IReadOnlyList<string> CommanderCardIds,
    string? CompanionCardId,
    IReadOnlyList<PortableDeckEntry> Entries,
    IReadOnlyList<DeckSectionState> Sections,
    int ImportedCardCount,
    bool HasUnresolvedLines)
{
    public PortableDeckSnapshotContract ToContract() => new()
    {
        DeckName = DeckName,
        CommanderCardIds = CommanderCardIds,
        CompanionCardId = CompanionCardId,
        ImportedCardCount = ImportedCardCount,
        HasUnresolvedLines = HasUnresolvedLines,
        Entries = Entries.Select(static entry => entry.ToContract()).ToArray(),
        Sections = Sections.Select(static section => section.ToContract()).ToArray(),
    };

    public static PortableDeckSnapshot FromContract(PortableDeckSnapshotContract contract) => new(
        contract.DeckName,
        contract.CommanderCardIds,
        contract.CompanionCardId,
        contract.Entries.Select(PortableDeckEntry.FromContract).ToArray(),
        contract.Sections.Select(DeckSectionState.FromContract).ToArray(),
        contract.ImportedCardCount,
        contract.HasUnresolvedLines);
}

public sealed record PortableDeckEntry(
    string? CardId,
    string? OriginalCardText,
    string DisplayName,
    string? ManaCost,
    string? TypeLine,
    IReadOnlyList<string> ColorIdentity,
    decimal? SaltScore,
    string? ImageUri,
    bool HasMultipleFaces,
    CommanderEligibilityBasis CommanderEligibilityBasis,
    int Quantity,
    string SectionId,
    bool IsCommander,
    bool IsCompanion,
    ParseConfidence ParseConfidence)
{
    public PortableDeckEntryContract ToContract() => new()
    {
        CardId = CardId,
        OriginalCardText = OriginalCardText,
        DisplayName = DisplayName,
        ManaCost = ManaCost,
        TypeLine = TypeLine,
        ColorIdentity = ColorIdentity,
        SaltScore = SaltScore,
        ImageUri = ImageUri,
        HasMultipleFaces = HasMultipleFaces,
        CommanderEligibilityBasis = CommanderEligibilityBasis,
        Quantity = Quantity,
        SectionId = SectionId,
        IsCommander = IsCommander,
        IsCompanion = IsCompanion,
        ParseConfidence = ParseConfidence.ToContractValue(),
    };

    public static PortableDeckEntry FromContract(PortableDeckEntryContract contract) => new(
        contract.CardId,
        contract.OriginalCardText,
        contract.DisplayName,
        contract.ManaCost,
        contract.TypeLine,
        contract.ColorIdentity,
        contract.SaltScore,
        contract.ImageUri,
        contract.HasMultipleFaces,
        contract.CommanderEligibilityBasis,
        contract.Quantity,
        contract.SectionId,
        contract.IsCommander,
        contract.IsCompanion,
        ParseConfidenceExtensions.FromContractValue(contract.ParseConfidence));
}

public sealed record DeckSectionState(
    string SectionId,
    string DisplayName,
    DeckSectionRole Role,
    int SortOrder,
    int EntryCount)
{
    public DeckSectionStateContract ToContract() => new()
    {
        SectionId = SectionId,
        DisplayName = DisplayName,
        Role = Role.ToContractValue(),
        SortOrder = SortOrder,
        EntryCount = EntryCount,
    };

    public static DeckSectionState FromContract(DeckSectionStateContract contract) => new(
        contract.SectionId,
        contract.DisplayName,
        DeckSectionRoleExtensions.FromContractValue(contract.Role),
        contract.SortOrder,
        contract.EntryCount);
}

public enum ParseConfidence
{
    Exact,
    Normalized,
    Ambiguous,
    Unresolved,
}

public enum DeckSectionRole
{
    Commander,
    Mainboard,
    Sideboard,
    Maybeboard,
    Companion,
    Custom,
}

internal static class ParseConfidenceExtensions
{
    public static string ToContractValue(this ParseConfidence parseConfidence) => parseConfidence switch
    {
        ParseConfidence.Exact => "exact",
        ParseConfidence.Normalized => "normalized",
        ParseConfidence.Ambiguous => "ambiguous",
        _ => "unresolved",
    };

    public static ParseConfidence FromContractValue(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "exact" => ParseConfidence.Exact,
        "normalized" => ParseConfidence.Normalized,
        "ambiguous" => ParseConfidence.Ambiguous,
        _ => ParseConfidence.Unresolved,
    };
}

internal static class DeckSectionRoleExtensions
{
    public static string ToContractValue(this DeckSectionRole role) => role switch
    {
        DeckSectionRole.Commander => "commander",
        DeckSectionRole.Mainboard => "mainboard",
        DeckSectionRole.Sideboard => "sideboard",
        DeckSectionRole.Maybeboard => "maybeboard",
        DeckSectionRole.Companion => "companion",
        _ => "custom",
    };

    public static DeckSectionRole FromContractValue(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "commander" => DeckSectionRole.Commander,
        "mainboard" => DeckSectionRole.Mainboard,
        "sideboard" => DeckSectionRole.Sideboard,
        "maybeboard" => DeckSectionRole.Maybeboard,
        "companion" => DeckSectionRole.Companion,
        _ => DeckSectionRole.Custom,
    };
}