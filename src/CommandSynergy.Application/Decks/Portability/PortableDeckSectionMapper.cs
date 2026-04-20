namespace CommandSynergy.Application.Decks.Portability;

internal static class PortableDeckSectionMapper
{
    internal const string CommandZonePileId = "command-zone";
    internal const string MainboardPileId = "mainboard";
    internal const string SideboardPileId = "sideboard";
    internal const string MaybeboardPileId = "maybeboard";
    internal const string CompanionPileId = "companion";

    internal static PortableDeckSnapshot NormalizeImportedSnapshot(PortableDeckSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var normalizedEntries = snapshot.Entries
            .Select(entry => entry with
            {
                SectionId = NormalizeSectionId(entry.SectionId, ResolveEntryRole(entry, snapshot.Sections)),
            })
            .ToArray();

        var normalizedSections = BuildSections(
            snapshot.Sections,
            normalizedEntries,
            static section => NormalizeSectionId(section.SectionId, section.Role),
            static section => NormalizeDisplayName(section.DisplayName, section.Role));

        return snapshot with
        {
            Entries = normalizedEntries,
            Sections = normalizedSections,
        };
    }

    internal static PortableDeckSnapshot ToExternalSnapshot(PortableDeckSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var externalEntries = snapshot.Entries
            .Select(entry =>
            {
                var role = ResolveEntryRole(entry, snapshot.Sections);
                return entry with
                {
                    SectionId = ToExternalSectionId(entry.SectionId, role),
                };
            })
            .ToArray();

        var externalSections = BuildSections(
            snapshot.Sections,
            externalEntries,
            static section => ToExternalSectionId(section.SectionId, section.Role),
            static section => ToExternalDisplayName(section.DisplayName, section.Role));

        return snapshot with
        {
            Entries = externalEntries,
            Sections = externalSections,
        };
    }

    internal static DeckSectionRole InferRole(string? sectionId)
    {
        return NormalizeSectionToken(sectionId) switch
        {
            "command-zone" or "commander" or "command-zone:" or "command-zone]" => DeckSectionRole.Commander,
            "companion" => DeckSectionRole.Companion,
            "sideboard" => DeckSectionRole.Sideboard,
            "maybeboard" => DeckSectionRole.Maybeboard,
            "mainboard" or "deck" => DeckSectionRole.Mainboard,
            _ => DeckSectionRole.Custom,
        };
    }

    private static IReadOnlyList<DeckSectionState> BuildSections(
        IReadOnlyList<DeckSectionState> sourceSections,
        IReadOnlyList<PortableDeckEntry> remappedEntries,
        Func<DeckSectionState, string> sectionIdSelector,
        Func<DeckSectionState, string> displayNameSelector)
    {
        var sectionsById = new Dictionary<string, DeckSectionState>(StringComparer.OrdinalIgnoreCase);

        foreach (var sourceSection in sourceSections.OrderBy(static section => section.SortOrder))
        {
            var sectionId = sectionIdSelector(sourceSection);
            var displayName = displayNameSelector(sourceSection);

            if (!sectionsById.TryGetValue(sectionId, out var existingSection))
            {
                sectionsById[sectionId] = sourceSection with
                {
                    SectionId = sectionId,
                    DisplayName = displayName,
                    EntryCount = 0,
                };
                continue;
            }

            sectionsById[sectionId] = existingSection with
            {
                SortOrder = Math.Min(existingSection.SortOrder, sourceSection.SortOrder),
            };
        }

        foreach (var entryGroup in remappedEntries.GroupBy(static entry => entry.SectionId, StringComparer.OrdinalIgnoreCase))
        {
            var sectionId = entryGroup.Key;
            var quantity = entryGroup.Sum(static entry => entry.Quantity);

            if (!sectionsById.TryGetValue(sectionId, out var existingSection))
            {
                var inferredRole = ResolveGroupRole(entryGroup);
                sectionsById[sectionId] = new DeckSectionState(
                    sectionId,
                    inferredRole is DeckSectionRole.Custom ? sectionId : NormalizeDisplayName(sectionId, inferredRole),
                    inferredRole,
                    sectionsById.Count,
                    quantity);
                continue;
            }

            sectionsById[sectionId] = existingSection with
            {
                EntryCount = quantity,
            };
        }

        return sectionsById.Values
            .OrderBy(static section => section.SortOrder)
            .ThenBy(static section => section.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static DeckSectionRole ResolveEntryRole(PortableDeckEntry entry, IReadOnlyList<DeckSectionState> sections)
    {
        if (entry.IsCommander)
        {
            return DeckSectionRole.Commander;
        }

        if (entry.IsCompanion)
        {
            return DeckSectionRole.Companion;
        }

        var section = sections.FirstOrDefault(candidate => string.Equals(candidate.SectionId, entry.SectionId, StringComparison.OrdinalIgnoreCase));
        return section?.Role ?? InferRole(entry.SectionId);
    }

    private static DeckSectionRole ResolveGroupRole(IGrouping<string, PortableDeckEntry> entries)
    {
        var firstEntry = entries.First();
        return firstEntry.IsCommander
            ? DeckSectionRole.Commander
            : firstEntry.IsCompanion
                ? DeckSectionRole.Companion
                : InferRole(firstEntry.SectionId);
    }

    private static string NormalizeSectionId(string? sectionId, DeckSectionRole role)
    {
        return role switch
        {
            DeckSectionRole.Commander => CommandZonePileId,
            DeckSectionRole.Mainboard => MainboardPileId,
            DeckSectionRole.Sideboard => SideboardPileId,
            DeckSectionRole.Maybeboard => MaybeboardPileId,
            DeckSectionRole.Companion => CompanionPileId,
            _ => NormalizeSectionToken(sectionId),
        };
    }

    private static string NormalizeDisplayName(string? displayName, DeckSectionRole role)
    {
        return role switch
        {
            DeckSectionRole.Commander => "Command Zone",
            DeckSectionRole.Mainboard => "Mainboard",
            DeckSectionRole.Sideboard => "Sideboard",
            DeckSectionRole.Maybeboard => "Maybeboard",
            DeckSectionRole.Companion => "Companion",
            _ => string.IsNullOrWhiteSpace(displayName) ? "Custom" : displayName.Trim(),
        };
    }

    private static string ToExternalSectionId(string? sectionId, DeckSectionRole role)
    {
        return role switch
        {
            DeckSectionRole.Commander => "commander",
            DeckSectionRole.Mainboard => "mainboard",
            DeckSectionRole.Sideboard => "sideboard",
            DeckSectionRole.Maybeboard => "maybeboard",
            DeckSectionRole.Companion => "companion",
            _ => NormalizeSectionToken(sectionId),
        };
    }

    private static string ToExternalDisplayName(string? displayName, DeckSectionRole role)
    {
        return role switch
        {
            DeckSectionRole.Commander => "Commander",
            DeckSectionRole.Mainboard => "Mainboard",
            DeckSectionRole.Sideboard => "Sideboard",
            DeckSectionRole.Maybeboard => "Maybeboard",
            DeckSectionRole.Companion => "Companion",
            _ => string.IsNullOrWhiteSpace(displayName) ? "Custom" : displayName.Trim(),
        };
    }

    private static string NormalizeSectionToken(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? MainboardPileId
            : value.Trim().ToLowerInvariant().Replace(' ', '-');
    }
}