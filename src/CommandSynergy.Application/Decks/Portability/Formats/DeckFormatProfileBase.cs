using System.Text.RegularExpressions;

namespace CommandSynergy.Application.Decks.Portability.Formats;

public abstract class DeckFormatProfileBase
{
    private static readonly Regex QuantityLinePattern = new("^(?<quantity>\\d+)x?\\s+(?<name>.+?)$", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    private static readonly Regex EntryMetadataLinePattern = new("^(?:(?<quantity>\\d+)x?\\s+)?(?<name>.+?)(?:\\s+\\((?<setCode>[^()]+)\\)(?:\\s+(?<collectorNumber>[^\\s\\[\\]]+))?)?(?:\\s+\\[(?<tag>[^\\[\\]]+)\\])?$", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    public abstract string FormatId { get; }

    public abstract string DisplayName { get; }

    public virtual bool SupportsImport => true;

    public virtual bool SupportsExport => true;

    public abstract int Detect(string documentText);

    public abstract FormatParseResult Parse(string documentText);

    public abstract string Render(PortableDeckSnapshot snapshot, IReadOnlyList<string> warnings);

    internal static bool TryParseQuantityLine(string line, out int quantity, out string name)
    {
        var match = QuantityLinePattern.Match(line.Trim());
        if (!match.Success || !int.TryParse(match.Groups["quantity"].Value, out quantity))
        {
            quantity = 0;
            name = string.Empty;
            return false;
        }

        name = match.Groups["name"].Value.Trim();
        return !string.IsNullOrWhiteSpace(name);
    }

    internal static bool TryParseEntryLine(
        string line,
        out int quantity,
        out string name,
        out string? setCode,
        out string? collectorNumber,
        out string? tag)
    {
        var match = EntryMetadataLinePattern.Match(line.Trim());
        if (!match.Success)
        {
            quantity = 0;
            name = string.Empty;
            setCode = null;
            collectorNumber = null;
            tag = null;
            return false;
        }

        var quantityGroup = match.Groups["quantity"];
        var hasQuantity = quantityGroup.Success && !string.IsNullOrWhiteSpace(quantityGroup.Value);
        quantity = 1;
        if (hasQuantity && !int.TryParse(quantityGroup.Value, out quantity))
        {
            quantity = 0;
            name = string.Empty;
            setCode = null;
            collectorNumber = null;
            tag = null;
            return false;
        }

        name = match.Groups["name"].Value.Trim();
        setCode = NormalizeOptionalValue(match.Groups["setCode"].Value);
        collectorNumber = NormalizeOptionalValue(match.Groups["collectorNumber"].Value);
        tag = NormalizeOptionalValue(match.Groups["tag"].Value);

        var hasTrailingMetadata = setCode is not null || collectorNumber is not null || tag is not null;
        return !string.IsNullOrWhiteSpace(name) && (hasQuantity || hasTrailingMetadata);
    }

    internal static string NormalizeSectionId(string value) => value.Trim().ToLowerInvariant().Replace(' ', '-');

    private static string? NormalizeOptionalValue(string value)
    {
        var normalized = value.Trim();
        return normalized.Length == 0 ? null : normalized;
    }
}

public sealed record FormatParseResult(
    string? DeckName,
    IReadOnlyList<FormatDeckEntryDraft> Entries,
    IReadOnlyList<DeckSectionDraft> Sections,
    IReadOnlyList<ImportDiagnostic> Diagnostics,
    IReadOnlyDictionary<string, string> SourceMetadata);

public sealed record FormatDeckEntryDraft(
    int LineNumber,
    string OriginalLine,
    string DisplayName,
    int Quantity,
    string SectionId,
    bool IsCommander,
    bool IsCompanion,
    string? SourceSetCode,
    string? SourceCollectorNumber,
    string? SourceTag);

public sealed record DeckSectionDraft(
    string SectionId,
    string DisplayName,
    DeckSectionRole Role,
    int SortOrder);