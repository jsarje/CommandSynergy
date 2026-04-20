namespace CommandSynergy.Application.Decks.Portability.Formats;

internal static class MoxfieldStyleParser
{
    public static FormatParseResult Parse(string documentText, Func<string, bool> isSectionHeader)
    {
        var diagnostics = new List<ImportDiagnostic>();
        var entries = new List<FormatDeckEntryDraft>();
        var sections = new List<DeckSectionDraft>();
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var currentSection = CreateSection("mainboard", "Mainboard", DeckSectionRole.Mainboard, 1);
        sections.Add(currentSection);
        var sawExplicitSectionHeader = false;

        var lines = documentText.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        for (var index = 0; index < lines.Length; index++)
        {
            var lineNumber = index + 1;
            var rawLine = lines[index];
            var trimmedLine = rawLine.Trim();

            if (string.IsNullOrWhiteSpace(trimmedLine))
            {
                continue;
            }

            if (trimmedLine.StartsWith("Deck:", StringComparison.OrdinalIgnoreCase) && trimmedLine.Length > 5)
            {
                metadata["deckName"] = trimmedLine[5..].Trim();
                continue;
            }

            if (isSectionHeader(trimmedLine))
            {
                sawExplicitSectionHeader = true;
                currentSection = ParseSection(trimmedLine, sections.Count);
                if (sections.All(section => !string.Equals(section.SectionId, currentSection.SectionId, StringComparison.OrdinalIgnoreCase)))
                {
                    sections.Add(currentSection);
                }

                continue;
            }

            if (DeckFormatProfileBase.TryParseEntryLine(trimmedLine, out var quantity, out var name, out var setCode, out var collectorNumber, out var tag))
            {
                entries.Add(new FormatDeckEntryDraft(
                    lineNumber,
                    rawLine,
                    name,
                    quantity,
                    currentSection.SectionId,
                    currentSection.Role == DeckSectionRole.Commander,
                    currentSection.Role == DeckSectionRole.Companion,
                    setCode,
                    collectorNumber,
                    tag));
                continue;
            }

            if (!metadata.ContainsKey("deckName")
                && entries.Count == 0
                && !sawExplicitSectionHeader
                && !trimmedLine.Contains(':', StringComparison.Ordinal))
            {
                metadata["deckName"] = trimmedLine;
                continue;
            }

            diagnostics.Add(new ImportDiagnostic(
                Guid.NewGuid().ToString("N"),
                DiagnosticSeverity.Warning,
                "unrecognized-line",
                "The line could not be interpreted as a supported deck entry.",
                lineNumber,
                rawLine,
                "Check the quantity and card name format, or choose a different source format."));
        }

        var deckName = metadata.TryGetValue("deckName", out var explicitDeckName)
            ? explicitDeckName
            : entries.FirstOrDefault(static entry => entry.IsCommander)?.DisplayName;

        return new FormatParseResult(deckName, entries, sections, diagnostics, metadata);
    }

    private static DeckSectionDraft ParseSection(string line, int sortOrder)
    {
        var cleaned = line.Trim().Trim('[', ']').TrimEnd(':').Trim().TrimStart('#').Trim();
        var role = cleaned.ToLowerInvariant() switch
        {
            "commander" or "command zone" => DeckSectionRole.Commander,
            "companion" => DeckSectionRole.Companion,
            "sideboard" => DeckSectionRole.Sideboard,
            "maybeboard" => DeckSectionRole.Maybeboard,
            "deck" or "mainboard" => DeckSectionRole.Mainboard,
            _ => DeckSectionRole.Custom,
        };

        return CreateSection(DeckFormatProfileBase.NormalizeSectionId(cleaned), cleaned, role, sortOrder + 1);
    }

    private static DeckSectionDraft CreateSection(string sectionId, string displayName, DeckSectionRole role, int sortOrder) =>
        new(sectionId, displayName, role, sortOrder);
}

internal static class MoxfieldStyleRenderer
{
    public static string Render(PortableDeckSnapshot snapshot, bool includeBracketHeaders, bool includeHashHeaders = false)
    {
        var lines = new List<string>();
        if (!string.IsNullOrWhiteSpace(snapshot.DeckName))
        {
            lines.Add($"Deck: {snapshot.DeckName}");
            lines.Add(string.Empty);
        }

        foreach (var section in snapshot.Sections.OrderBy(static section => section.SortOrder))
        {
            var sectionEntries = snapshot.Entries
                .Where(entry => string.Equals(entry.SectionId, section.SectionId, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(static entry => entry.IsCommander)
                .ThenBy(static entry => entry.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (sectionEntries.Length == 0)
            {
                continue;
            }

            if (includeBracketHeaders)
            {
                lines.Add($"[{section.DisplayName}]");
            }
            else if (includeHashHeaders)
            {
                lines.Add($"# {section.DisplayName}");
            }
            else
            {
                lines.Add($"{section.DisplayName}:");
            }

            foreach (var entry in sectionEntries)
            {
                lines.Add($"{entry.Quantity} {entry.DisplayName}");
            }

            lines.Add(string.Empty);
        }

        while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[^1]))
        {
            lines.RemoveAt(lines.Count - 1);
        }

        return string.Join(Environment.NewLine, lines);
    }
}