using System.Text.RegularExpressions;

namespace CommandSynergy.Application.Decks.Portability.Formats;

public abstract class DeckFormatProfileBase
{
    private static readonly Regex QuantityLinePattern = new("^(?<quantity>\\d+)x?\\s+(?<name>.+?)$", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

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

    internal static string NormalizeSectionId(string value) => value.Trim().ToLowerInvariant().Replace(' ', '-');
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
    bool IsCompanion);

public sealed record DeckSectionDraft(
    string SectionId,
    string DisplayName,
    DeckSectionRole Role,
    int SortOrder);