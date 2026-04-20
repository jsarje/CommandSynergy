using CommandSynergy.Application.Abstractions;
using CommandSynergy.Application.Contracts;

namespace CommandSynergy.Application.Decks.Portability;

public sealed class WorkingCopyProjectionService : IWorkingCopyProjectionService
{
    public DeckSnapshotContract CreateWorkingCopy(PortableDeckSnapshot snapshot, string? deckId = null, string? deckName = null)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var normalizedSnapshot = PortableDeckSectionMapper.NormalizeImportedSnapshot(snapshot);
        var piles = BuildWorkspacePiles(normalizedSnapshot);

        var commanderCardId = normalizedSnapshot.CommanderCardIds.FirstOrDefault()
            ?? normalizedSnapshot.Entries.FirstOrDefault(static entry => entry.IsCommander && !string.IsNullOrWhiteSpace(entry.CardId))?.CardId;

        return new DeckSnapshotContract
        {
            DeckId = deckId,
            Name = deckName ?? normalizedSnapshot.DeckName,
            CommanderCardId = commanderCardId,
            CompanionCardId = normalizedSnapshot.CompanionCardId,
            Piles = piles,
            Entries = normalizedSnapshot.Entries
                .Where(static entry => !string.IsNullOrWhiteSpace(entry.CardId))
                .Select(static entry => new DeckEntryContract
                {
                    CardId = entry.CardId!,
                    Quantity = entry.Quantity,
                    AssignedPileId = entry.SectionId,
                    IsCommander = entry.IsCommander,
                    IsCompanion = entry.IsCompanion,
                })
                .ToArray(),
        };
    }

    private static IReadOnlyList<PileDefinitionContract> BuildWorkspacePiles(PortableDeckSnapshot snapshot)
    {
        var piles = snapshot.Sections
            .OrderBy(static section => section.SortOrder)
            .Select(static section => new PileDefinitionContract
            {
                PileId = section.SectionId,
                Name = section.DisplayName,
                SortOrder = section.SortOrder,
            })
            .ToList();

        if (piles.All(static pile => !string.Equals(pile.PileId, PortableDeckSectionMapper.CommandZonePileId, StringComparison.OrdinalIgnoreCase)))
        {
            piles.Insert(0, new PileDefinitionContract
            {
                PileId = PortableDeckSectionMapper.CommandZonePileId,
                Name = "Command Zone",
                SortOrder = 0,
            });
        }

        if (piles.All(static pile => !string.Equals(pile.PileId, PortableDeckSectionMapper.MainboardPileId, StringComparison.OrdinalIgnoreCase)))
        {
            var mainboardIndex = Math.Min(1, piles.Count);
            piles.Insert(mainboardIndex, new PileDefinitionContract
            {
                PileId = PortableDeckSectionMapper.MainboardPileId,
                Name = "Mainboard",
                SortOrder = mainboardIndex,
            });
        }

        return piles
            .Select(static (pile, index) => new PileDefinitionContract
            {
                PileId = pile.PileId,
                Name = pile.Name,
                SortOrder = index,
            })
            .ToArray();
    }
}